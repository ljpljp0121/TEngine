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
        public void InstallPackage(string packageName, string version, Action onSuccess, Action<string> onError, Action<float> onProgress = null)
        {
            // 1. 下载 .tgz 文件
            downloader.DownloadPackage(packageName, version,
                onSuccess: (tgzPath) =>
                {
                    try
                    {
                        // 2. 解压到临时目录
                        string extractPath = extractor.ExtractPackage(tgzPath, packageName);

                        // 3. 获取显示名称作为目录名
                        string displayName = FileSystemOperations.ReadPackageDisplayName(extractPath);
                        string targetDirName = !string.IsNullOrEmpty(displayName) ? displayName : packageName;

                        // 4. 安装到目标目录
                        InstallToTargetDirectory(extractPath, targetDirName, packageName);

                        // 5. 刷新 Unity AssetDatabase
                        FileSystemOperations.RefreshAssetDatabase();
                        onSuccess?.Invoke();
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"安装失败: {e.Message}");
                    }
                },
                onError: onError,
                onProgress: onProgress
            );
        }

        /// <summary>
        /// 安装到目标目录
        /// </summary>
        private void InstallToTargetDirectory(string sourcePath, string directoryName, string packageName)
        {
            string targetPath = Path.Combine(installPath, directoryName);

            // 如果已存在，先删除（更新）
            if (Directory.Exists(targetPath))
            {
                FileSystemOperations.DeleteDirectoryWithMeta(targetPath);
            }

            // 复制目录
            FileSystemOperations.CopyDirectory(sourcePath, targetPath);

            // 注册映射关系
            FileSystemOperations.RegisterPackageMapping(packageName, directoryName);
        }

        /// <summary>
        /// 卸载包
        /// </summary>
        public void UninstallPackage(string packageName, Action onSuccess, Action<string> onError)
        {
            try
            {
                // 通过映射表获取实际的目录名
                string directoryName = FileSystemOperations.GetPackageDirectory(packageName, installPath);
                string packagePath = Path.Combine(installPath, directoryName);

                if (!Directory.Exists(packagePath))
                {
                    onError?.Invoke($"包不存在: {packagePath}");
                    return;
                }

                // 删除包目录和meta文件
                FileSystemOperations.DeleteDirectoryWithMeta(packagePath);

                // 移除映射关系
                FileSystemOperations.UnregisterPackageMapping(packageName);

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
            // 通过映射表获取实际的目录名
            string directoryName = FileSystemOperations.GetPackageDirectory(packageName, installPath);
            string packagePath = Path.Combine(installPath, directoryName);
            return Directory.Exists(packagePath);
        }

        /// <summary>
        /// 获取已安装包的版本
        /// </summary>
        public string GetInstalledVersion(string packageName)
        {
            // 通过映射表获取实际的目录名
            string directoryName = FileSystemOperations.GetPackageDirectory(packageName, installPath);
            string packagePath = Path.Combine(installPath, directoryName);
            return FileSystemOperations.ReadPackageVersion(packagePath);
        }
    }
}