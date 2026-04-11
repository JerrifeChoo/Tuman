using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;

//解密流
public class AesDecryptorStream : FileStream
{
    private readonly byte[] Key;
    private readonly byte[] Iv;
    private readonly int HeaderSize;
    private byte[] Header;
    private int DecryptedHeaderSize;

    public AesDecryptorStream(string path, FileMode mode, FileAccess access, FileShare share, byte[] key, byte[] iv, int headerSize = 1024) : base(path, mode, access, share)
    {
        Key = key;
        Iv = iv;
        HeaderSize = headerSize;
        InitializeHeaderBuffer();
    }

    public override int Read(byte[] array, int offset, int count)
    {
        long readStart = Position;
        int readSize = base.Read(array, offset, count);
        AesStreamOverlay.ApplyHeader(Header, DecryptedHeaderSize, readStart, readSize, array, offset);
        return readSize;
    }

    private void InitializeHeaderBuffer()
    {
        long originalPosition = Position;
        byte[] rentedEncrypted = null;
        try
        {
            DecryptedHeaderSize = (int)Math.Min(HeaderSize, Length);
            if (DecryptedHeaderSize <= 0)
                return;

            rentedEncrypted = ArrayPool<byte>.Shared.Rent(DecryptedHeaderSize);
            Position = 0;

            int totalRead = 0;
            while (totalRead < DecryptedHeaderSize)
            {
                int read = base.Read(rentedEncrypted, totalRead, DecryptedHeaderSize - totalRead);
                if (read <= 0)
                    break;
                totalRead += read;
            }

            DecryptedHeaderSize = totalRead;
            if (DecryptedHeaderSize <= 0)
                return;

            if (DecryptedHeaderSize % 16 != 0)
                throw new CryptographicException($"Encrypted header size is not AES block aligned. Bundle Size:{DecryptedHeaderSize}");

            Header = new byte[DecryptedHeaderSize];
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (var cryptoTransform = aes.CreateDecryptor(Key, Iv))
                {
                    int bytesWritten = cryptoTransform.TransformBlock(rentedEncrypted, 0, DecryptedHeaderSize, Header, 0);
                    cryptoTransform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (bytesWritten != DecryptedHeaderSize)
                        throw new CryptographicException($"Invalid decrypted bytes count. Bundle Written:{bytesWritten} Need:{DecryptedHeaderSize}");
                }
            }
        }
        finally
        {
            if (rentedEncrypted != null)
                ArrayPool<byte>.Shared.Return(rentedEncrypted);
            Position = originalPosition;
        }
    }
}

//加密流
public class AesEncryptorStream : FileStream
{
    private readonly byte[] Key;
    private readonly byte[] Iv;
    private readonly int HeaderSize;
    private byte[] Header;
    private int EncryptedHeaderSize;

    public AesEncryptorStream(string path, FileMode mode, FileAccess access, FileShare share, byte[] key, byte[] iv, int headSize = 1024) : base(path, mode, access, share)
    {
        Key = key;
        Iv = iv;
        HeaderSize = headSize;
        InitializeHeaderBuffer();
    }

    public override int Read(byte[] array, int offset, int count)
    {
        long readStart = Position;
        int readSize = base.Read(array, offset, count);
        AesStreamOverlay.ApplyHeader(Header, EncryptedHeaderSize, readStart, readSize, array, offset);
        return readSize;
    }

    private void InitializeHeaderBuffer()
    {
        long originalPosition = Position;
        byte[] rentedPlain = null;
        try
        {
            EncryptedHeaderSize = (int)Math.Min(HeaderSize, Length);
            if (EncryptedHeaderSize <= 0)
                return;

            rentedPlain = ArrayPool<byte>.Shared.Rent(EncryptedHeaderSize);
            Position = 0;

            int totalRead = 0;
            while (totalRead < EncryptedHeaderSize)
            {
                int read = base.Read(rentedPlain, totalRead, EncryptedHeaderSize - totalRead);
                if (read <= 0)
                    break;
                totalRead += read;
            }

            EncryptedHeaderSize = totalRead;
            if (EncryptedHeaderSize <= 0)
                return;

            if (EncryptedHeaderSize % 16 != 0)
                throw new CryptographicException($"Original header size is not AES block aligned. Bundle Size:{EncryptedHeaderSize}");

            Header = new byte[EncryptedHeaderSize];
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using (var cryptoTransform = aes.CreateEncryptor(Key, Iv))
                {
                    int bytesWritten = cryptoTransform.TransformBlock(rentedPlain, 0, EncryptedHeaderSize, Header, 0);
                    cryptoTransform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (bytesWritten != EncryptedHeaderSize)
                        throw new CryptographicException($"Invalid encrypted bytes count. Bundle Written:{bytesWritten} Need:{EncryptedHeaderSize}");
                }
            }
        }
        finally
        {
            if (rentedPlain != null)
                ArrayPool<byte>.Shared.Return(rentedPlain);
            Position = originalPosition;
        }
    }
}

/// <summary>
/// 将 base.Read 得到的缓冲区中属于「文件头」区间的那一段替换为内存中的头副本。
/// </summary>
internal static class AesStreamOverlay
{
    internal static void ApplyHeader(byte[] header, int headerByteCount, long readStart, int readSize, byte[] buffer, int bufferOffset)
    {
        if (readSize <= 0 || headerByteCount <= 0 || header == null)
            return;
        // 常见热路径：连续读取已超过头部，无需 BlockCopy
        if (readStart >= headerByteCount)
            return;

        long readEnd = readStart + readSize;
        long overlapEnd = readEnd < headerByteCount ? readEnd : headerByteCount;
        int copySize = (int)(overlapEnd - readStart);
        Buffer.BlockCopy(header, (int)readStart, buffer, bufferOffset, copySize);
    }
}
