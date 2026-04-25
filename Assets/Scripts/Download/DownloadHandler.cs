using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace TT.Download
{
    public sealed class DownloadHandler : DownloadHandlerScript
    {
        private const int FileStreamBufferSize = 128 * 1024;

        private FileStream fileStream;
        private Exception writeException;
        private bool isDisposed;

        public DownloadHandler(string filePath, long position, byte[] preallocatedBuffer):base(preallocatedBuffer)
        {
            InitializeFileStream(filePath, position);
        }

        private void InitializeFileStream(string filePath, long position)
        {
            fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, FileStreamBufferSize, FileOptions.SequentialScan);
            fileStream.Seek(position, SeekOrigin.Begin);
        }

        //protected override void ReceiveContentLengthHeader(ulong contentLength)
        //{
        //    base.ReceiveContentLengthHeader(contentLength);
        //}

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0 || dataLength > data.Length || fileStream == null || isDisposed || writeException != null)
                return false;

            try
            {
                fileStream.Write(data, 0, dataLength);
                //Debug.LogError(dataLength);
                return true;
            }
            catch (Exception ex)
            {
                writeException = ex;
                //Debug.LogException(ex);
                return false;
            }
        }

        // 下载完成时调用
        protected override void CompleteContent()
        {
            FlushAndDispose();
            base.CompleteContent();
            //Debug.LogError("CompleteContent");
        }

        private void FlushAndDispose()
        {
            if (isDisposed)
                return;
            var stream = fileStream;
            if (stream == null)
                return;
            try
            {
                if (writeException == null)
                    stream.Flush();
            }
            catch (Exception ex)
            {
                writeException = ex;
                //Debug.LogException(ex);
            }
            finally
            {
                stream.Dispose();
                fileStream = null;
                isDisposed = true;
            }
        }

        // 释放资源
        public override void Dispose()
        {
            FlushAndDispose();
            base.Dispose();
        }
    }
}
