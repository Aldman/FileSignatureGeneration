using FileSignatureGeneration.Extensions;

namespace FileSignatureGeneration.SegmentGeneration;

internal static class SegmentGenerator
{
    internal static void RunSegmentGeneration(SegmentGenerationData data)
    {
        using var fileStream = data.FileStream;
        
        var segmentInfo = GetSegmentInfo(fileStream, data.SegmentSizeInBytes);
        var generationTasks = GetGenerateSegmentsTasks(fileStream, segmentInfo);

        Task.WaitAll(generationTasks.ToArray());
        
        Console.WriteLine("Программа завершена");
    }

    private static List<Task> GetGenerateSegmentsTasks(FileStream fileStream, SegmentInfo segmentInfo)
    {
        var tasks = new List<Task>();
        
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
                if (readBytes <= 0) return tasks;
                    
                var task = Task.Run(() => Console.WriteLine($"Сегмент #{segmentNumber}, хэш: {buffer.ToHexString()}"));
                tasks.Add(task);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Расчет сегмента #{segmentNumber} завершился с ошибкой: {e}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
        }

        return tasks;
    }

    private static SegmentInfo GetSegmentInfo(FileStream fileStream, long segmentSizeInBytes)
    {
        var fileSizeInBytes = fileStream.Length;

        var segmentsCount = (int)(fileSizeInBytes / segmentSizeInBytes);
        
        var lastSegmentSize = (int)(fileSizeInBytes % segmentSizeInBytes);
        if (lastSegmentSize > 0) segmentsCount++;

        return new SegmentInfo(segmentsCount, segmentSizeInBytes, lastSegmentSize);
    }
}