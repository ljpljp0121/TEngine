using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 包安装器 - 协调下载、解压、安装、卸载操作
    /// </summary>
    public class PFPackageInstaller
    {
        private readonly string installPath;
        private readonly PackageDownloader downloader;
        private readonly PackageExtractor extractor;

        public PFPackageInstaller(string registryUrl, string installPath)
        {
            this.installPath = installPath;
            this.downloader = new PackageDownloader(registryUrl);
            this.extractor = new PackageExtractor();
        }

        /// <summary>
        /// 安装包（下载 → 解压 → 安装）
        /// </summary>
        public void InstallPackage(string packageName, string version, Action<string> onProgress, Action onSuccess, Action<string> onError)
        {
            onProgress?.Invoke($"正在下载 {packageName}@{version}...");

            // 1. 下载 .tgz 文件
            downloader.DownloadPackage(packageName, version,
                onSuccess: (tgzPath) =>
                {
                    onProgress?.Invoke($"正在解压 {packageName}...");

                    try
                    {
                        // 2. 解压到临时目录
                        string extractPath = extractor.ExtractPackage(tgzPath, packageName);

                        onProgress?.Invoke($"正在安装 {packageName}...");

                        // 3. 安装到目标目录
                        InstallToTargetDirectory(extractPath, packageName);

                        // 4. 刷新 Unity AssetDatabase
                        FileSystemOperations.RefreshAssetDatabase();

                        onProgress?.Invoke($"安装完成: {packageName}@{version}");
                        onSuccess?.Invoke();
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"安装失败: {e.Message}");
                    }
                },
                onError: onError
            );
        }

        /// <summary>
        /// 安装到目标目录
        /// </summary>
        private void InstallToTargetDirectory(string sourcePath, string packageName)
        {
            string targetPath = Path.Combine(installPath, packageName);

            // 如果已存在，先删除（更新）
            if (Directory.Exists(targetPath))
            {
                FileSystemOperations.DeleteDirectoryWithMeta(targetPath);
            }

            // 复制目录
            FileSystemOperations.CopyDirectory(sourcePath, targetPath);
        }

        /// <summary>
        /// 卸载包
        /// </summary>
        public void UninstallPackage(string packageName, Action onSuccess, Action<string> onError)
        {
            try
            {
                string packagePath = Path.Combine(installPath, packageName);

                if (!Directory.Exists(packagePath))
                {
                    onError?.Invoke($"包不存在: {packagePath}");
                    return;
                }

                // 删除包目录和meta文件
                FileSystemOperations.DeleteDirectoryWithMeta(packagePath);

                // 刷新 Unity AssetDatabase
                FileSystemOperations.RefreshAssetDatabase();

                onSuccess?.Invoke();
            }
            catch (Exception e)
            {
                onError?.Invoke($"卸载失败: {e.Message}");
            }
        }

        /// <summary>
        /// 检查包是否已安装
        /// </summary>
        public bool IsPackageInstalled(string packageName)
        {
            string packagePath = Path.Combine(installPath, packageName);
            return Directory.Exists(packagePath);
        }

        /// <summary>
        /// 获取已安装包的版本
        /// </summary>
        public string GetInstalledVersion(string packageName)
        {
            string packagePath = Path.Combine(installPath, packageName);
            return FileSystemOperations.ReadPackageVersion(packagePath);
        }
    }
}
