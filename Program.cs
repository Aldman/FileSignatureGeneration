using FileSignatureGeneration.Utils;

namespace FileSignatureGeneration
{
    internal static class Program
    {
        public static void Main()
        {
            var data = SegmentGenerationData.GetData();
            SegmentGenerator.RunSegmentGeneration(data);
        }
    }
}