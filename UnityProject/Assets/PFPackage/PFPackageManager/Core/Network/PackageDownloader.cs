using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace PFPackageManager
{
    /// <summary>
    /// 专门负责下载包文件
    /// </summary>
    public class PackageDownloader
    {
        private readonly string registryUrl;
        private readonly string tempPath;

        public PackageDownloader(string registryUrl)
        {
            this.registryUrl = registryUrl.TrimEnd('/');
            this.tempPath = Path.Combine(Application.temporaryCachePath, "PFPackages");

            // 确保临时目录存在
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        /// <summary>
        /// 下载包的 .tgz 文件
        /// </summary>
        public void DownloadPackage(string packageName, string version, Action<string> onSuccess, Action<string> onError, Action<float> onProgress = null)
        {
            // NPM tarball URL: {registry}/{packageName}/-/{packageName}-{version}.tgz
            string tarballUrl = $"{registryUrl}/{packageName}/-/{packageName}-{version}.tgz";
            string savePath = Path.Combine(tempPath, $"{packageName}-{version}.tgz");

            UnityWebRequest request = UnityWebRequest.Get(tarballUrl);
            var operation = request.SendWebRequest();
            
            if (onProgress != null)
            {
                EditorApplication.CallbackFunction progressCallback = null;
                progressCallback = () =>
                {
                    if (!operation.isDone)
                    {
                        // downloadProgress 范围是0-1，转换为0-100百分比
                        float progress = request.downloadProgress;
                        onProgress?.Invoke(progress);
                    }
                    else
                    {
                        // 完成时停止回调
                        EditorApplication.update -= progressCallback;
                    }
                };
                EditorApplication.update += progressCallback;
            }

            operation.completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // 保存到临时目录
                        File.WriteAllBytes(savePath, request.downloadHandler.data);
                        onSuccess?.Invoke(savePath);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"保存文件失败: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"下载失败: {request.error}");
                }
                request.Dispose();
            };
        }
    }
}