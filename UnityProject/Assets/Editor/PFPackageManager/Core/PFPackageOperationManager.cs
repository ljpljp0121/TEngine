using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 包操作管理器 - 负责包的安装/卸载/更新逻辑
    /// </summary>
    public class PFPackageOperationManager
    {
        private readonly PFPackageInstaller installer;
        private readonly List<PackageInfo> allPackages;

        // 进度状态
        public bool IsOperating { get; private set; }
        public string CurrentOperation { get; private set; }

        // 事件
        public event Action OnOperationStarted;
        public event Action OnOperationCompleted;
        public event Action<PackageInfo> OnPackageUpdated;

        public PFPackageOperationManager(PFPackageInstaller installer, List<PackageInfo> allPackages)
        {
            this.installer = installer;
            this.allPackages = allPackages;
            IsOperating = false;
            CurrentOperation = "";
        }

        /// <summary>
        /// 安装包（入口方法）
        /// </summary>
        public void InstallPackage(PackageInfo package, string version = null)
        {
            Debug.Log($"[InstallPackage] 开始安装 {package.name}");
            string targetVersion = version ?? package.version;

            // 检查并安装依赖
            if (package.dependencies != null && package.dependencies.Count > 0)
            {
                Debug.Log($"检查依赖: {package.displayName} 需要 {package.dependencies.Count} 个依赖");

                var missingDeps = package.dependencies
                    .Where(dep => !installer.IsPackageInstalled(dep.Key))
                    .ToList();

                if (missingDeps.Count > 0)
                {
                    var missingDepsList = missingDeps.Select(dep => $"• {dep.Key}@{dep.Value}");

                    if (EditorUtility.DisplayDialog("缺少依赖",
                        $"{package.displayName} 需要安装以下依赖：\n\n" +
                        string.Join("\n", missingDepsList),
                        "自动安装依赖", "取消"))
                    {
                        InstallWithDependencies(package, targetVersion);
                        return;
                    }
                    else
                    {
                        return; // 用户取消安装
                    }
                }
            }

            // 直接安装
            InstallPackageInternal(package.name, targetVersion, package);
        }

        /// <summary>
        /// 卸载包
        /// </summary>
        public void UninstallPackage(PackageInfo package)
        {
            if (!EditorUtility.DisplayDialog("确认卸载",
                $"确定要卸载 {package.displayName} 吗？", "卸载", "取消"))
            {
                return;
            }

            SetOperationState(true, $"正在卸载 {package.displayName}...");

            installer.UninstallPackage(package.name,
                onSuccess: () =>
                {
                    Debug.Log($"✓ 卸载成功: {package.displayName}");

                    // 更新包状态
                    package.isInstalled = false;
                    package.localVersion = null;
                    package.hasUpdate = false;

                    // 更新版本列表的 isInstalled 状态
                    if (package.versions != null)
                    {
                        foreach (var ver in package.versions)
                        {
                            ver.isInstalled = false;
                        }
                    }

                    SetOperationState(false, "");
                    OnPackageUpdated?.Invoke(package);
                },
                onError: (error) =>
                {
                    Debug.LogError($"✗ 卸载失败: {error}");
                    SetOperationState(false, "");
                    EditorUtility.DisplayDialog("卸载失败", error, "OK");
                }
            );
        }

        /// <summary>
        /// 安装依赖包（递归）
        /// </summary>
        private void InstallWithDependencies(PackageInfo package, string targetVersion)
        {
            var missingDeps = package.dependencies
                .Where(dep => !installer.IsPackageInstalled(dep.Key))
                .ToList();

            if (missingDeps.Count == 0)
            {
                // 所有依赖已安装，安装主包
                InstallPackageInternal(package.name, targetVersion, package);
                return;
            }

            // 安装第一个缺失的依赖
            var firstDep = missingDeps[0];
            string depName = firstDep.Key;
            string depVersionRange = firstDep.Value;

            Debug.Log($"正在安装依赖: {depName}@{depVersionRange}");

            // 查找依赖包
            var depPackage = allPackages.Find(p => p.name == depName);
            if (depPackage == null)
            {
                Debug.LogError($"依赖包 {depName} 不在当前包列表中，无法自动安装");
                EditorUtility.DisplayDialog("安装失败", $"依赖包 {depName} 不存在", "OK");
                return;
            }

            // 获取符合版本要求的版本
            string depVersion = ResolveVersion(depVersionRange, depPackage.version);

            // 递归安装依赖包
            InstallPackageInternal(depName, depVersion, depPackage,
                onSuccess: () =>
                {
                    // 依赖安装完成，继续安装下一个依赖
                    InstallWithDependencies(package, targetVersion);
                });
        }

        /// <summary>
        /// 安装包内部实现（支持回调链）
        /// </summary>
        private void InstallPackageInternal(string packageName, string version, PackageInfo package, Action onSuccess = null)
        {
            SetOperationState(true, $"正在安装 {package.displayName} v{version}...");

            installer.InstallPackage(packageName, version,
                onProgress: (msg) =>
                {
                    CurrentOperation = msg;
                    Debug.Log(msg);
                },
                onSuccess: () =>
                {
                    Debug.Log($"安装成功: {package.displayName} v{version}");

                    // 更新包状态
                    package.isInstalled = true;
                    package.localVersion = version;
                    package.hasUpdate = CompareVersions(package.version, version) > 0;

                    // 更新版本列表的 isInstalled 状态
                    if (package.versions != null)
                    {
                        foreach (var ver in package.versions)
                        {
                            ver.isInstalled = (ver.version == version);
                        }
                    }

                    SetOperationState(false, "");
                    OnPackageUpdated?.Invoke(package);
                    onSuccess?.Invoke();
                },
                onError: (error) =>
                {
                    Debug.LogError($"✗ 安装失败: {error}");
                    SetOperationState(false, "");
                    EditorUtility.DisplayDialog("安装失败", error, "OK");
                }
            );
        }

        /// <summary>
        /// 设置操作状态
        /// </summary>
        private void SetOperationState(bool isOperating, string operation)
        {
            IsOperating = isOperating;
            CurrentOperation = operation;

            if (isOperating)
            {
                OnOperationStarted?.Invoke();
            }
            else
            {
                OnOperationCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 解析版本范围（简单实现）
        /// </summary>
        private string ResolveVersion(string versionRange, string latestVersion)
        {
            // 移除版本前缀符号 ^, ~, >, <, =
            string version = versionRange.TrimStart('^', '~', '>', '<', '=', ' ');

            // TODO: 实现完整的语义化版本匹配
            // 目前简单返回最新版本
            return latestVersion;
        }

        /// <summary>
        /// 比较版本号（语义化版本）
        /// </summary>
        public static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int n1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int n2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (n1 != n2)
                    return n1.CompareTo(n2);
            }

            return 0;
        }
    }
}
