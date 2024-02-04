namespace FileSignatureGeneration;

public class SegmentGenerationData
{
    public string FilePath { get; private set; }
    
    public long SegmentSizeInBytes { get; private set; }
    
    public Lazy<FileStream> FileStream => new(File.Open(FilePath, FileMode.Open));

    public static SegmentGenerationData GetData()
    {
        return new SegmentGenerationData
        {
            FilePath = ConsoleDataGetter.FilePath,
            SegmentSizeInBytes = ConsoleDataGetter.SegmentSizeInBytes
        };
    }
}