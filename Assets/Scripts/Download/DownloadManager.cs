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

        //void Start()
        //{
        //    StartDownload("http://127.0.0.1/StreamingTool-11.1.4-x64.exe", Application.persistentDataPath + "/StreamingTool-11.1.4-x64.exe", 3);
        //}


        public async UniTask StartDownload(string url, string path, int chunkCount = 1)
        {
            pendings.Enqueue(new DownloadTask
            {
                Url = url,
                LocalPath = path,
                ChunkCount = chunkCount,
                CTS = new CancellationTokenSource()
            });
        }

        private async UniTask Download()
        {
            if (pendings.Count == 0)
                return;
            await semaphoreSlim.WaitAsync();
            try
            {
                if (pendings.Count == 0)
                    return;
                var task = pendings.Dequeue();
                var totalSize = await GetFileSize(task.Url, task.CTS.Token);
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
                            CTS = task.CTS,
                            ChunkIndex = i,
                        });
                }
                else
                {
                    //分块下载
                    if (File.Exists(binFile))
                    {
                        var fileBin = GetFileBin(binFile);
                        var request = UnityWebRequest.Get(task.Url);
                        long chunkLength = totalSize / fileBin.Downloads.Length;
                        long chunkLoaded = fileBin.Downloads[task.ChunkIndex];
                        long rangeStart = chunkLoaded + task.ChunkIndex * (chunkLength + 1);
                        long rangeEnd;
                        if (task.ChunkIndex == fileBin.Downloads.Length - 1)
                            rangeEnd = totalSize;
                        else
                            rangeEnd = rangeStart + chunkLength;
                        request.SetRequestHeader("Range", $"bytes={rangeStart}-{rangeEnd}");
                        Debug.LogError(task.ChunkIndex);
                        request.downloadHandler = new DownloadHandler(task.LocalPath, rangeStart);
                        await request.SendWebRequest().ToUniTask(cancellationToken: task.CTS.Token);
                    }
                    else { }
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
            Download();
        }

        private async UniTask<long> GetFileSize(string url, CancellationToken token)
        {
            using (var request = UnityWebRequest.Head(url))
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: token);
                if (request.result != UnityWebRequest.Result.Success)
                    throw new Exception(request.error);
                return long.Parse(request.GetResponseHeader("Content-Length"));
            }
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
    }
}
