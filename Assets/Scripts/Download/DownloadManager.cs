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
        private List<CancellationTokenSource> taskCTS = new List<CancellationTokenSource>();
        private Dictionary<string, FileBin> fileBins = new Dictionary<string, FileBin>();
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
            StartDownload("http://127.0.0.1/UnitySetup64-2022.3.62f3.exe", Application.persistentDataPath + "/UnitySetup64-2022.3.62f3.exe", 1).Forget();
        }

        public async UniTask<CancellationTokenSource> StartDownload(string url, string path, int chunkCount = 1)
        {
            var downloadCTS = new CancellationTokenSource();
            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(downloadCTS.Token, lifecycleToken);
            taskCTS.Add(linkedCTS);
            pendings.Enqueue(new DownloadTask
            {
                Url = url,
                LocalPath = path,
                ChunkCount = chunkCount,
                CTS = linkedCTS
            });
            return linkedCTS;
        }

        private async UniTask Download()
        {
            if (!pendings.TryDequeue(out var task) || task.Equals(default(DownloadTask)) || task.CTS.IsCancellationRequested)
                return;
            try
            {
                var totalSize = task.TotalSize != 0 ? task.TotalSize : await GetFileSize(task.Url, task.CTS.Token);
                var tempFile = task.LocalPath;
                var binFile = task.LocalPath + ".bin";
                if (task.ChunkCount > 1)
                {
                    if (!File.Exists(tempFile))
                    {
                        var file = File.Create(tempFile);
                        file.SetLength(totalSize);
                        file.Close();
                        file.Dispose();
                    }
                    if (!File.Exists(binFile))
                    {
                        var file = File.Create(binFile);
                        file.SetLength(task.ChunkCount * 8);
                        file.Close();
                        file.Dispose();
                    }
                    for (var i = 0; i < task.ChunkCount; i++)
                        pendings.Enqueue(new DownloadTask
                        {
                            Url = task.Url,
                            LocalPath = task.LocalPath,
                            ChunkCount = 1,
                            TotalSize = totalSize,
                            CTS = task.CTS,
                            ChunkIndex = i,
                        });
                }
                else
                {
                    await DownloadTask(task, tempFile, totalSize, binFile);
                    task.CTS?.Dispose();
                    taskCTS.Remove(task.CTS);
                }
            }
            catch (Exception ex)
            {
                ///TODO 判断类型，是否属于用户取消等
                //task.CTS.Cancel();
                //task.CTS.Dispose();
                //taskCTS.Remove(task.CTS);
                //Debug.LogException(ex);
            }
            finally
            {
                //task.CTS?.Dispose();
                Interlocked.Decrement(ref activeDownloads);
            }
        }

        private async UniTask DownloadTask(DownloadTask task, string localPath, long size, string binPath)
        {
            if (!File.Exists(localPath))
            {
                var file = File.Create(localPath);
                file.Close();
                file.Dispose();
            }
            var downloadByChunk = File.Exists(binPath);
            long rangeStart = 0;
            long rangeEnd = 0;
            if (downloadByChunk)
            {
                var fileBin = GetFileBin(binPath);
                long chunkLength = size / fileBin.Downloads.Length;
                long chunkLoaded = fileBin.Downloads[task.ChunkIndex];
                rangeStart = chunkLoaded + task.ChunkIndex * (chunkLength + 1);
                if (task.ChunkIndex == fileBin.Downloads.Length - 1)
                    rangeEnd = size - 1;
                else
                    rangeEnd = rangeStart + chunkLength;
            }
            else
            {
                rangeStart = new FileInfo(localPath).Length;
                rangeEnd = size - 1;
            }
            for (int i = 0; i < RetryCount; i++)
            {
                using (var request = UnityWebRequest.Get(task.Url))
                {
                    request.timeout = TimeOut;
                    //分块下载
                    if (downloadByChunk)
                    {
                        request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                        byte[] buffer = new byte[DownloadBufferSize];
                        ///TODO 需要处理下载过程中的fileBin更新写入以及下载完成时的清理等
                        request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart, buffer);
                    }
                    else
                    {
                        if (rangeStart == size)
                        {
                            return;
                        }
                        if (rangeStart > 0)
                            request.SetRequestHeader("Range", $"bytes={rangeStart}-{size - 1}");
                        request.downloadHandler = new DownloadHandlerFile(task.LocalPath, true);
                    }
                    request.disposeDownloadHandlerOnDispose = true;
                    await request.SendWebRequest().ToUniTask(cancellationToken: task.CTS.Token);
                    if (request.result == UnityWebRequest.Result.Success)
                        return;
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
                        return long.Parse(request.GetResponseHeader("Content-Length"));
                }
            }
            throw new Exception("Timeout");
        }

        ///TODO 添加缓存
        private FileBin GetFileBin(string path)
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

        private void OnDestroy()
        {
            if (AppInstance.Instance != null)
                AppInstance.Instance.OnUpdate -= UpdateDownload;
            foreach (var cts in taskCTS)
            {
                cts.Cancel();
                cts.Dispose();
            }
            taskCTS.Clear();
        }
    }
}
