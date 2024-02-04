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
        var threadsCount = (int)fileSizeInBytes / segmentSizeInBytes;
        var lastSizeInBytes = fileSizeInBytes % segmentSizeInBytes;
        if (lastSizeInBytes > 0) threadsCount++;
        
        for (var i = 0; i < threadsCount; i++)
        {
            var offset = i * (int)segmentSizeInBytes;
            var bytesCount = i + 1 == threadsCount
                ? lastSizeInBytes
                : segmentSizeInBytes;
            var thread = new Thread(() => ComputeSha256(fileStream, offset, (int)bytesCount))
            {
                Name = $"{i + 1}"
            };
            threadList.Add(thread);

            thread.Start();
        }
    }

    private static void ComputeSha256(FileStream fileStream, int offset, int bytesCount)
    {
        lock (fileStream)
        {
            var buffer = new byte[bytesCount];
            var threadNumber = Thread.CurrentThread.Name;

            try
            {
                fileStream.Position = offset;
                var readBytes = fileStream.Read(buffer, 0, bytesCount);
                if (readBytes <= 0) return;
                var hash = SHA256.HashData(buffer);
                Console.WriteLine($"Сегмент #{threadNumber}, хэш: {ToHexHashString(hash)}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Расчет сегмента #{threadNumber} завершился с ошибкой: {e}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
            finally
            {
                Console.WriteLine();
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