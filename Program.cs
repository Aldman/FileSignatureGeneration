using FileSignatureGeneration.SegmentGeneration;

namespace FileSignatureGeneration
{
    internal static class Program
    {
        public static void Main()
        {
            var data = SegmentGenerationData.GetInputDataFromUser();
            SegmentGenerator.RunSegmentGeneration(data);
        }
    }
}