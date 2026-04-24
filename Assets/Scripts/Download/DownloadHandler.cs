using System.IO;
using UnityEngine.Networking;

namespace TT.Download
{
    public class DownloadHandler : DownloadHandlerScript
    {
        private FileStream fileStream;

        public DownloadHandler(string filePath, long position)
        {
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fileStream.Seek(position, SeekOrigin.Begin);
        }

        //protected override void ReceiveContentLengthHeader(ulong contentLength)
        //{
        //    base.ReceiveContentLengthHeader(contentLength);
        //}

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return false;
            fileStream.Write(data, 0, dataLength);
            return true;
        }

        // 下载完成时调用
        protected override void CompleteContent()
        {
            // 关闭文件流，确保所有数据都已写入磁盘
            fileStream?.Close();
            base.CompleteContent();
        }

        // 释放资源
        public override void Dispose()
        {
            base.Dispose();
            fileStream?.Dispose();
        }
    }
}
