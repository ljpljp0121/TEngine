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

        // public DependencyStatus CheckDependency(string depName, string requiredVersion)
        // {
        //     var status = new DependencyStatus()
        //     {
        //         packageName = depName,
        //         requiredVersion = requiredVersion,
        //     };
        //
        //     var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(depName);
        //
        //     if (packageInfo != null) //包已经在UnityPackageManager注册
        //     {
        //         status.installedVersion = packageInfo.version;
        //         if (packageInfo.source == UnityEditor.PackageManager.PackageSource.Git)
        //             status.source = PackageSource.Git;
        //         else
        //             status.source = PackageSource.UnityPackage;
        //     }
        //
        //     if (depName.IsUnityPackage())
        //         status.source = PackageSource.UnityPackage;
        //     // else 
        //         // status.source 
        //
        //     return status;
        // }

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
            PFLog.Log($"开始下载包 {package.GetDisplayName()}");
            string targetVersion = version ?? package.newestVersion;

            //检查并安装依赖
            if (package.dependencies != null && package.dependencies.Count > 0)
            {
                PFLog.Log($"检查依赖: {package.GetDisplayName()} 需要 {package.dependencies.Count} 个依赖");
            }

            await InstallPackageInternal(package, targetVersion);
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
    }
}