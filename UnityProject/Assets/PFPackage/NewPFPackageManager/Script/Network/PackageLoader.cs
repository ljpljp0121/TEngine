using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PFPackage
{
    public class PackageLoader
    {
        private static string TempPath => Path.Combine(Application.temporaryCachePath, "PFPackages");
        
        /// <summary>
        /// 获取所有包
        /// </summary>
        /// <returns></returns>
        public static async Task<List<PackageInfo>> GetAllPackages()
        {
            string url = $"{PFPackageConfig.I.RegistryUrl}/-/all";
            var request = await GetRequest(url);
            string json = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                json = request.downloadHandler.text;
            }
            request.Dispose();
            PFLog.Log($"获取所有包信息: {json}");
            return PackageJsonParser.ParseAllPackages(json);
        }

        /// <summary>
        /// 获取指定包的详细信息
        /// </summary>
        public static async Task<PackageInfo> GetPackageDetailAsync(string packageName)
        {
            string url = $"{PFPackageConfig.I.RegistryUrl}/{packageName}";
            var request = await GetRequest(url);
            string json = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                json = request.downloadHandler.text;
            }
            request.Dispose();
            PFLog.Log($"获取包 {packageName} 详细信息: {json}");
            return PackageJsonParser.ParsePackageDetail(json);
        }

        /// <summary>
        /// 安装Package到对应目录
        /// </summary>
        public static async Task<bool> DownloadPackage(PackageInfo package, string targetVersion, Action<float> onProgress = null)
        {
            // NPM tarball URL: {registry}/{packageName}/-/{packageName}-{version}.tgz
            string tarballUrl = $"{PFPackageConfig.I.RegistryUrl}/{package.PackageName}/-/{package.PackageName}-{targetVersion}.tgz";
            // 确保临时目录存在
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }
            string tgzPath = Path.Combine(TempPath, $"{package.PackageName}-{targetVersion}.tgz");

            var request = await GetRequest(tarballUrl, onProgress);
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                PFLog.LogError($"网络请求失败: {request.result}");
                request.Dispose();
                return false;
            }   
            else
            {
                //保存压缩包到临时目录
                await File.WriteAllBytesAsync(tgzPath, request.downloadHandler.data);
                //解压到临时目录并删除压缩包
                ExtractPackage(tgzPath, package);
                request.Dispose();
                return true;
            }
        }

        public static async Task InstallToTargetDirectory(PackageInfo package)
        {
            string targetPath = Path.Combine(PFPackageConfig.I.InstallPath, package.GetDisplayName());
            string extractPath = Path.Combine(TempPath, package.PackageName);
            // NPM 包解压后会有一个 "package" 目录
            string sourcePath = Path.Combine(extractPath, "package");
            
            //TODO 如果已存在，先删除旧的,也许后续可以添加过滤机制保留下配置 
            if (Directory.Exists(targetPath))
            {
                FileSystemOperations.DeleteDirectoryWithMeta(targetPath);
            }

            FileSystemOperations.CopyDirectory(sourcePath, targetPath);
        }
        
        /// <summary>
        /// 解压 .tgz 文件（tar.gz 格式）
        /// </summary>
        private static void ExtractPackage(string tgzPath, PackageInfo package)
        {
            string extractPath = Path.Combine(TempPath, package.PackageName);
            
            // 删除旧的解压目录
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            Directory.CreateDirectory(extractPath);
            
            // .tgz 是 tar.gz 格式，需要先 gunzip 再 untar
            // Unity C# 可以用 GZipStream 解压 .gz
            using FileStream originalFileStream = File.OpenRead(tgzPath);
            using GZipStream gzipStream = new GZipStream(originalFileStream, CompressionMode.Decompress);
            string tarPath = tgzPath.Replace(".tgz", ".tar");
            using (FileStream decompressedFileStream = File.Create(tarPath))
            {
                gzipStream.CopyTo(decompressedFileStream);
            }

            // 解压 tar 文件
            ExtractTar(tarPath, extractPath);

            // 删除临时 tar 文件
            File.Delete(tarPath);
        }
        
        /// <summary>
        /// 解压 tar 文件（使用系统命令）
        /// </summary>
        private static void ExtractTar(string tarPath, string outputPath)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Windows 10+ 自带 tar 命令
                Process process = new Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new System.Exception($"tar 解压失败，退出码: {process.ExitCode}");
                }
            }
            else
            {
                // macOS/Linux
                Process process = new Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
        }
        

        /// <summary>
        /// 通用HTTP GET请求
        /// </summary>
        private static async Task<UnityWebRequest> GetRequest(string url, Action<float> onProgress = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            string auth = System.Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{PFPackageConfig.I.UserName}:{PFPackageConfig.I.Password}")
            );
            request.SetRequestHeader("Authorization", $"Basic {auth}");
            request.timeout = 10;

            var operation = request.SendWebRequest();

            // 等待请求完成或取消
            while (!operation.isDone)
            {
                onProgress?.Invoke(request.downloadProgress);
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                PFLog.LogError($"网络请求失败: {request.error}");
                return null;
            }

            return request;
        }
    }
}