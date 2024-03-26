namespace FileSignatureGeneration.Utils;

internal static class ConsoleDataGetter
{
    public static string GetFilePath()
    {
        while (true)
        {
            Console.WriteLine("Введите путь к файлу");
            var inputPath = Console.ReadLine();
            if (!string.IsNullOrEmpty(inputPath) && File.Exists(inputPath))
                return inputPath;

            Console.WriteLine("Введен некорректный или несуществующий путь к файлу");
            Console.WriteLine();
        }
    }
    
    public static long GetSegmentSizeInBytes()
    {
        while (true)
        {
            Console.WriteLine("Введите размер сегмента в байтах");
            var input = Console.ReadLine();
            if (long.TryParse(input, out var segmentSize))
                return segmentSize;

            Console.WriteLine("Введен некорректный размер сегмента");
            Console.WriteLine();
        }
    }
}