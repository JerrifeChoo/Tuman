using System.Threading;

namespace TT.Download {
    public enum DownloadChunkMode
    {
        RandomWriteSingleFile,
        SeparatePartFiles
    }

    public struct DownloadRequest
    {
        public string Url;
        public string LocalPath;
        public int ChunkCount;
        public DownloadChunkMode ChunkMode;

        public DownloadRequest(string url, string localPath, int chunkCount = 1, DownloadChunkMode chunkMode = DownloadChunkMode.SeparatePartFiles)
        {
            Url = url;
            LocalPath = localPath;
            ChunkCount = chunkCount;
            ChunkMode = chunkMode;
        }
    }

    public struct DownloadTask
    {
        public string Url { get; set; }
        public string LocalPath { get; set; }
        public int ChunkCount { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public DownloadChunkMode ChunkMode { get; set; }
        public long TotalSize { get; set; }
        public DownloadState State;
    }

    public sealed class DownloadState
    {
        public CancellationTokenSource CTS { get; }
        public int RefCount { get; set; }
        public bool Finished { get; set; }
        public bool IsMerging { get; set; }

        public DownloadState(CancellationTokenSource cts)
        {
            CTS = cts;
            RefCount = 1;
        }
    }

    //public enum DownloadStatus
    //{
    //    Pending,
    //    Downloading,
    //    Success,
    //    Failed,
    //    Cancelled
    //}

    public struct FileBin
    {
        public long[] Downloads;
    }

    //public class DownloadResult
    //{
    //    public string Url { get; set; }
    //    public string FilePath { get; set; }
    //    public bool Success { get; set; }
    //    public string Error { get; set; }
    //}

    //public class DownloadProgress
    //{
    //    public string Url { get; set; }
    //    public float Progress { get; set; }
    //    public int CompletedCount { get; set; }
    //    public int TotalCount { get; set; }
    //}
}