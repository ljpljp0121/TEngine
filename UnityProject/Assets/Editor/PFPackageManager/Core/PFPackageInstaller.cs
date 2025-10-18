using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace PFPackageManager
{
    /// <summary>
    /// 包安装器 - 负责下载、解压、安装、卸载包
    /// </summary>
    public class PFPackageInstaller
    {
        private readonly string registryUrl;
        private readonly string installPath;
        private readonly string tempPath;

        public PFPackageInstaller(string registryUrl, string installPath)
        {
            this.registryUrl = registryUrl.TrimEnd('/');
            this.installPath = installPath;
            this.tempPath = Path.Combine(Application.temporaryCachePath, "PFPackages");

            // 确保临时目录存在
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        /// <summary>
        /// 安装包（下载 → 解压 → 安装）
        /// </summary>
        public void InstallPackage(string packageName, string version, Action<string> onProgress, Action onSuccess, Action<string> onError)
        {
            onProgress?.Invoke($"正在下载 {packageName}@{version}...");

            // 1. 下载 .tgz 文件
            DownloadPackage(packageName, version,
                onSuccess: (tgzPath) =>
                {
                    onProgress?.Invoke($"正在解压 {packageName}...");

                    try
                    {
                        // 2. 解压到临时目录
                        string extractPath = ExtractPackage(tgzPath, packageName);

                        onProgress?.Invoke($"正在安装 {packageName}...");

                        // 3. 安装到目标目录
                        InstallToTargetDirectory(extractPath, packageName);

                        // 4. 刷新 Unity AssetDatabase
                        AssetDatabase.Refresh();

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
        /// 下载包的 .tgz 文件
        /// </summary>
        private void DownloadPackage(string packageName, string version, Action<string> onSuccess, Action<string> onError)
        {
            // NPM tarball URL: {registry}/{packageName}/-/{packageName}-{version}.tgz
            string tarballUrl = $"{registryUrl}/{packageName}/-/{packageName}-{version}.tgz";
            string savePath = Path.Combine(tempPath, $"{packageName}-{version}.tgz");

            UnityWebRequest request = UnityWebRequest.Get(tarballUrl);
            var operation = request.SendWebRequest();

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

        /// <summary>
        /// 解压 .tgz 文件（tar.gz 格式）
        /// </summary>
        private string ExtractPackage(string tgzPath, string packageName)
        {
            string extractPath = Path.Combine(tempPath, packageName);

            // 删除旧的解压目录
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            Directory.CreateDirectory(extractPath);

            // .tgz 是 tar.gz 格式，需要先 gunzip 再 untar
            // Unity C# 可以用 GZipStream 解压 .gz
            using (FileStream originalFileStream = File.OpenRead(tgzPath))
            using (GZipStream gzipStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            {
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

            // NPM 包解压后会有一个 "package" 目录
            string packageDir = Path.Combine(extractPath, "package");
            return packageDir;
        }

        /// <summary>
        /// 解压 tar 文件（简单实现，仅支持基本的 tar 格式）
        /// </summary>
        private void ExtractTar(string tarPath, string outputPath)
        {
            // TODO: 实现 tar 解压
            // Unity 没有内置 tar 解压，可以：
            // 1. 使用第三方库（如 SharpZipLib）
            // 2. 调用系统命令（tar -xf）
            // 3. 手动解析 tar 格式（复杂）

            // 临时方案：使用系统命令
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Windows 10+ 自带 tar 命令
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"tar 解压失败，退出码: {process.ExitCode}");
                }
            }
            else
            {
                // macOS/Linux
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
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
                Directory.Delete(targetPath, true);
            }

            // 复制目录
            CopyDirectory(sourcePath, targetPath);
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            // 复制所有文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // 递归复制子目录
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(subDir, destDir);
            }
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

                // 删除包目录
                Directory.Delete(packagePath, true);

                // 删除 .meta 文件
                string metaFile = packagePath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }

                // 刷新 Unity AssetDatabase
                AssetDatabase.Refresh();

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
            string packageJsonPath = Path.Combine(packagePath, "package.json");

            if (!File.Exists(packageJsonPath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(packageJsonPath);
                var packageJson = TEngine.Localization.SimpleJSON.JSON.Parse(json);
                return packageJson["version"];
            }
            catch
            {
                return null;
            }
        }
    }
}
