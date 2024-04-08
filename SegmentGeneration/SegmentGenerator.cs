using FileSignatureGeneration.Extensions;

namespace FileSignatureGeneration.SegmentGeneration;

internal static class SegmentGenerator
{
    internal static void RunSegmentGeneration(SegmentGenerationData data)
    {
        using var fileStream = data.FileStream;
        
        var threadsCount = GetOptimalThreadsCount();
        var threadPool = new ThreadPool(threadsCount);

        var segmentInfo = GetSegmentInfo(fileStream, data.SegmentSizeInBytes);
        GenerateSegments(fileStream, segmentInfo, threadPool);

        while (!threadPool.IsIdle)
        {
            Thread.Sleep(100);
        }
        
        Console.WriteLine("Программа завершена");
    }

    private static SegmentInfo GetSegmentInfo(FileStream fileStream, long segmentSizeInBytes)
    {
        var fileSizeInBytes = fileStream.Length;

        var segmentsCount = (int)(fileSizeInBytes / segmentSizeInBytes);
        
        var lastSegmentSize = (int)(fileSizeInBytes % segmentSizeInBytes);
        if (lastSegmentSize > 0) segmentsCount++;

        return new SegmentInfo(segmentsCount, segmentSizeInBytes, lastSegmentSize);
    }

    private static int GetOptimalThreadsCount() => Environment.ProcessorCount / 2;

    private static void GenerateSegments(FileStream fileStream, SegmentInfo segmentInfo, ThreadPool threadPool)
    {
        for (var i = 1; i <= segmentInfo.SegmentsCount; i++)
        {
            var buffer = new byte[segmentInfo.SegmentSizeInBytes];
            var readBytesCount = i == segmentInfo.SegmentsCount
                ? segmentInfo.LastSegmentSize
                : (int)segmentInfo.SegmentSizeInBytes;

            var segmentNumber = i;
            try
            {
                var readBytes = fileStream.Read(buffer, 0, readBytesCount);
                if (readBytes >= 0)
                    threadPool.Run(() => Console.WriteLine($"Сегмент #{segmentNumber}, хэш: {buffer.ToHexString()}"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Расчет сегмента #{segmentNumber} завершился с ошибкой: {e}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
        }
    }
}