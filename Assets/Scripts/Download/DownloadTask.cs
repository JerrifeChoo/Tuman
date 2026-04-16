using System.Threading;

namespace TT.Download {
    public struct DownloadTask
    {
        public string Url { get; set; }
        public string LocalPath { get; set; }
        public int ChunkCount { get; set; }
        public int ChunkIndex { get; set; }
        public long TotalSize { get; set; }
        public CancellationTokenSource CTS;
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