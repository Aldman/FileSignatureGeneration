using System.Security.Cryptography;
using System.Text;
using FileSignatureGeneration.Utils;

namespace FileSignatureGeneration;

internal static class SegmentGenerator
{
    internal static void RunSegmentGeneration(SegmentGenerationData data)
    {
        using var fileStream = data.FileStream.Value;
        var segmentSizeInBytes = data.SegmentSizeInBytes;

        var threadList = new List<Thread>();
        RunSegmentGenerationInThreads(fileStream, threadList, segmentSizeInBytes);

        foreach (var thread in threadList)
        {
            thread.Join();
        }

        Console.WriteLine("Программа завершена");
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