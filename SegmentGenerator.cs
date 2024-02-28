using System.Collections.Concurrent;
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
        var backgroundThread = new Thread(() =>
        {
            while (true)
            {
                var oldQueueCount = _segmentTuples.Count;
                
                if (_segmentTuples.IsEmpty)
                {
                    Thread.Sleep(MyFavoriteNumber);
                    continue;
                }

                if (_calculationThreadCount < _necessaryCalculationThreadsCount)
                {
                    var calculationThread = new Thread(() =>
                    {
                        var success = _segmentTuples.TryDequeue(out var segmentTuple);
                        if (!success) return;
                        
                        TryActionRelatesSegmentCalculation(() =>
                        {
                            var hash = SHA256.HashData(segmentTuple.Data);
                            _calculationThreadCount++; // TODO: вынести вне тредов
                            Console.WriteLine($"Сегмент #{segmentTuple.SegmentNumber}, хэш: {ToHexHashString(hash)}");
                            //Console.WriteLine($"Tread count: {_calculationThreadCount}");
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

    private static void RunSegmentGenerationInThreads(FileStream fileStream,
        List<Thread> threadList, long segmentSizeInBytes)
    {
        var fileSizeInBytes = fileStream.Length;

        var optimalThreadsCount = Environment.ProcessorCount / 2;
        var segmentsCount = fileSizeInBytes / segmentSizeInBytes;
        var requiredThreadsCount = Math.Min(optimalThreadsCount, segmentsCount);
        var segmentsPerThread = segmentsCount / requiredThreadsCount;
        var segmentsRemnant = segmentsCount % requiredThreadsCount;

        for (var i = 1; i <= requiredThreadsCount; i++)
        {
             var thread = new Thread(() => ComputeSha256(fileStream,
                 (int)segmentSizeInBytes, segmentsPerThread, segmentsRemnant))
             {
                 Name = $"{i}"
             };
             threadList.Add(thread);
             
             thread.Start();
        }
    }

    private static void ComputeSha256(FileStream fileStream, int bytesCount, long segmentsPerThread,
        long segmentsRemnant)
    {
        lock (fileStream)
        {
            var currentThreadNumber = Thread.CurrentThread.Name!.ToInt();
            var segmentsForCurrentThread = currentThreadNumber <= segmentsRemnant
                ? segmentsPerThread + 1
                : segmentsPerThread;

            var firstSegmentNumber = currentThreadNumber <= segmentsRemnant
                ? currentThreadNumber * segmentsForCurrentThread - segmentsPerThread
                : currentThreadNumber * segmentsForCurrentThread + segmentsRemnant;

            foreach (var additionNumber in Enumerable.Range(0, (int)segmentsForCurrentThread))
            {
                var currentSegmentNumber = firstSegmentNumber + additionNumber;

                var buffer = new byte[bytesCount];

                try
                {
                    fileStream.Position = (currentSegmentNumber - 1) * bytesCount;
                    var readBytes = fileStream.Read(buffer, 0, bytesCount);
                    if (readBytes <= 0) return;
                    var hash = SHA256.HashData(buffer);
                    Console.WriteLine($"Сегмент #{currentSegmentNumber}, хэш: {ToHexHashString(hash)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Расчет сегмента #{currentSegmentNumber} завершился с ошибкой: {e}");
                    Console.WriteLine($"Stack trace: {e.StackTrace}");
                }
                finally
                {
                    Console.WriteLine();
                }
            }
        }
    }

    
    // TODO: проверить что будет, если передавть сюда полный массив, либо 2 по частям. Если строка хэша сходится,
    // то можно органиваться систему. не падающую при большом введенном сегменте
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