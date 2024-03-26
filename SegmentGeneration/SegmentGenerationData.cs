using FileSignatureGeneration.Utils;

namespace FileSignatureGeneration.SegmentGeneration;

internal class SegmentGenerationData
{
    private string FilePath { get; init; }
    
    public long SegmentSizeInBytes { get; private set; }
    
    public FileStream FileStream => File.Open(FilePath, FileMode.Open);

    public static SegmentGenerationData GetInputDataFromUser()
    {
        return new SegmentGenerationData
        {
            FilePath = ConsoleDataGetter.GetFilePath(),
            SegmentSizeInBytes = ConsoleDataGetter.GetSegmentSizeInBytes()
        };
    }
}