namespace FileSignatureGeneration.SegmentGeneration;

internal record SegmentInfo
{
    internal int SegmentsCount;
    internal int LastSegmentSize;
    internal long SegmentSizeInBytes;
}