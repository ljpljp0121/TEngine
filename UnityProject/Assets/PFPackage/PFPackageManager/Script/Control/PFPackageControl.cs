using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PFPackage
{
    public class PFPackageControl
    {
        private static PFPackageControl instance;
        public static PFPackageControl I => instance ??= new PFPackageControl();

        /// <summary>
        /// 检查依赖状态
        /// </summary>
        /// <param name="depName">依赖包名</param>
        /// <param name="requiredVersion">需求的版本</param>
        /// <returns>依赖状态</returns>
        public async Task<DependencyStatus> CheckDependency(string depName, string requiredVersion)
        {
            var status = new DependencyStatus()
            {
                packageName = depName,
                requiredVersion = requiredVersion,
                source = PackageSource.Unknown,
                isInstalled = false,
                installedVersion = null,
                isCompatible = false,
            };

            // 1. 检查是否是 PFPackage
            var pfPackage = PFPackageData.I.AllPackages?.Find(p => p.PackageName == depName);
            if (pfPackage != null)
            {
                status.packageName = pfPackage.GetDisplayName();
                status.relatedPackage = pfPackage;
                status.source = PackageSource.PFPackage;
                status.isInstalled = pfPackage.IsInstalled;
                status.installedVersion = pfPackage.localVersion;

                status.isCompatible = CheckVersionCompatible(pfPackage.localVersion, requiredVersion);

                // 解析目标版本
                status.resolvedVersion = pfPackage.ResolveVersion(requiredVersion);

                return status;
            }

            // 2. 检查是否在 Unity PackageManager 中已注册
            var unityPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(depName);
            if (unityPackageInfo != null)
            {
                // 包已经在UnityPackageManager注册
                if (unityPackageInfo.source == UnityEditor.PackageManager.PackageSource.Git)
                    status.source = PackageSource.Git;
                else
                    status.source = PackageSource.UnityPackage;

                status.packageName = unityPackageInfo.displayName;
                status.isInstalled = true;
                status.installedVersion = unityPackageInfo.version;
                
                // 对于Unity包，使用简单的版本比较（因为我们不知道所有可用版本）
                status.isCompatible = CheckVersionCompatible(unityPackageInfo.version, requiredVersion);
                return status;
            }

            // 3. 检查是否是 Unity 官方包（通过包名判断）
            if (depName.IsUnityPackage())
            {
                status.source = PackageSource.UnityPackage;
                status.isInstalled = false;
                status.isCompatible = false;
                return status;
            }

            // 4. 未知来源，未安装
            status.source = PackageSource.Unknown;
            status.isInstalled = false;
            status.isCompatible = false;
            return status;
        }

        /// <summary>
        /// 检查版本是否兼容（使用 VersionRange 进行完整的语义化版本匹配）
        /// </summary>
        /// <param name="installedVersion">已安装版本</param>
        /// <param name="requiredVersion">需求版本</param>
        /// <returns>true表示兼容</returns>
        private bool CheckVersionCompatible(string installedVersion, string requiredVersion)
        {
            if (string.IsNullOrEmpty(installedVersion))
                return false;

            if (string.IsNullOrEmpty(requiredVersion) || requiredVersion == "*")
                return true;

            // 使用 VersionRange 进行完整的范围匹配
            var versionRange = new VersionRange(requiredVersion);
            return versionRange.IsSatisfiedBy(installedVersion);
        }

        /// <summary>
        /// 移除包
        /// </summary>
        public bool UninstallPackage(PackageInfo package)
        {
            PFLog.Log($"卸载包 {package.GetDisplayName()}");
            if (!EditorUtility.DisplayDialog("确认卸载",
                    $"确定要卸载{package.displayName} 吗?", "卸载", "取消"))
                return false;
            string packagePath = package.GetDirectory();
            if (!package.IsPackageInstalled())
            {
                PFLog.LogError($"包不存在: {packagePath}, 卸载失败");
                return false;
            }
            FileSystemOperations.DeleteDirectoryWithMeta(packagePath);
            FileSystemOperations.RefreshAssetDatabase();
            return true;
        }

        /// <summary>
        /// 安装包
        /// </summary>
        public async Task InstallPackage(PackageInfo package, string version = null)
        {
            PFLog.Log($"开始安装包 {package.GetDisplayName()}");
            string targetVersion = version ?? package.newestVersion;

            // 先递归安装所有依赖
            if (package.dependencies != null && package.dependencies.Count > 0)
            {
                PFLog.Log($"开始安装 {package.GetDisplayName()} 的依赖，共 {package.dependencies.Count} 个");
                await InstallDependencies(package.dependencies);
            }

            // 再安装主包
            PFLog.Log($"依赖安装完成，开始下载主包 {package.GetDisplayName()} v{targetVersion}");
            await InstallPackageInternal(package, targetVersion);
            package.RefreshStatus();
        }

        private async Task InstallPackageInternal(PackageInfo package, string targetVersion)
        {
            void OnProgress(float progress)
            {
                string progressText = $"快马加鞭下载中 {package.GetDisplayName()} v{targetVersion} - {Math.Round(progress * 100, 1)}%";
                EditorUtility.DisplayProgressBar("PFPackage", progressText, progress);
            }

            bool success = await PackageLoader.DownloadPackage(package, targetVersion, OnProgress);
            if (success)
            {
                await PackageLoader.InstallToTargetDirectory(package);
                FileSystemOperations.RefreshAssetDatabase();
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// 递归安装所有依赖
        /// </summary>
        /// <param name="dependencies">依赖字典（包名 -> 版本要求）</param>
        private async Task InstallDependencies(Dictionary<string, string> dependencies)
        {
            foreach (var dep in dependencies)
            {
                string depName = dep.Key;
                string requiredVersion = dep.Value;

                PFLog.Log($"处理依赖: {depName}@{requiredVersion}");

                try
                {
                    await InstallDependency(depName, requiredVersion);
                }
                catch (Exception e)
                {
                    // 记录警告但继续安装其他依赖
                    PFLog.LogWarning($"依赖 {depName} 安装失败: {e.Message}，继续安装其他依赖");
                }
            }
        }

        /// <summary>
        /// 安装单个依赖（递归处理子依赖）
        /// </summary>
        /// <param name="depName">依赖包名</param>
        /// <param name="requiredVersion">需求的版本</param>
        private async Task InstallDependency(string depName, string requiredVersion)
        {
            // 1. 查找依赖包的 PackageInfo
            var depPackage = PFPackageData.I.AllPackages?.Find(p => p.PackageName == depName);
            if (depPackage == null)
            {
                // 不是 PFPackage，尝试检查是否是 Unity 包
                if (depName.IsUnityPackage())
                {
                    PFLog.Log($"依赖 {depName} 是 Unity 官方包，跳过安装（假设已安装）");
                    return;
                }

                PFLog.LogWarning($"无法找到依赖包: {depName}，跳过");
                return;
            }

            // 2. 检查是否已安装以及版本是否兼容
            bool isInstalled = depPackage.IsPackageInstalled();
            bool isCompatible = CheckVersionCompatible(depPackage.localVersion, requiredVersion);

            if (isInstalled && isCompatible)
            {
                PFLog.Log($"依赖 {depName} 已安装且版本兼容（{depPackage.localVersion}），跳过");
                return;
            }

            // 3. 解析目标版本
            string targetVersion = depPackage.ResolveVersion(requiredVersion);
            PFLog.Log($"依赖 {depName} 需要安装版本 {targetVersion}（要求: {requiredVersion}）");

            // 4. 递归安装依赖的依赖
            if (depPackage.dependencies != null && depPackage.dependencies.Count > 0)
            {
                PFLog.Log($"开始安装 {depName} 的依赖，共 {depPackage.dependencies.Count} 个");
                await InstallDependencies(depPackage.dependencies);
            }

            // 5. 下载并安装该依赖包
            PFLog.Log($"开始下载依赖包 {depName} v{targetVersion}");
            await InstallPackageInternal(depPackage, targetVersion);
            PFLog.Log($"依赖包 {depName} 安装完成");
        }
    }
}