namespace FileSignatureGeneration.Utils;

internal class SegmentGenerationData
{
    private string FilePath { get; init; }
    
    public long SegmentSizeInBytes { get; private set; }
    
    public Lazy<FileStream> FileStream => new(File.Open(FilePath, FileMode.Open));

    public static SegmentGenerationData GetInputDataFromUser()
    {
        return new SegmentGenerationData
        {
            FilePath = ConsoleDataGetter.FilePath,
            SegmentSizeInBytes = ConsoleDataGetter.SegmentSizeInBytes
        };
    }
}