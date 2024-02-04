using System.Security.Cryptography;
using System.Text;

namespace FileSignatureGeneration
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Введите путь к файлу");
            //var filePath = Console.ReadLine();
            var filePath = @"C:\Users\AlyoshkinDV\Desktop\Robin\Tasks\FileSignatureGeneration\TestFile.pdf";
            using var fileStream = File.Open(filePath, FileMode.Open);

            Console.WriteLine("Введите размер сегмента в байтах");
            //var segmentSize = int.Parse(Console.ReadLine()!);

            //var segmentSizeInBytes = int.Parse(Console.ReadLine()!);
            var segmentSizeInBytes = 100;
            var fileSizeInBytes = fileStream.Length;
            var threadsCount = (int)fileSizeInBytes / segmentSizeInBytes;
            var lastSizeInBytes = fileSizeInBytes % segmentSizeInBytes;
            if (lastSizeInBytes > 0) threadsCount++;

            var threadList = new List<Thread>();

            RunSegmentGenerationInThreads(fileStream, threadList, threadsCount, segmentSizeInBytes, lastSizeInBytes);

            foreach (var thread in threadList)
            {
                thread.Join();
            }

            Console.WriteLine("Программа завершена");
        }

        private static void RunSegmentGenerationInThreads(FileStream fileStream, List<Thread> threadList,
            long threadsCount, int segmentSizeInBytes, long lastSizeInBytes)
        {
            for (var i = 0; i < threadsCount; i++)
            {
                var offset = i * segmentSizeInBytes;
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
}