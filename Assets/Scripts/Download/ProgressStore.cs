using System;
using System.Collections.Concurrent;
using System.IO;

namespace TT.Download
{
    public sealed class ProgressStore
    {
        private readonly ConcurrentDictionary<string, FileBin> fileBins = new ConcurrentDictionary<string, FileBin>();

        public FileBin Get(string path)
        {
            return fileBins.GetOrAdd(path, ReadFileBin);
        }

        public bool TryGet(string path, out FileBin fileBin)
        {
            return fileBins.TryGetValue(path, out fileBin);
        }

        public void Set(string path, FileBin fileBin)
        {
            fileBins[path] = fileBin;
        }

        public void Remove(string path)
        {
            fileBins.TryRemove(path, out _);
        }

        public void AddDownloadedBytes(string path, int chunkIndex, int bytesWritten, long chunkLength)
        {
            if (bytesWritten <= 0)
                return;

            var fileBin = Get(path);
            var downloaded = Math.Min(fileBin.Downloads[chunkIndex] + bytesWritten, chunkLength);
            if (downloaded != fileBin.Downloads[chunkIndex])
                fileBin.Downloads[chunkIndex] = downloaded;
        }

        public void Flush(string path)
        {
            if (string.IsNullOrEmpty(path) || !fileBins.TryGetValue(path, out var fileBin))
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

        public void FlushAll()
        {
            foreach (var path in fileBins.Keys)
                Flush(path);
        }

        public void DeleteFile(string path)
        {
            Remove(path);
            if (File.Exists(path))
                TryDeleteFile(path);
        }

        public void Clear()
        {
            fileBins.Clear();
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
                for (int i = 0; i < bytes.Length; i += 8)
                {
                    fileBin.Downloads[index] = BitConverter.ToInt64(bytes, i);
                    index++;
                }
                return fileBin;
            }
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
    }
}
