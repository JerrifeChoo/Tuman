using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace TT.Download
{
    public class DownloadManager : MonoBehaviour
    {
        private Queue<DownloadTask> pendings = new Queue<DownloadTask>();
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(5);
        private readonly int RetryCount = 3;
        private CancellationTokenSource test;

        void Start()
        {
            StartDownload("http://127.0.0.1/UnitySetup64-2022.3.62f3.exe", Application.persistentDataPath + "/UnitySetup64-2022.3.62f3.exe", 3);
        }

        public async UniTask StartDownload(string url, string path, int chunkCount = 1)
        {
            test = new CancellationTokenSource();
            pendings.Enqueue(new DownloadTask
            {
                Url = url,
                LocalPath = path,
                ChunkCount = chunkCount,
                CTS = test
            });
        }

        private async UniTask Download()
        {
            if (pendings.Count == 0)
                return;
            await semaphoreSlim.WaitAsync();
            if (pendings.Count == 0)
                return;
            var task = pendings.Dequeue();
            if (task.CTS.IsCancellationRequested)
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
                        var request = UnityWebRequest.Get(task.Url);
                        //分块下载
                        if (File.Exists(binFile))
                        {
                            var fileBin = GetFileBin(binFile);
                            long chunkLength = totalSize / fileBin.Downloads.Length;
                            long chunkLoaded = fileBin.Downloads[task.ChunkIndex];
                            long rangeStart = chunkLoaded + task.ChunkIndex * (chunkLength + 1);
                            long rangeEnd;
                            if (task.ChunkIndex == fileBin.Downloads.Length - 1)
                                rangeEnd = totalSize;
                            else
                                rangeEnd = rangeStart + chunkLength;
                            request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                            request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart);
                            var asyncOp = request.SendWebRequest();
                            asyncOp.ToUniTask(cancellationToken: task.CTS.Token).Forget();
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
                                return;
                            if (rangeStart > 0)
                                request.SetRequestHeader("Range", $"bytes={rangeStart}-{totalSize}");
                            request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart);
                            var asyncOp = request.SendWebRequest();
                            asyncOp.ToUniTask(cancellationToken: task.CTS.Token).Forget();
                        }
                    //}
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private void Update()
        {
            //Application.internetReachability == NetworkReachability.NotReachable
            Download().Forget();
        }

        private async UniTask<long> GetFileSize(string url, CancellationToken token)
        {
            for (var i = 0; i < RetryCount; i++)
                using (var request = UnityWebRequest.Head(url))
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new Exception(request.error);
                    return long.Parse(request.GetResponseHeader("Content-Length"));
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
            test?.Cancel();
            test?.Dispose();
        }
    }
}
