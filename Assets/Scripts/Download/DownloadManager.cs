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
        private const int RetryCount = 3;
        private const float NetworkCheckInterval = 1f;

        private ConcurrentQueue<DownloadTask> pendings = new ConcurrentQueue<DownloadTask>();
        private List<DownloadState> downloadStates = new List<DownloadState>();
        private ProgressStore progressStore = new ProgressStore();
        private int activeDownloads;
        private float nextNetworkCheckTime;
        private bool networkReachable = true;
        private CancellationToken lifecycleToken;

        private void Awake()
        {
            lifecycleToken = this.GetCancellationTokenOnDestroy();
            AppInstance.Instance.OnUpdate += UpdateDownload;
        }

        private void Start()
        {
            //StartDownload("http://127.0.0.1/UnitySetup64-2022.3.62f3.exe", Application.persistentDataPath + "/UnitySetup64-2022.3.62f3.exe", 3).Forget();
        }

        public UniTask<CancellationTokenSource> StartDownload(string url, string path, int chunkCount = 1, DownloadChunkMode chunkMode = DownloadChunkMode.RandomWriteSingleFile)
        {
            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(lifecycleToken);
            var state = new DownloadState(linkedCTS);
            EnqueueDownload(new DownloadRequest(url, path, chunkCount, chunkMode), state);
            return UniTask.FromResult(linkedCTS);
        }

        public UniTask<CancellationTokenSource> StartDownload(DownloadRequest[] requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            if (requests.Length == 0)
                throw new ArgumentException("Downloads require at least one request.", nameof(requests));

            for (var i = 0; i < requests.Length; i++)
                ValidateDownloadRequest(requests[i]);

            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(lifecycleToken);
            for (var i = 0; i < requests.Length; i++)
                EnqueueDownload(requests[i], new DownloadState(linkedCTS));

            return UniTask.FromResult(linkedCTS);
        }

        private void EnqueueDownload(DownloadRequest request, DownloadState state)
        {
            ValidateDownloadRequest(request);
            var chunkCount = Math.Max(1, request.ChunkCount);
            downloadStates.Add(state);
            pendings.Enqueue(new DownloadTask
            {
                Url = request.Url,
                LocalPath = request.LocalPath,
                ChunkCount = chunkCount,
                TotalChunks = chunkCount,
                ChunkMode = request.ChunkMode,
                State = state
            });
        }

        private void ValidateDownloadRequest(DownloadRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
                throw new ArgumentException("Download url cannot be empty.", nameof(request));
            if (string.IsNullOrEmpty(request.LocalPath))
                throw new ArgumentException("Download local path cannot be empty.", nameof(request));
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
                var binFile = GetProgressPath(task.LocalPath);
                if (task.ChunkCount > 1)
                {
                    EnsureDownloadProgressMatchesMode(task.LocalPath, binFile, task.ChunkMode, task.ChunkCount);
                    QueueChunkTasks(task, totalSize, tempFile, binFile);
                }
                else
                {
                    if (await DownloadTask(task, tempFile, totalSize, binFile))
                        MarkTaskFinished(task.State);
                }
            }
            catch (OperationCanceledException)
            {
                progressStore.Flush(GetProgressPath(task.LocalPath));
                MarkTaskFinished(task.State);
            }
            catch (Exception ex)
            {
                if (task.State != null && task.State.CTS.IsCancellationRequested)
                {
                    progressStore.Flush(GetProgressPath(task.LocalPath));
                    MarkTaskFinished(task.State);
                    return;
                }

                progressStore.Flush(GetProgressPath(task.LocalPath));
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

        private void EnsureDownloadProgressMatchesMode(string localPath, string binPath, DownloadChunkMode chunkMode, int chunkCount)
        {
            if (chunkMode == DownloadChunkMode.SeparatePartFiles)
            {
                DeleteRandomWriteProgress(localPath, binPath);
                return;
            }

            DeletePartProgress(localPath, chunkCount);
        }

        private void DeleteRandomWriteProgress(string localPath, string binPath)
        {
            if (!File.Exists(binPath))
                return;

            if (new FileInfo(binPath).Length == 8)
            {
                if (!File.Exists(GetPartPath(localPath, 0)))
                    progressStore.DeleteFile(binPath);
                return;
            }

            progressStore.DeleteFile(binPath);
            if (File.Exists(localPath))
                TryDeleteFile(localPath);
        }

        private void DeletePartProgress(string localPath, int chunkCount)
        {
            for (var i = 0; i < chunkCount; i++)
                DeletePartFile(localPath, i);

            var progressPath = GetProgressPath(localPath);
            if (File.Exists(progressPath) && new FileInfo(progressPath).Length == 8)
                progressStore.DeleteFile(progressPath);
        }

        private void QueueChunkTasks(DownloadTask task, long totalSize, string localPath, string binPath)
        {
            if (task.ChunkMode == DownloadChunkMode.RandomWriteSingleFile)
                PrepareRandomWriteFiles(localPath, binPath, totalSize, task.ChunkCount);

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

        private void PrepareRandomWriteFiles(string localPath, string binPath, long totalSize, int chunkCount)
        {
            if (!File.Exists(localPath) || new FileInfo(localPath).Length != totalSize)
                CreateFileWithLength(localPath, totalSize);

            if (File.Exists(binPath) && new FileInfo(binPath).Length == chunkCount * 8)
                return;

            progressStore.Remove(binPath);
            CreateFileWithLength(binPath, chunkCount * 8);
        }

        private async UniTask<bool> DownloadTask(DownloadTask task, string localPath, long size, string binPath)
        {
            var downloadByPart = task.ChunkMode == DownloadChunkMode.SeparatePartFiles && task.TotalChunks > 1;
            if (!downloadByPart && !File.Exists(localPath))
            {
                CreateFileWithLength(localPath, 0);
            }
            var downloadByChunk = !downloadByPart && File.Exists(binPath);
            Exception lastException = null;
            for (int i = 0; i < RetryCount; i++)
            {
                if (downloadByPart)
                {
                    if (IsPartMergeStarted(localPath))
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);
                }

                var range = GetDownloadRange(task, localPath, binPath, size, downloadByPart, downloadByChunk);
                if (downloadByPart)
                {
                    if (range.ChunkLength <= 0 || range.IsComplete)
                        return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);
                }
                else if (downloadByChunk)
                {
                    if (range.ChunkLength <= 0 || range.IsComplete)
                        return TryCompleteChunkDownload(localPath, binPath, size);
                }
                else
                {
                    if (range.Start >= size)
                        return true;
                }

                try
                {
                    await SendDownloadRequest(task, binPath, range, downloadByPart, downloadByChunk);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    continue;
                }

                if (downloadByPart)
                    return await TryCompletePartDownload(localPath, size, task.TotalChunks, task.State);
                if (downloadByChunk)
                    return TryCompleteChunkDownload(localPath, binPath, size);
                return true;
            }
            throw new Exception("Download failed after retries", lastException);
        }

        private DownloadRange GetDownloadRange(DownloadTask task, string localPath, string binPath, long size, bool downloadByPart, bool downloadByChunk)
        {
            if (downloadByPart)
            {
                GetChunkRange(size, task.TotalChunks, task.ChunkIndex, out var chunkStart, out var chunkEnd);
                var chunkLength = chunkEnd - chunkStart + 1;
                var downloadPath = GetPartPath(localPath, task.ChunkIndex);
                if (chunkLength <= 0)
                    return new DownloadRange(downloadPath, chunkEnd + 1, chunkEnd, chunkLength, true);

                var chunkLoaded = Math.Min(GetPartFileLength(downloadPath, chunkLength), chunkLength);
                return new DownloadRange(downloadPath, chunkStart + chunkLoaded, chunkEnd, chunkLength, true);
            }

            if (downloadByChunk)
            {
                var fileBin = progressStore.Get(binPath);
                GetChunkRange(size, fileBin.Downloads.Length, task.ChunkIndex, out var chunkStart, out var chunkEnd);
                var chunkLength = chunkEnd - chunkStart + 1;
                if (chunkLength <= 0)
                    return new DownloadRange(localPath, chunkEnd + 1, chunkEnd, chunkLength, true);

                var chunkLoaded = Math.Min(fileBin.Downloads[task.ChunkIndex], chunkLength);
                return new DownloadRange(localPath, chunkStart + chunkLoaded, chunkEnd, chunkLength, true);
            }

            var rangeStart = new FileInfo(localPath).Length;
            return new DownloadRange(localPath, rangeStart, size - 1, 0, rangeStart > 0);
        }

        private async UniTask SendDownloadRequest(DownloadTask task, string binPath, DownloadRange range, bool downloadByPart, bool downloadByChunk)
        {
            using (var request = UnityWebRequest.Get(task.Url))
            {
                request.timeout = TimeOut;
                ConfigureDownloadHandler(request, task, binPath, range, downloadByPart, downloadByChunk);
                request.disposeDownloadHandlerOnDispose = true;
                await request.SendWebRequest().ToUniTask(cancellationToken: task.State.CTS.Token);
                if (request.result != UnityWebRequest.Result.Success)
                    throw new Exception($"Download request failed: {request.error}");
                if (range.RequiresPartialResponse && request.responseCode != 206)
                    throw new Exception($"Range request failed, response code: {request.responseCode}");
            }
        }

        private void ConfigureDownloadHandler(UnityWebRequest request, DownloadTask task, string binPath, DownloadRange range, bool downloadByPart, bool downloadByChunk)
        {
            if (downloadByPart)
            {
                request.SetRequestHeader("Range", $"bytes={range.Start}-{range.End}");
                request.downloadHandler = new DownloadHandlerFile(range.Path, true);
                return;
            }

            if (downloadByChunk)
            {
                request.SetRequestHeader("Range", $"bytes={range.Start}-{range.End}");
                byte[] buffer = new byte[DownloadBufferSize];
                request.downloadHandler = new DownloadHandler(task.LocalPath, range.Start, buffer, progressStore, binPath, task.ChunkIndex, range.ChunkLength);
                return;
            }

            if (range.Start > 0)
                request.SetRequestHeader("Range", $"bytes={range.Start}-{range.End}");
            request.downloadHandler = new DownloadHandlerFile(task.LocalPath, true);
        }

        private void UpdateDownload()
        {
            if (!IsNetworkReachable())
                return;

            while (Volatile.Read(ref activeDownloads) < MaxConcurrentDownloads && pendings.TryPeek(out _))
            {
                Interlocked.Increment(ref activeDownloads);
                Download().Forget();
            }
        }

        private bool IsNetworkReachable()
        {
            var now = Time.unscaledTime;
            if (now < nextNetworkCheckTime)
                return networkReachable;

            nextNetworkCheckTime = now + NetworkCheckInterval;
            networkReachable = Application.internetReachability != NetworkReachability.NotReachable;
            return networkReachable;
        }

        private async UniTask<long> GetFileSize(string url, CancellationToken token)
        {
            Exception lastException = null;
            for (var i = 0; i < RetryCount; i++)
            {
                try
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

                        lastException = new Exception($"File size request failed: {request.error}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }
            throw new Exception("File size request failed after retries", lastException);
        }

        private void CreateFileWithLength(string path, long length)
        {
            using (var file = File.Create(path))
            {
                file.SetLength(length);
            }
        }

        private bool TryCompleteChunkDownload(string localPath, string binPath, long totalSize)
        {
            if (!File.Exists(binPath))
                return File.Exists(localPath) && new FileInfo(localPath).Length >= totalSize;

            var fileBin = progressStore.Get(binPath);
            for (var i = 0; i < fileBin.Downloads.Length; i++)
            {
                GetChunkRange(totalSize, fileBin.Downloads.Length, i, out var chunkStart, out var chunkEnd);
                if (fileBin.Downloads[i] < chunkEnd - chunkStart + 1)
                    return false;
            }

            if (File.Exists(localPath) && new FileInfo(localPath).Length >= totalSize)
            {
                progressStore.Remove(binPath);
                if (File.Exists(binPath))
                    TryDeleteFile(binPath);
                return true;
            }
            else
            {
                progressStore.Flush(binPath);
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
                    progressStore.Flush(GetProgressPath(localPath));
                    DeletePartFile(localPath, i);
                    remainingMergedBytes = 0;
                }
                await output.FlushAsync(token);
            }

            token.ThrowIfCancellationRequested();
            if (File.Exists(localPath))
                TryDeleteFile(localPath);
            File.Move(part0Path, localPath);
            DeleteProgressFile(localPath);
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
                DeleteProgressFile(localPath);
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

        private bool IsPartMergeStarted(string localPath)
        {
            var progressPath = GetProgressPath(localPath);
            return File.Exists(progressPath) && new FileInfo(progressPath).Length == 8 && File.Exists(GetPartPath(localPath, 0));
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

        private string GetProgressPath(string localPath)
        {
            return localPath + ".bin";
        }

        private long ReadMergeOffset(string localPath)
        {
            var progressPath = GetProgressPath(localPath);
            if (progressStore.TryGet(progressPath, out var cachedBin))
            {
                if (cachedBin.Downloads.Length == 1)
                    return cachedBin.Downloads[0];

                progressStore.Remove(progressPath);
            }

            if (!File.Exists(progressPath) || new FileInfo(progressPath).Length != 8)
                return 0;

            var fileBin = progressStore.Get(progressPath);
            return fileBin.Downloads.Length == 1 ? fileBin.Downloads[0] : 0;
        }

        private void UpdateMergeOffset(string localPath, long offset)
        {
            var progressPath = GetProgressPath(localPath);
            if (!progressStore.TryGet(progressPath, out var fileBin) || fileBin.Downloads.Length != 1)
            {
                fileBin = new FileBin { Downloads = new long[1] };
                progressStore.Set(progressPath, fileBin);
            }

            fileBin.Downloads[0] = offset;
        }

        private void DeletePartFiles(string localPath, int chunkCount)
        {
            for (var i = 0; i < chunkCount; i++)
                DeletePartFile(localPath, i);

            DeleteProgressFile(localPath);
        }

        private void DeletePartFile(string localPath, int chunkIndex)
        {
            var partPath = GetPartPath(localPath, chunkIndex);
            if (File.Exists(partPath))
                TryDeleteFile(partPath);
        }

        private void DeleteProgressFile(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
                return;

            var progressPath = GetProgressPath(localPath);
            progressStore.DeleteFile(progressPath);
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
            if (!HasDownloadStateWithToken(state.CTS))
                state.CTS.Dispose();
        }

        private bool HasDownloadStateWithToken(CancellationTokenSource cts)
        {
            for (var i = 0; i < downloadStates.Count; i++)
            {
                if (downloadStates[i].CTS == cts)
                    return true;
            }

            return false;
        }

        private bool HasPreviousDownloadStateWithToken(int index)
        {
            var cts = downloadStates[index].CTS;
            for (var i = 0; i < index; i++)
            {
                if (downloadStates[i].CTS == cts)
                    return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            if (AppInstance.Instance != null)
                AppInstance.Instance.OnUpdate -= UpdateDownload;
            for (var i = 0; i < downloadStates.Count; i++)
            {
                if (HasPreviousDownloadStateWithToken(i))
                    continue;

                downloadStates[i].CTS.Cancel();
                downloadStates[i].CTS.Dispose();
            }
            progressStore.FlushAll();
            downloadStates.Clear();
            progressStore.Clear();
        }
    }
}
