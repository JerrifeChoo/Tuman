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
        private const int MaxConcurrentDownloads = 5;
        private const int DownloadBufferSize = 256 * 1024;

        private ConcurrentQueue<DownloadTask> pendings = new ConcurrentQueue<DownloadTask>();
        private List<CancellationTokenSource> taskCTS = new List<CancellationTokenSource>();
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

        public async UniTask StartDownload(string url, string path, int chunkCount = 1)
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
                    //for (int i = 0; i < RetryCount; i++)
                    //{
                    using (var request = UnityWebRequest.Get(task.Url))
                    {
                        //分块下载
                        if (File.Exists(binFile))
                        {
                            var fileBin = GetFileBin(binFile);
                            long chunkLength = totalSize / fileBin.Downloads.Length;
                            long chunkLoaded = fileBin.Downloads[task.ChunkIndex];
                            long rangeStart = chunkLoaded + task.ChunkIndex * (chunkLength + 1);
                            long rangeEnd;
                            if (task.ChunkIndex == fileBin.Downloads.Length - 1)
                                rangeEnd = totalSize - 1;
                            else
                                rangeEnd = rangeStart + chunkLength;
                            request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                            byte[] buffer = new byte[DownloadBufferSize];
                            request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart, buffer);
                            request.disposeDownloadHandlerOnDispose = true;
                            await request.SendWebRequest().ToUniTask(cancellationToken: task.CTS.Token);
                        }
                        else
                        {
                            long rangeStart = 0;
                            if (!File.Exists(tempFile))
                            {
                                var file = File.Create(tempFile);
                                file.Close();
                                file.Dispose();
                            }
                            else
                                rangeStart = new FileInfo(tempFile).Length;
                            if (rangeStart == totalSize)
                            {
                                task.CTS?.Dispose();
                                task.CTS = null;
                                return;
                            }
                            if (rangeStart > 0)
                                request.SetRequestHeader("Range", $"bytes={rangeStart}-{totalSize - 1}");
                            request.downloadHandler = new DownloadHandlerFile(task.LocalPath, true);
                            request.disposeDownloadHandlerOnDispose = true;
                            await request.SendWebRequest().ToUniTask(cancellationToken: task.CTS.Token);
                        }
                        if (request.result != UnityWebRequest.Result.Success)
                            throw new Exception(request.error);
                    }
                    //}
                    task.CTS?.Dispose();
                    taskCTS.Remove(task.CTS);
                }
            }
            catch (Exception ex)
            {
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
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new Exception(request.error);
                    return long.Parse(request.GetResponseHeader("Content-Length"));
                }
            }

            throw new Exception("Timeout");
        }

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
