namespace TT.Download
{
    public readonly struct DownloadRange
    {
        public readonly string Path;
        public readonly long Start;
        public readonly long End;
        public readonly long ChunkLength;
        public readonly bool RequiresPartialResponse;
        public bool IsComplete => Start > End;

        public DownloadRange(string path, long start, long end, long chunkLength, bool requiresPartialResponse)
        {
            Path = path;
            Start = start;
            End = end;
            ChunkLength = chunkLength;
            RequiresPartialResponse = requiresPartialResponse;
        }
    }
}
