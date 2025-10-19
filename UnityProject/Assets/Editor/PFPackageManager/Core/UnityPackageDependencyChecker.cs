using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// Unity包依赖检查器 - 检查Unity官方包和Package Manager兼容性
    /// </summary>
    public static class UnityPackageDependencyChecker
    {
        private static Dictionary<string, string> cachedDependencies = null;

        /// <summary>
        /// 检查依赖包状态
        /// </summary>
        public static DependencyStatus CheckDependency(string packageName, string requiredVersion)
        {
            var status = new DependencyStatus
            {
                packageName = packageName,
                requiredVersion = requiredVersion,
                isAvailable = false,
                isUnityPackage = IsUnityPackage(packageName),
                installedVersion = null,
                canInstall = false
            };

            // 使用Unity PackageManager API查询包信息
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);

            if (packageInfo != null)
            {
                // 找到了包信息，说明包已安装或内置
                status.isAvailable = true;
                status.installedVersion = packageInfo.version;
                status.source = DeterminePackageSource(packageInfo);

                // 检查版本兼容性
                status.isVersionCompatible = IsVersionCompatible(packageInfo.version, requiredVersion);

                // 内置包不能重新安装
                status.canInstall = status.source != PackageSource.BuiltIn;
            }
            else
            {
                // 包未找到，检查是否是Unity官方包
                if (status.isUnityPackage)
                {
                    status.canInstall = true;
                    status.source = PackageSource.PackageManager;
                }
            }

            return status;
        }

        /// <summary>
        /// 确定包来源
        /// </summary>
        private static PackageSource DeterminePackageSource(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            switch (packageInfo.source)
            {
                case UnityEditor.PackageManager.PackageSource.BuiltIn:
                    return PackageSource.BuiltIn;
                case UnityEditor.PackageManager.PackageSource.Registry:
                    return PackageSource.PackageManager;
                case UnityEditor.PackageManager.PackageSource.Git:
                    return PackageSource.Git;
                case UnityEditor.PackageManager.PackageSource.Local:
                case UnityEditor.PackageManager.PackageSource.Embedded:
                    return PackageSource.Local;
                default:
                    return PackageSource.Unknown;
            }
        }

        /// <summary>
        /// 判断是否是Unity官方包
        /// </summary>
        private static bool IsUnityPackage(string packageName)
        {
            return packageName.StartsWith("com.unity.") ||
                   packageName.StartsWith("com.unity.modules.");
        }

        /// <summary>
        /// 从manifest.json读取已安装的包
        /// </summary>
        private static Dictionary<string, string> GetInstalledPackagesFromManifest()
        {
            if (cachedDependencies != null)
                return cachedDependencies;

            cachedDependencies = new Dictionary<string, string>();

            try
            {
                string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

                if (File.Exists(manifestPath))
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = TEngine.Localization.SimpleJSON.JSON.Parse(json);

                    if (manifest["dependencies"] != null)
                    {
                        foreach (var depKey in manifest["dependencies"].Keys)
                        {
                            cachedDependencies[depKey] = manifest["dependencies"][depKey];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"读取manifest.json失败: {e.Message}");
            }

            return cachedDependencies;
        }

        /// <summary>
        /// 检查版本兼容性（简化版本）
        /// </summary>
        private static bool IsVersionCompatible(string installedVersion, string requiredVersion)
        {
            if (string.IsNullOrEmpty(requiredVersion) || requiredVersion == "*")
                return true;

            // 处理常见的版本范围符号
            if (requiredVersion.StartsWith("^"))
            {
                // ^1.0.0 表示 >=1.0.0 且 <2.0.0
                string baseVersion = requiredVersion.Substring(1);
                return CompareVersions(installedVersion, baseVersion) >= 0 &&
                       GetMajorVersion(installedVersion) == GetMajorVersion(baseVersion);
            }
            else if (requiredVersion.StartsWith("~"))
            {
                // ~1.0.0 表示 >=1.0.0 且 <1.1.0
                string baseVersion = requiredVersion.Substring(1);
                return CompareVersions(installedVersion, baseVersion) >= 0 &&
                       GetMajorVersion(installedVersion) == GetMajorVersion(baseVersion) &&
                       GetMinorVersion(installedVersion) == GetMinorVersion(baseVersion);
            }
            else if (requiredVersion.StartsWith(">="))
            {
                // >=1.0.0
                string minVersion = requiredVersion.Substring(2);
                return CompareVersions(installedVersion, minVersion) >= 0;
            }
            else if (requiredVersion.StartsWith("<="))
            {
                // <=1.0.0
                string maxVersion = requiredVersion.Substring(2);
                return CompareVersions(installedVersion, maxVersion) <= 0;
            }
            else if (requiredVersion.StartsWith(">"))
            {
                // >1.0.0
                string minVersion = requiredVersion.Substring(1);
                return CompareVersions(installedVersion, minVersion) > 0;
            }
            else if (requiredVersion.StartsWith("<"))
            {
                // <1.0.0
                string maxVersion = requiredVersion.Substring(1);
                return CompareVersions(installedVersion, maxVersion) < 0;
            }
            else
            {
                // 精确版本
                return installedVersion == requiredVersion;
            }
        }

        /// <summary>
        /// 获取主版本号
        /// </summary>
        private static int GetMajorVersion(string version)
        {
            var parts = version.Split('.');
            if (parts.Length > 0)
                return int.Parse(parts[0]);
            return 0;
        }

        /// <summary>
        /// 获取次版本号
        /// </summary>
        private static int GetMinorVersion(string version)
        {
            var parts = version.Split('.');
            if (parts.Length > 1)
                return int.Parse(parts[1]);
            return 0;
        }

        /// <summary>
        /// 比较版本号
        /// </summary>
        private static int CompareVersions(string v1, string v2)
        {
            return VersionComparer.CompareVersion(v1, v2);
        }

        /// <summary>
        /// 通过Package Manager安装Unity包
        /// </summary>
        public static void InstallUnityPackage(string packageName, string version = null)
        {
            try
            {
                string packageId = string.IsNullOrEmpty(version) ? packageName : $"{packageName}@{version}";

                // 显示确认对话框
                bool confirm = EditorUtility.DisplayDialog(
                    "安装Unity包",
                    $"将通过Unity Package Manager安装包:\n{packageId}\n\n是否继续？",
                    "安装", "取消");

                if (!confirm)
                    return;

                // 异步添加包
                UnityEditor.PackageManager.Client.Add(packageId);

                // 清除缓存，下次重新读取
                cachedDependencies = null;

                Debug.Log($"正在通过Package Manager安装包: {packageId}");

                EditorUtility.DisplayDialog("安装已启动",
                    $"包 {packageId} 的安装已启动。\n请查看Package Manager窗口查看安装进度。",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"安装Unity包失败: {e.Message}");
                EditorUtility.DisplayDialog("安装失败",
                    $"无法安装包 {packageName}:\n{e.Message}",
                    "OK");
            }
        }

        /// <summary>
        /// 打开Package Manager窗口
        /// </summary>
        public static void OpenPackageManagerWindow()
        {
            // 通过菜单打开Package Manager窗口
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        /// <summary>
        /// 刷新缓存（当包发生变化时调用）
        /// </summary>
        public static void RefreshCache()
        {
            cachedDependencies = null;
        }
    }
}