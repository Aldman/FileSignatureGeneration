using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FileSignatureGeneration.Utils;

namespace FileSignatureGeneration;

internal static class SegmentGenerator
{
    private const int MyFavoriteNumber = 7;
    
    private static ConcurrentQueue<(int SegmentNumber, byte[] Data)> _segmentTuples = new();
    
    private static int _necessaryCalculationThreadsCount;
    private static int _segmentsCount;
    private static int _lastSegmentSize;
    private static long _segmentSizeInBytes;

    private static readonly Stopwatch Sw = new();

    private static int _calculationThreadCount = 0;
    
    internal static void RunSegmentGeneration(SegmentGenerationData data)
    {
        using var fileStream = data.FileStream.Value;
        _segmentSizeInBytes = data.SegmentSizeInBytes;

        CheckQueueAndCalculateHashInBackgroundThreads();
        FillOtherFields(fileStream);
        FillSegmentTuples(fileStream);

        while (!_segmentTuples.IsEmpty)
        {
            Thread.Sleep(MyFavoriteNumber);
        }
        
        Console.WriteLine("Программа завершена");
    }

    // TODO: рефакторинг
    private static void CheckQueueAndCalculateHashInBackgroundThreads()
    {
        const int timeoutInSec = 5;
        Sw.Start();
        
        var backgroundThread = new Thread(() =>
        {
            while (true)
            {
                var oldQueueCount = _segmentTuples.Count;
                
                if (_segmentTuples.IsEmpty)
                {
                    Thread.Sleep(MyFavoriteNumber);
                    if (Sw.Elapsed.Seconds >= timeoutInSec)
                        return;
                    continue;
                }
                
                Sw.Reset();

                if (_calculationThreadCount < _necessaryCalculationThreadsCount)
                {
                    var calculationThread = new Thread(() =>
                    {
                        var success = _segmentTuples.TryDequeue(out var segmentTuple);
                        if (!success) return;
                        
                        TryActionRelatesSegmentCalculation(() =>
                        {
                            var hash = SHA256.HashData(segmentTuple.Data);
                            _calculationThreadCount++;
                            Console.WriteLine($"Сегмент #{segmentTuple.SegmentNumber}, хэш: {ToHexHashString(hash)}");
                        },
                            segmentTuple.SegmentNumber);
                    });
                    calculationThread.Start();
                }

                if (oldQueueCount > _segmentTuples.Count)
                    _calculationThreadCount--;
            }
        });
        backgroundThread.Start();
    }

    private static void FillOtherFields(FileStream fileStream)
    {
        var fileSizeInBytes = fileStream.Length;

        var optimalThreadsCount = Environment.ProcessorCount / 2;
        _segmentsCount = (int)(fileSizeInBytes / _segmentSizeInBytes);
        _necessaryCalculationThreadsCount = Math.Min(optimalThreadsCount, _segmentsCount);
        
        _lastSegmentSize = (int)(fileSizeInBytes % _segmentSizeInBytes);
        if (_lastSegmentSize > 0) _segmentsCount++;
    }

    private static void FillSegmentTuples(FileStream fileStream)
    {
        for (var i = 1; i <= _segmentsCount; i++)
        {
            var buffer = new byte[_segmentSizeInBytes];
            var readBytesCount = i == _segmentsCount
                ? _lastSegmentSize
                : (int)_segmentSizeInBytes;
            
            var wasReturned = false;

            TryActionRelatesSegmentCalculation(() =>
            {
                var readBytes = fileStream.Read(buffer, 0, readBytesCount);
                if (readBytes <= 0) wasReturned = true;
            },
                i);
            if (wasReturned) return;
            
            _segmentTuples.Enqueue((i, buffer));
        }
    }

    private static bool TryActionRelatesSegmentCalculation(Action action, int calculatedSegment)
    {
        try
        {
            action.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Расчет сегмента #{calculatedSegment} завершился с ошибкой: {e}");
            Console.WriteLine($"Stack trace: {e.StackTrace}");
            return false;
        }
    }

   private static string ToHexHashString(byte[] hash256)
    {
        var sb = new StringBuilder();
        foreach (var sigh in hash256)
        {
            sb.Append($"{sigh:X2}");
        }

        return sb.ToString();
    }
}