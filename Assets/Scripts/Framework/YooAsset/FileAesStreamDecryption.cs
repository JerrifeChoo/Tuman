// using System;
// using System.IO;
// using System.Text;
// using UnityEngine;
// using YooAsset;

// /// <summary>
// /// 文件流加密方式
// /// </summary>
// public class FileAesStreamEncrypt : IEncryptionServices
// {
//     public EncryptResult Encrypt(EncryptFileInfo fileInfo)
//     {
//         EncryptResult result = new EncryptResult();
//         using (AesEncryptorStream encryptStream = new AesEncryptorStream(
//             fileInfo.FileLoadPath,
//             FileMode.Open,
//             FileAccess.Read,
//             FileShare.Read,
//             Path.GetFileName(fileInfo.FileLoadPath),
//             GameManager.KEY,
//             GameManager.IV))
//         {
//             if (encryptStream.Length > int.MaxValue)
//                 throw new IOException($"File is too large to encrypt in memory: {fileInfo.FileLoadPath}");

//             byte[] encryptedData = new byte[encryptStream.Length];
//             int totalRead = 0;
//             while (totalRead < encryptedData.Length)
//             {
//                 int read = encryptStream.Read(encryptedData, totalRead, encryptedData.Length - totalRead);
//                 if (read <= 0)
//                     break;
//                 totalRead += read;
//             }

//             if (totalRead != encryptedData.Length)
//                 throw new EndOfStreamException($"Failed to read complete encrypted stream. Read:{totalRead} Length:{encryptedData.Length}");

//             result.EncryptedData = encryptedData;
//         }
//         result.Encrypted = true;
//         return result;
//     }
// }

// /// <summary>
// /// 资源文件流解密类
// /// </summary>
// public class FileAesStreamDecryption : IDecryptionServices
// {
    
// /// <summary>
// /// 同步方式获取解密的资源包对象
// /// </summary>
//     DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
//     {
//         DecryptResult decryptResult = new DecryptResult();
//         try
//         {
//             AesDecryptorStream bundleStream = new AesDecryptorStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read, fileInfo.BundleName, GameManager.KEY, GameManager.IV);
//             decryptResult.ManagedStream = bundleStream;
//             //if(bundleStream.Length != 54500)
//                 decryptResult.Result = AssetBundle.LoadFromStream(bundleStream, 0u, 1024);
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError(ex.Message);
//         }
//         return decryptResult;
//     }

//     /// <summary>
//     /// 异步方式获取解密的资源包对象
//     /// </summary>
//     DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
//     {
//         DecryptResult decryptResult = new DecryptResult();
//         try
//         {
//             AesDecryptorStream bundleStream = new AesDecryptorStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read, fileInfo.BundleName, GameManager.KEY, GameManager.IV);
//             decryptResult.ManagedStream = bundleStream;
//             decryptResult.CreateRequest = AssetBundle.LoadFromStreamAsync(bundleStream, 0u, 1024);
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError(ex.Message);
//         }
//         return decryptResult;
//     }

//     /// <summary>
//     /// 后备方式获取解密的资源包
//     /// 注意：当正常解密方法失败后，会触发后备加载！
//     /// 说明：建议通过LoadFromMemory()方法加载资源包作为保底机制。
//     /// </summary>
//     DecryptResult IDecryptionServices.LoadAssetBundleFallback(DecryptFileInfo fileInfo)
//     {
//         Debug.LogError("LoadAssetBundleFallback");
//         byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
//         var assetBundle = AssetBundle.LoadFromMemory(fileData);
//         DecryptResult decryptResult = new DecryptResult();
//         decryptResult.Result = assetBundle;
//         return decryptResult;
//     }

//     /// <summary>
//     /// 获取解密的字节数据
//     /// </summary>
//     byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
//     {
//         throw new System.NotImplementedException();
//     }

//     /// <summary>
//     /// 获取解密的文本数据
//     /// </summary>
//     string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
//     {
//         throw new System.NotImplementedException();
//     }

//     private static uint GetManagedReadBufferSize()
//     {
//         return 1024;
//     }
// }

// /// <summary>
// /// WebGL平台解密类
// /// 注意：WebGL平台支持内存解密
// /// </summary>
// public class TestWebFileMemoryDecryption : IWebDecryptionServices
// {
//     public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
//     {
//         /*
//         byte[] copyData = new byte[fileInfo.FileData.Length];
//         Buffer.BlockCopy(fileInfo.FileData, 0, copyData, 0, fileInfo.FileData.Length);

//         for (int i = 0; i < copyData.Length; i++)
//         {
//             copyData[i] ^= BundleStream.KEY;
//         }

//         WebDecryptResult decryptResult = new WebDecryptResult();
//         decryptResult.Result = AssetBundle.LoadFromMemory(copyData);
//         return decryptResult;
//         */

//         //for (int i = 0; i < fileInfo.FileData.Length; i++)
//         //{
//         //    fileInfo.FileData[i] ^= BundleStream.KEY;
//         //}

//         WebDecryptResult decryptResult = new WebDecryptResult();
//         decryptResult.Result = AssetBundle.LoadFromMemory(fileInfo.FileData);
//         return decryptResult;
//     }
// }