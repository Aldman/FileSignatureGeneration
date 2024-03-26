using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using FileSignatureGeneration.Extensions;

namespace FileSignatureGeneration.SegmentGeneration;

internal static class SegmentGenerator
{
    private static int _necessaryCalculationThreadsCount;
    private static int _currentCalculationThreadCount;
    
    private static readonly ConcurrentQueue<(int SegmentNumber, byte[] Data)> SegmentTuples = new();
    private static readonly SegmentInfo SegmentInfo = new();
    private static readonly Stopwatch Sw = new();
    
    internal static void RunSegmentGeneration(SegmentGenerationData data)
    {
        using var fileStream = data.FileStream;
        SegmentInfo.SegmentSizeInBytes = data.SegmentSizeInBytes;

        CheckQueueAndCalculateHashInBackgroundThreads();
        InitialiseFields(fileStream);
        FillSegmentTuples(fileStream);

        while (!SegmentTuples.IsEmpty)
        {
            Thread.Sleep(100);
        }
        
        Console.WriteLine("Программа завершена");
    }

    private static void CheckQueueAndCalculateHashInBackgroundThreads()
    {
        const int timeoutInSec = 5;
        Sw.Start();
        
        var checkQueueThread = new Thread(() =>
        {
            while (true)
            {
                var oldQueueCount = SegmentTuples.Count;
                
                if (SegmentTuples.IsEmpty)
                {
                    Thread.Sleep(7);
                    if (Sw.Elapsed.Seconds >= timeoutInSec)
                        break;
                    
                    continue;
                }

                if (_currentCalculationThreadCount < _necessaryCalculationThreadsCount)
                    TryCalculateSegmentHashInNewThread();

                if (oldQueueCount > SegmentTuples.Count)
                    _currentCalculationThreadCount = oldQueueCount - SegmentTuples.Count;
            }
        });
        checkQueueThread.Start();
    }

    private static void TryCalculateSegmentHashInNewThread()
    {
        var calculationThread = new Thread(() =>
        {
            var success = SegmentTuples.TryDequeue(out var segmentTuple);
            if (!success) return;

            Sw.Reset();
            TryDoActionWithSegment(() =>
                {
                    var hash = SHA256.HashData(segmentTuple.Data);
                    _currentCalculationThreadCount++;
                    Console.WriteLine($"Сегмент #{segmentTuple.SegmentNumber}, хэш: {hash.ToHexString()}");
                },
                segmentTuple.SegmentNumber);
        });
        calculationThread.Start();
    }

    private static void InitialiseFields(FileStream fileStream)
    {
        var fileSizeInBytes = fileStream.Length;

        var optimalThreadsCount = Environment.ProcessorCount / 2;
        SegmentInfo.SegmentsCount = (int)(fileSizeInBytes / SegmentInfo.SegmentSizeInBytes);
        _necessaryCalculationThreadsCount = Math.Min(optimalThreadsCount, SegmentInfo.SegmentsCount);
        
        SegmentInfo.LastSegmentSize = (int)(fileSizeInBytes % SegmentInfo.SegmentSizeInBytes);
        if (SegmentInfo.LastSegmentSize > 0) SegmentInfo.SegmentsCount++;
    }

    private static void FillSegmentTuples(FileStream fileStream)
    {
        for (var i = 1; i <= SegmentInfo.SegmentsCount; i++)
        {
            var buffer = new byte[SegmentInfo.SegmentSizeInBytes];
            var readBytesCount = i == SegmentInfo.SegmentsCount
                ? SegmentInfo.LastSegmentSize
                : (int)SegmentInfo.SegmentSizeInBytes;
            
            var endStream = false;

            TryDoActionWithSegment(() =>
            {
                var readBytes = fileStream.Read(buffer, 0, readBytesCount);
                if (readBytes <= 0) endStream = true;
            },
                i);
            if (endStream) return;
            
            SegmentTuples.Enqueue((i, buffer));
        }
    }

    private static bool TryDoActionWithSegment(Action action, int segment)
    {
        try
        {
            action.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Расчет сегмента #{segment} завершился с ошибкой: {e}");
            Console.WriteLine($"Stack trace: {e.StackTrace}");
            return false;
        }
    }
}