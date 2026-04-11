// using System;
// using System.IO;
// using System.Security.Cryptography;
// using System.Text;
// using UnityEngine;

// //解密流
// public class AesDecryptorStream : FileStream
// {
//     private const int EncryptHeaderSize = 1024;
//     private string bundleName;
//     private byte[] key;
//     private byte[] iv;
//     private byte[] decryptedHeader;
//     private int decryptedHeaderSize;


//     public AesDecryptorStream(string path, FileMode mode, FileAccess access, FileShare share, string BundleName, byte[] KEY, byte[] IV) : base(path, mode, access, share)
//     {
//         bundleName = BundleName;
//         key = KEY;
//         iv = IV;
//         InitializeHeaderBuffer();
//     }
//     public AesDecryptorStream(string path, FileMode mode) : base(path, mode) { }


//     protected override void Dispose(bool disposing)
//     {
//         base.Dispose(disposing);
//     }

//     public override int Read(byte[] array, int offset, int count)
//     {
//         long readStart = Position;
//         int readSize = base.Read(array, offset, count);
//         if (readSize <= 0 || decryptedHeaderSize <= 0)
//             return readSize;

//         long readEnd = readStart + readSize;
//         long overlapStart = Math.Max(0, readStart);
//         long overlapEnd = Math.Min(decryptedHeaderSize, readEnd);
//         if (overlapStart < overlapEnd)
//         {
//             int copySize = (int)(overlapEnd - overlapStart);
//             int sourceOffset = (int)overlapStart;
//             int targetOffset = offset + (int)(overlapStart - readStart);
//             Buffer.BlockCopy(decryptedHeader, sourceOffset, array, targetOffset, copySize);
//         }
//         return readSize;
//     }

//     private void InitializeHeaderBuffer()
//     {
//         long originalPosition = Position;
//         try
//         {
//             decryptedHeaderSize = (int)Math.Min(EncryptHeaderSize, Length);
//             if (decryptedHeaderSize <= 0)
//                 return;

//             byte[] encryptedHeader = new byte[decryptedHeaderSize];
//             Position = 0;

//             int totalRead = 0;
//             while (totalRead < decryptedHeaderSize)
//             {
//                 int read = base.Read(encryptedHeader, totalRead, decryptedHeaderSize - totalRead);
//                 if (read <= 0)
//                     break;
//                 totalRead += read;
//             }

//             decryptedHeaderSize = totalRead;
//             if (decryptedHeaderSize <= 0)
//                 return;

//             if (decryptedHeaderSize % 16 != 0)
//                 throw new CryptographicException($"Encrypted header size is not AES block aligned. Bundle:{bundleName} Size:{decryptedHeaderSize}");

//             decryptedHeader = new byte[decryptedHeaderSize];
//             using (var aes = Aes.Create())
//             {
//                 aes.Mode = CipherMode.ECB;
//                 aes.Padding = PaddingMode.None;
//                 using (var cryptoTransform = aes.CreateDecryptor(key, iv))
//                 {
//                     int bytesWritten = cryptoTransform.TransformBlock(encryptedHeader, 0, decryptedHeaderSize, decryptedHeader, 0);
//                     cryptoTransform.TransformFinalBlock(new byte[0], 0, 0);
//                     if (bytesWritten != decryptedHeaderSize)
//                         throw new CryptographicException($"Invalid decrypted bytes count. Bundle:{bundleName} Written:{bytesWritten} Need:{decryptedHeaderSize}");
//                 }
//             }
//         }
//         finally
//         {
//             Position = originalPosition;
//         }
//     }
// }

// //加密流
// public class AesEncryptorStream : FileStream
// {
//     private const int EncryptHeaderSize = 1024;
//     private string bundleName;
//     private byte[] key;
//     private byte[] iv;
//     private byte[] encryptedHeader;
//     private int encryptedHeaderSize;

//     public AesEncryptorStream(string path, FileMode mode, FileAccess access, FileShare share, string BundleName, byte[] KEY, byte[] IV) : base(path, mode, access, share)
//     {
//         bundleName = BundleName;
//         key = KEY;
//         iv = IV;
//         InitializeHeaderBuffer();
//     }
//     public AesEncryptorStream(string path, FileMode mode) : base(path, mode) { }

//     public override int Read(byte[] array, int offset, int count)
//     {
//         long readStart = Position;
//         int readSize = base.Read(array, offset, count);
//         if (readSize <= 0 || encryptedHeaderSize <= 0)
//             return readSize;

//         long readEnd = readStart + readSize;
//         long overlapStart = Math.Max(0, readStart);
//         long overlapEnd = Math.Min(encryptedHeaderSize, readEnd);
//         if (overlapStart < overlapEnd)
//         {
//             int copySize = (int)(overlapEnd - overlapStart);
//             int sourceOffset = (int)overlapStart;
//             int targetOffset = offset + (int)(overlapStart - readStart);
//             Buffer.BlockCopy(encryptedHeader, sourceOffset, array, targetOffset, copySize);
//         }
//         return readSize;
//     }

//     private void InitializeHeaderBuffer()
//     {
//         long originalPosition = Position;
//         try
//         {
//             encryptedHeaderSize = (int)Math.Min(EncryptHeaderSize, Length);
//             if (encryptedHeaderSize <= 0)
//                 return;

//             byte[] originalHeader = new byte[encryptedHeaderSize];
//             Position = 0;

//             int totalRead = 0;
//             while (totalRead < encryptedHeaderSize)
//             {
//                 int read = base.Read(originalHeader, totalRead, encryptedHeaderSize - totalRead);
//                 if (read <= 0)
//                     break;
//                 totalRead += read;
//             }

//             encryptedHeaderSize = totalRead;
//             if (encryptedHeaderSize <= 0)
//                 return;

//             if (encryptedHeaderSize % 16 != 0)
//                 throw new CryptographicException($"Original header size is not AES block aligned. Bundle:{bundleName} Size:{encryptedHeaderSize}");

//             encryptedHeader = new byte[encryptedHeaderSize];
//             using (var aes = Aes.Create())
//             {
//                 aes.Mode = CipherMode.ECB;
//                 aes.Padding = PaddingMode.None;
//                 using (var cryptoTransform = aes.CreateEncryptor(key, iv))
//                 {
//                     int bytesWritten = cryptoTransform.TransformBlock(originalHeader, 0, encryptedHeaderSize, encryptedHeader, 0);
//                     cryptoTransform.TransformFinalBlock(new byte[0], 0, 0);
//                     if (bytesWritten != encryptedHeaderSize)
//                         throw new CryptographicException($"Invalid encrypted bytes count. Bundle:{bundleName} Written:{bytesWritten} Need:{encryptedHeaderSize}");
//                 }
//             }
//         }
//         finally
//         {
//             Position = originalPosition;
//         }
//     }
// }
