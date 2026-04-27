using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace TT.Download
{
    public class DownloadManager : MonoBehaviour
    {
        private const int TimeOut = 30;
        private const int MaxConcurrentDownloads = 5;
        private const int DownloadBufferSize = 256 * 1024;

        private ConcurrentQueue<DownloadTask> pendings = new ConcurrentQueue<DownloadTask>();
        private List<DownloadState> downloadStates = new List<DownloadState>();
        private ConcurrentDictionary<string, FileBin> fileBins = new ConcurrentDictionary<string, FileBin>();
        private ConcurrentDictionary<string, byte> dirtyFileBins = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, long> mergeOffsets = new ConcurrentDictionary<string, long>();
        private ConcurrentDictionary<string, byte> dirtyMergeOffsets = new ConcurrentDictionary<string, byte>();
        private readonly int RetryCount = 3;
        private int activeDownloads;
        private CancellationToken lifecycleToken;

        private void Awake()
        {
            lifecycleToken = this.GetCancellationTokenOnDestroy();
            AppInstance.Instance.OnUpdate += UpdateDownload;
        }

        private void Start()
        {
            StartDownload("http://127.0.0.1/UnitySetup64-2022.3.62f3.exe", Application.persistentDataPath + "/UnitySetup64-2022.3.62f3.exe", 3).Forget();
        }

        public UniTask<CancellationTokenSource> StartDownload(string url, string path, int chunkCount = 1, DownloadChunkMode chunkMode = DownloadChunkMode.SeparatePartFiles)
        {
            var downloadCTS = new CancellationTokenSource();
            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(downloadCTS.Token, lifecycleToken);
            var state = new DownloadState(linkedCTS);
            downloadStates.Add(state);
            pendings.Enqueue(new DownloadTask
            {
                Url = url,
                LocalPath = path,
                ChunkCount = chunkCount,
                TotalChunks = chunkCount,
                ChunkMode = chunkMode,
                State = state
            });
            return UniTask.FromResult(linkedCTS);
        }

        private async UniTask Download()
        {
            DownloadTask task = default;
            try
            {
                if (!pendings.TryDequeue(out task) || task.Equals(default(DownloadTask)))
                    return;
                if (task.State.CTS.IsCancellationRequested)
                {
                    MarkTaskFinished(task.State);
                    return;
                }
                var totalSize = task.TotalSize != 0 ? task.TotalSize : await GetFileSize(task.Url, task.State.CTS.Token);
                var tempFile = task.LocalPath;
                var binFile = task.LocalPath + ".bin";
                if (task.ChunkCount > 1)
                {
                    if (task.ChunkMode == DownloadChunkMode.RandomWriteSingleFile && (!File.Exists(tempFile) || new FileInfo(tempFile).Length != totalSize))
                    {
                        CreateFileWithLength(tempFile, totalSize);
                    }
                    if (task.ChunkMode == DownloadChunkMode.RandomWriteSingleFile && (!File.Exists(binFile) || new FileInfo(binFile).Length != task.ChunkCount * 8))
                    {
                        fileBins.TryRemove(binFile, out _);
                        dirtyFileBins.TryRemove(binFile, out _);
                        CreateFileWithLength(binFile, task.ChunkCount * 8);
                    }
                    AddTaskReferences(task.State, task.ChunkCount);
                    for (var i = 0; i < task.ChunkCount; i++)
                        pendings.Enqueue(new DownloadTask
                        {
                            Url = task.Url,
                            LocalPath = task.LocalPath,
                            ChunkCount = 1,
                            TotalSize = totalSize,
                            TotalChunks = task.ChunkCount,
                            ChunkMode = task.ChunkMode,
                            State = task.State,
                            ChunkIndex = i,
                        });
                }
                else
                {
                    if (await DownloadTask(task, tempFile, totalSize, binFile))
                        MarkTaskFinished(task.State);
                }
            }
            catch (OperationCanceledException)
            {
                FlushFileBin(task.LocalPath + ".bin");
                FlushMergeOffset(task.LocalPath);
                MarkTaskFinished(task.State);
            }
            catch (Exception ex)
            {
                if (task.State != null && task.State.CTS.IsCancellationRequested)
                {
                    FlushFileBin(task.LocalPath + ".bin");
                    FlushMergeOffset(task.LocalPath);
                    MarkTaskFinished(task.State);
                    return;
                }

                FlushFileBin(task.LocalPath + ".bin");
                FlushMergeOffset(task.LocalPath);
                task.State?.CTS.Cancel();
                MarkTaskFinished(task.State);
                Debug.LogException(ex);
            }
            finally
            {
                ReleaseTaskReference(task.State);
                Interlocked.Decrement(ref activeDownloads);
            }
        }

        private async UniTask<bool> DownloadTask(DownloadTask task, string localPath, long size, string binPath)
        {
            var downloadByPart = task.ChunkMode == DownloadChunkMode.SeparatePartFiles && task.TotalChunks > 1;
            if (!downloadByPart && !File.Exists(localPath))
            {
                CreateFileWithLength(localPath, 0);
            }
            var downloadByChunk = !downloadByPart && File.Exists(binPath);
            for (int i = 0; i < RetryCount; i++)
            {
                long rangeStart;
                long rangeEnd;
                long chunkLength = 0;
                string downloadPath = localPath;
                var requiresPartialResponse = false;
                if (downloadByPart)
                {
                    if (IsPartMergeStarted(localPath, size, task.TotalChunks))
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);

                    GetChunkRange(size, task.TotalChunks, task.ChunkIndex, out var chunkStart, out var chunkEnd);
                    chunkLength = chunkEnd - chunkStart + 1;
                    if (chunkLength <= 0)
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);

                    downloadPath = GetPartPath(localPath, task.ChunkIndex);
                    var chunkLoaded = GetPartFileLength(downloadPath, chunkLength);
                    if (chunkLoaded >= chunkLength)
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);

                    rangeStart = chunkStart + chunkLoaded;
                    rangeEnd = chunkEnd;
                    requiresPartialResponse = true;
                }
                else if (downloadByChunk)
                {
                    var fileBin = GetFileBin(binPath);
                    GetChunkRange(size, fileBin.Downloads.Length, task.ChunkIndex, out var chunkStart, out var chunkEnd);
                    chunkLength = chunkEnd - chunkStart + 1;
                    if (chunkLength <= 0)
                        return TryCompleteChunkDownload(localPath, binPath, size);

                    var chunkLoaded = Math.Min(fileBin.Downloads[task.ChunkIndex], chunkLength);
                    if (chunkLoaded >= chunkLength)
                        return TryCompleteChunkDownload(localPath, binPath, size);

                    rangeStart = chunkStart + chunkLoaded;
                    rangeEnd = chunkEnd;
                    requiresPartialResponse = true;
                }
                else
                {
                    rangeStart = new FileInfo(localPath).Length;
                    rangeEnd = size - 1;
                    if (rangeStart >= size)
                        return true;
                    requiresPartialResponse = rangeStart > 0;
                }

                var requestSucceeded = false;
                using (var request = UnityWebRequest.Get(task.Url))
                {
                    request.timeout = TimeOut;
                    //分块下载
                    if (downloadByPart)
                    {
                        request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                        request.downloadHandler = new DownloadHandlerFile(downloadPath, true);
                    }
                    else if (downloadByChunk)
                    {
                        request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                        byte[] buffer = new byte[DownloadBufferSize];
                        dirtyFileBins[binPath] = 1;
                        request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart, buffer,
                            bytesWritten => UpdateFileBin(binPath, task.ChunkIndex, bytesWritten, chunkLength));
                    }
                    else
                    {
                        if (rangeStart > 0)
                            request.SetRequestHeader("Range", $"bytes={rangeStart}-{size - 1}");
                        request.downloadHandler = new DownloadHandlerFile(task.LocalPath, true);
                    }
                    request.disposeDownloadHandlerOnDispose = true;
                    await request.SendWebRequest().ToUniTask(cancellationToken: task.State.CTS.Token);
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        if (requiresPartialResponse && request.responseCode != 206)
                            throw new Exception($"Range request failed, response code: {request.responseCode}");
                        requestSucceeded = true;
                    }
                }

                if (requestSucceeded)
                {
                    if (downloadByPart)
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);
                    if (downloadByChunk)
                        return TryCompleteChunkDownload(localPath, binPath, size);
                    return true;
                }
            }
            throw new Exception("Timeout");
        }

        private void UpdateDownload()
        {
            //Application.internetReachability == NetworkReachability.NotReachable
            while (Volatile.Read(ref activeDownloads) < MaxConcurrentDownloads && pendings.TryPeek(out _))
            {
                Interlocked.Increment(ref activeDownloads);
                Download().Forget();
            }
        }

        private async UniTask<long> GetFileSize(string url, CancellationToken token)
        {
            for (var i = 0; i < RetryCount; i++)
            {
                using (var request = UnityWebRequest.Head(url))
                {
                    request.timeout = TimeOut;
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var contentLength = request.GetResponseHeader("Content-Length");
                        if (long.TryParse(contentLength, out var fileSize))
                            return fileSize;
                        throw new Exception("Missing Content-Length");
                    }
                }
            }
            throw new Exception("Timeout");
        }

        private FileBin GetFileBin(string path)
        {
            return fileBins.GetOrAdd(path, ReadFileBin);
        }

        private void CreateFileWithLength(string path, long length)
        {
            using (var file = File.Create(path))
            {
                file.SetLength(length);
            }
        }

        private FileBin ReadFileBin(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                FileBin fileBin = new FileBin();
                fileBin.Downloads = new long[bytes.Length / 8];
                var index = 0;
                for (int i = 0; i < bytes.Length; i = i + 8)
                {
                    fileBin.Downloads[index] = BitConverter.ToInt64(bytes, i);
                    index++;
                }
                return fileBin;
            }
        }

        private void UpdateFileBin(string path, int chunkIndex, int bytesWritten, long chunkLength)
        {
            if (bytesWritten <= 0)
                return;

            var fileBin = GetFileBin(path);
            var downloaded = Math.Min(fileBin.Downloads[chunkIndex] + bytesWritten, chunkLength);
            if (downloaded == fileBin.Downloads[chunkIndex])
                return;

            fileBin.Downloads[chunkIndex] = downloaded;
        }

        private bool TryCompleteChunkDownload(string localPath, string binPath, long totalSize)
        {
            if (!File.Exists(binPath))
                return File.Exists(localPath) && new FileInfo(localPath).Length >= totalSize;

            var fileBin = GetFileBin(binPath);
            for (var i = 0; i < fileBin.Downloads.Length; i++)
            {
                GetChunkRange(totalSize, fileBin.Downloads.Length, i, out var chunkStart, out var chunkEnd);
                if (fileBin.Downloads[i] < chunkEnd - chunkStart + 1)
                    return false;
            }

            if (File.Exists(localPath) && new FileInfo(localPath).Length >= totalSize)
            {
                fileBins.TryRemove(binPath, out _);
                dirtyFileBins.TryRemove(binPath, out _);
                if (File.Exists(binPath))
                    TryDeleteFile(binPath);
                return true;
            }
            else
            {
                FlushFileBin(binPath);
                return false;
            }
        }

        private async UniTask<bool> TryCompletePartDownload(string localPath, long totalSize, int chunkCount, DownloadState state)
        {
            if (state.IsMerging)
                return false;

            state.IsMerging = true;
            var switchedToThreadPool = false;
            try
            {
                if (!ArePartsReadyForMerge(localPath, totalSize, chunkCount))
                    return false;

                await UniTask.SwitchToThreadPool();
                switchedToThreadPool = true;
                await MergePartFilesInPlace(localPath, totalSize, chunkCount, state.CTS.Token);
                DeletePartFiles(localPath, chunkCount);
                return File.Exists(localPath) && new FileInfo(localPath).Length == totalSize;
            }
            finally
            {
                state.IsMerging = false;
                if (switchedToThreadPool)
                    await UniTask.SwitchToMainThread();
            }
        }

        private async UniTask MergePartFilesInPlace(string localPath, long totalSize, int chunkCount, CancellationToken token)
        {
            var part0Path = GetPartPath(localPath, 0);
            GetChunkRange(totalSize, chunkCount, 0, out var chunk0Start, out var chunk0End);
            var chunk0Length = chunk0End - chunk0Start + 1;
            var part0Length = new FileInfo(part0Path).Length;
            if (part0Length < chunk0Length || part0Length > totalSize)
                throw new Exception("Invalid part0 length");

            var mergedBytes = Math.Min(ReadMergeOffset(localPath), part0Length - chunk0Length);
            if (mergedBytes < part0Length - chunk0Length)
                mergedBytes = part0Length - chunk0Length;
            var remainingMergedBytes = mergedBytes;
            UpdateMergeOffset(localPath, mergedBytes);

            using (var output = new FileStream(part0Path, FileMode.Append, FileAccess.Write, FileShare.Read, DownloadBufferSize, FileOptions.Asynchronous))
            {
                var buffer = new byte[DownloadBufferSize];
                for (var i = 1; i < chunkCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    GetChunkRange(totalSize, chunkCount, i, out var chunkStart, out var chunkEnd);
                    var chunkLength = chunkEnd - chunkStart + 1;
                    if (remainingMergedBytes >= chunkLength)
                    {
                        remainingMergedBytes -= chunkLength;
                        DeletePartFile(localPath, i);
                        continue;
                    }

                    using (var input = new FileStream(GetPartPath(localPath, i), FileMode.Open, FileAccess.Read, FileShare.Read, DownloadBufferSize, FileOptions.Asynchronous))
                    {
                        if (remainingMergedBytes > 0)
                            input.Seek(remainingMergedBytes, SeekOrigin.Begin);

                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            await output.WriteAsync(buffer, 0, read, token);
                            mergedBytes += read;
                            UpdateMergeOffset(localPath, mergedBytes);
                        }
                    }
                    FlushMergeOffset(localPath);
                    DeletePartFile(localPath, i);
                    remainingMergedBytes = 0;
                }
                await output.FlushAsync(token);
            }

            token.ThrowIfCancellationRequested();
            if (File.Exists(localPath))
                TryDeleteFile(localPath);
            File.Move(part0Path, localPath);
            mergeOffsets.TryRemove(localPath, out _);
            dirtyMergeOffsets.TryRemove(localPath, out _);
            DeleteMergeFile(localPath);
        }

        private long GetPart0FileLength(string localPath, long chunkLength, long totalSize)
        {
            var part0Path = GetPartPath(localPath, 0);
            if (!File.Exists(part0Path))
                return 0;

            var length = new FileInfo(part0Path).Length;
            if (length >= chunkLength && length <= totalSize)
                return length;

            if (TryDeleteFile(part0Path))
                DeleteMergeFile(localPath);
            return 0;
        }

        private bool ArePartsReadyForMerge(string localPath, long totalSize, int chunkCount)
        {
            GetChunkRange(totalSize, chunkCount, 0, out var firstChunkStart, out var firstChunkEnd);
            var firstChunkLength = firstChunkEnd - firstChunkStart + 1;
            if (GetPart0FileLength(localPath, firstChunkLength, totalSize) < firstChunkLength)
                return false;

            var remainingMergedBytes = GetMergedBytes(localPath, totalSize, chunkCount);
            for (var i = 1; i < chunkCount; i++)
            {
                GetChunkRange(totalSize, chunkCount, i, out var chunkStart, out var chunkEnd);
                var chunkLength = chunkEnd - chunkStart + 1;
                if (remainingMergedBytes >= chunkLength)
                {
                    remainingMergedBytes -= chunkLength;
                    continue;
                }

                if (GetPartFileLength(GetPartPath(localPath, i), chunkLength) < chunkLength)
                    return false;
            }

            return true;
        }

        private bool IsPartMergeStarted(string localPath, long totalSize, int chunkCount)
        {
           return File.Exists(GetMergePath(localPath));
        }

        private long GetMergedBytes(string localPath, long totalSize, int chunkCount)
        {
            GetChunkRange(totalSize, chunkCount, 0, out var firstChunkStart, out var firstChunkEnd);
            var firstChunkLength = firstChunkEnd - firstChunkStart + 1;
            var part0Length = GetPart0FileLength(localPath, firstChunkLength, totalSize);
            if (part0Length < firstChunkLength)
                return 0;

            var lengthBasedMergedBytes = part0Length - firstChunkLength;
            var offsetBasedMergedBytes = ReadMergeOffset(localPath);
            return Math.Max(lengthBasedMergedBytes, offsetBasedMergedBytes);
        }

        private long GetPartFileLength(string path, long chunkLength)
        {
            if (!File.Exists(path))
                return 0;

            var length = new FileInfo(path).Length;
            if (length <= chunkLength)
                return length;

            TryDeleteFile(path);
            return 0;
        }

        private string GetPartPath(string localPath, int chunkIndex)
        {
            return $"{localPath}.part{chunkIndex}";
        }

        private string GetMergePath(string localPath)
        {
            return localPath + ".merge";
        }

        private long ReadMergeOffset(string localPath)
        {
            if (mergeOffsets.TryGetValue(localPath, out var cachedOffset))
                return cachedOffset;

            var mergePath = GetMergePath(localPath);
            if (!File.Exists(mergePath))
                return 0;

            using (var fs = new FileStream(mergePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length < 8)
                    return 0;

                var bytes = new byte[8];
                fs.Read(bytes, 0, bytes.Length);
                var offset = BitConverter.ToInt64(bytes, 0);
                mergeOffsets[localPath] = offset;
                return offset;
            }
        }

        private void UpdateMergeOffset(string localPath, long offset)
        {
            mergeOffsets[localPath] = offset;
            dirtyMergeOffsets[localPath] = 1;
        }

        private void FlushMergeOffset(string localPath)
        {
            if (string.IsNullOrEmpty(localPath) || !dirtyMergeOffsets.TryRemove(localPath, out _) || !mergeOffsets.TryGetValue(localPath, out var offset))
                return;

            using (var fs = new FileStream(GetMergePath(localPath), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var bytes = BitConverter.GetBytes(offset);
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        private void FlushMergeOffsets()
        {
            foreach (var path in dirtyMergeOffsets.Keys)
                FlushMergeOffset(path);
        }

        private void DeletePartFiles(string localPath, int chunkCount)
        {
            for (var i = 0; i < chunkCount; i++)
                DeletePartFile(localPath, i);

            var mergePath = GetMergePath(localPath);
            if (File.Exists(mergePath))
                TryDeleteFile(mergePath);
            mergeOffsets.TryRemove(localPath, out _);
            dirtyMergeOffsets.TryRemove(localPath, out _);
        }

        private void DeletePartFile(string localPath, int chunkIndex)
        {
            var partPath = GetPartPath(localPath, chunkIndex);
            if (File.Exists(partPath))
                TryDeleteFile(partPath);
        }

        private void DeleteMergeFile(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
                return;

            var mergePath = GetMergePath(localPath);
            if (File.Exists(mergePath))
                TryDeleteFile(mergePath);
        }

        private bool TryDeleteFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return true;

                File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void FlushFileBin(string path)
        {
            if (string.IsNullOrEmpty(path) || !dirtyFileBins.TryRemove(path, out _) || !fileBins.TryGetValue(path, out var fileBin))
                return;

            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.SetLength(fileBin.Downloads.Length * 8);
                for (var i = 0; i < fileBin.Downloads.Length; i++)
                {
                    var bytes = BitConverter.GetBytes(fileBin.Downloads[i]);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private void FlushFileBins()
        {
            foreach (var path in dirtyFileBins.Keys)
                FlushFileBin(path);
        }

        private void GetChunkRange(long totalSize, int chunkCount, int chunkIndex, out long start, out long end)
        {
            start = totalSize * chunkIndex / chunkCount;
            end = totalSize * (chunkIndex + 1) / chunkCount - 1;
        }

        private void AddTaskReferences(DownloadState state, int count)
        {
            if (state == null || count <= 0)
                return;

            state.RefCount += count;
        }

        private void MarkTaskFinished(DownloadState state)
        {
            if (state == null)
                return;

            state.Finished = true;
        }

        private void ReleaseTaskReference(DownloadState state)
        {
            if (state == null)
                return;

            state.RefCount--;
            if (state.RefCount > 0 || !state.Finished)
                return;

            downloadStates.Remove(state);
            state.CTS.Dispose();
        }

        private void OnDestroy()
        {
            if (AppInstance.Instance != null)
                AppInstance.Instance.OnUpdate -= UpdateDownload;
            var states = new List<DownloadState>(downloadStates);
            foreach (var state in states)
            {
                state.CTS.Cancel();
            }
            FlushFileBins();
            FlushMergeOffsets();
            foreach (var state in states)
            {
                state.CTS.Dispose();
            }
            downloadStates.Clear();
            fileBins.Clear();
            dirtyFileBins.Clear();
            mergeOffsets.Clear();
            dirtyMergeOffsets.Clear();
        }
    }
}
