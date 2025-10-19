using System;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 包详情视图的事件处理器 - 负责处理所有按钮点击和交互
    /// </summary>
    public class PackageDetailEventHandler
    {
        private readonly PFPackageOperationManager operationManager;
        private readonly Action<PackageInfo> onNavigateToDependency;

        public PackageDetailEventHandler(
            PFPackageOperationManager operationManager,
            Action<PackageInfo> onNavigateToDependency)
        {
            this.operationManager = operationManager;
            this.onNavigateToDependency = onNavigateToDependency;
        }

        /// <summary>
        /// 处理安装包请求
        /// </summary>
        public void HandleInstallPackage(PackageInfo package)
        {
            operationManager.InstallPackage(package);
        }

        /// <summary>
        /// 处理卸载包请求
        /// </summary>
        public void HandleUninstallPackage(PackageInfo package)
        {
            operationManager.UninstallPackage(package);
        }

        /// <summary>
        /// 处理安装指定版本包请求
        /// </summary>
        public void HandleInstallPackageVersion(PackageInfo package, string version)
        {
            operationManager.InstallPackage(package, version);
        }

        /// <summary>
        /// 处理点击依赖包
        /// </summary>
        public void HandleClickDependency(string packageName, System.Collections.Generic.List<PackageInfo> allPackages)
        {
            // 检查是否是Unity官方包
            if (packageName.StartsWith("com.unity."))
            {
                // 打开PackageManager并选中该包
                OpenPackageManagerAndSelectPackage(packageName);
            }
            else
            {
                // 查找第三方包
                var package = allPackages.Find(p => p.name == packageName);
                if (package != null)
                {
                    onNavigateToDependency?.Invoke(package);
                }
                else
                {
                    EditorUtility.DisplayDialog("未找到依赖包", $"依赖包 {packageName} 不在当前包列表中", "OK");
                }
            }
        }

        /// <summary>
        /// 打开PackageManager并选中指定包
        /// </summary>
        private void OpenPackageManagerAndSelectPackage(string packageName)
        {
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        /// <summary>
        /// 处理打开外部链接
        /// </summary>
        public void HandleOpenUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }
    }
}