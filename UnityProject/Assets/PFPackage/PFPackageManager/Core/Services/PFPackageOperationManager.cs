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

            // 1. 检测依赖冲突
            var conflicts = DependencyConflictDetector.DetectConflicts(package, allPackages, installer);
            if (conflicts.Count > 0)
            {
                string conflictReport = DependencyConflictDetector.GenerateConflictReport(conflicts);
                Debug.LogWarning(conflictReport);

                bool proceed = UnityEditor.EditorUtility.DisplayDialog(
                    "依赖冲突警告",
                    conflictReport + "\n\n是否仍要继续安装？",
                    "继续安装（可能出错）",
                    "取消"
                );

                if (!proceed)
                    return;
            }

            // 2. 检查并安装依赖
            if (package.dependencies != null && package.dependencies.Count > 0)
            {
                Debug.Log($"检查依赖: {package.displayName} 需要 {package.dependencies.Count} 个依赖");

                // 分析依赖状态
                var dependencyAnalysis = AnalyzeDependencies(package.dependencies);

                if (dependencyAnalysis.HasMissingOrIncompatibleDependencies)
                {
                    ShowDependencyDialog(package, dependencyAnalysis, targetVersion);
                    return;
                }
            }

            // 3. 直接安装
            InstallPackageInternal(package.name, targetVersion, package);
        }

        /// <summary>
        /// 分析依赖状态
        /// </summary>
        private DependencyAnalysis AnalyzeDependencies(Dictionary<string, string> dependencies)
        {
            var analysis = new DependencyAnalysis();
            analysis.MissingUnityPackages = new List<string>();
            analysis.MissingThirdPartyPackages = new List<string>();
            analysis.IncompatibleDependencies = new List<string>();

            foreach (var dep in dependencies)
            {
                var status = UnityPackageDependencyChecker.CheckDependency(dep.Key, dep.Value);

                if (!status.isAvailable)
                {
                    if (status.isUnityPackage)
                    {
                        analysis.MissingUnityPackages.Add($"{dep.Key}@{dep.Value}");
                    }
                    else
                    {
                        analysis.MissingThirdPartyPackages.Add($"{dep.Key}@{dep.Value}");
                    }
                }
                else if (!status.isVersionCompatible)
                {
                    analysis.IncompatibleDependencies.Add($"{dep.Key} (需要: {dep.Value}, 已安装: {status.installedVersion})");
                }
            }

            analysis.HasMissingOrIncompatibleDependencies =
                analysis.MissingUnityPackages.Count > 0 ||
                analysis.MissingThirdPartyPackages.Count > 0 ||
                analysis.IncompatibleDependencies.Count > 0;

            return analysis;
        }

        /// <summary>
        /// 显示依赖对话框
        /// </summary>
        private void ShowDependencyDialog(PackageInfo package, DependencyAnalysis analysis, string targetVersion)
        {
            string message = $"{package.displayName} 需要以下依赖：\n\n";

            if (analysis.MissingUnityPackages.Count > 0)
            {
                message += "【Unity官方包 - 未安装】\n";
                message += string.Join("\n", analysis.MissingUnityPackages.Select(p => $"  📦 {p}"));
                message += "\n\n";
            }

            if (analysis.IncompatibleDependencies.Count > 0)
            {
                message += "【版本不匹配】\n";
                message += string.Join("\n", analysis.IncompatibleDependencies.Select(p => $"  ⚠️ {p}"));
                message += "\n\n";
            }

            if (analysis.MissingThirdPartyPackages.Count > 0)
            {
                message += "【第三方包 - 将自动安装】\n";
                message += string.Join("\n", analysis.MissingThirdPartyPackages.Select(p => $"  • {p}"));
                message += "\n\n";
            }

            if (analysis.MissingUnityPackages.Count > 0)
            {
                message += "Unity官方包需要通过Package Manager安装。\n是否继续？";

                int option = EditorUtility.DisplayDialogComplex(
                    "缺少依赖",
                    message,
                    "继续并安装Unity包", // 0
                    "取消", // 1
                    "打开Package Manager" // 2
                );

                if (option == 0)
                {
                    // 继续并安装Unity包
                    InstallMissingUnityPackages(analysis.MissingUnityPackages);
                    InstallWithDependencies(package, targetVersion);
                }
                else if (option == 2)
                {
                    // 打开Package Manager
                    UnityPackageDependencyChecker.OpenPackageManagerWindow();
                }
            }
            else
            {
                message += "是否继续安装？";

                bool confirm = EditorUtility.DisplayDialog(
                    "依赖检查",
                    message,
                    "继续安装",
                    "取消"
                );

                if (confirm)
                {
                    InstallWithDependencies(package, targetVersion);
                }
            }
        }

        /// <summary>
        /// 安装缺失的Unity包
        /// </summary>
        private void InstallMissingUnityPackages(List<string> unityPackages)
        {
            foreach (var package in unityPackages)
            {
                // 解析包名和版本
                var parts = package.Split('@');
                string packageName = parts[0];
                string version = parts.Length > 1 ? parts[1] : null;

                UnityPackageDependencyChecker.InstallUnityPackage(packageName, version);
            }
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
                .Where(dep =>
                {
                    // 跳过Unity官方包（它们通过PackageManager管理）
                    if (dep.Key.StartsWith("com.unity."))
                        return false;

                    // 检查是否已安装
                    if (!installer.IsPackageInstalled(dep.Key))
                    {
                        // 未安装，需要安装
                        return true;
                    }

                    // 已安装，检查版本是否兼容
                    string installedVersion = installer.GetInstalledVersion(dep.Key);
                    string requiredVersion = dep.Value;

                    var versionRange = new VersionRange(requiredVersion);
                    bool isCompatible = versionRange.IsSatisfiedBy(installedVersion);

                    if (!isCompatible)
                    {
                        Debug.LogWarning($"⚠️ 依赖冲突: {dep.Key}");
                        Debug.LogWarning($"   已安装版本: {installedVersion}");
                        Debug.LogWarning($"   需要版本: {requiredVersion}");
                        Debug.LogWarning($"   将尝试升级到兼容版本");
                        return true; // 需要重新安装
                    }

                    Debug.Log($"✓ 依赖 {dep.Key}@{installedVersion} 已满足要求 ({requiredVersion})");
                    return false; // 已安装且兼容，跳过
                })
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
            string depVersion = ResolveVersion(depVersionRange, depPackage);

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
                onProgress: (progress) =>
                {
                    // 显示Unity自带的进度条
                    string progressText = $"快马加鞭下载中 {package.displayName} v{version} - {Math.Round(progress * 100, 1)}%";
                    EditorUtility.DisplayProgressBar("PFPackage", progressText, progress);
                },
                onSuccess: () =>
                {
                    // 清除进度条
                    EditorUtility.ClearProgressBar();

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
                    // 确保在错误时也清除进度条
                    EditorUtility.ClearProgressBar();

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
        /// 解析版本范围，从依赖包的所有版本中选择最佳版本
        /// </summary>
        private string ResolveVersion(string versionRange, PackageInfo depPackage)
        {
            // 如果没有指定范围，返回最新版本
            if (string.IsNullOrEmpty(versionRange) || versionRange == "*")
            {
                return depPackage.version;
            }

            // 收集所有可用版本
            var availableVersions = new System.Collections.Generic.List<string> { depPackage.version };
            if (depPackage.versions != null)
            {
                availableVersions.AddRange(depPackage.versions.Select(v => v.version));
            }

            // 使用版本范围解析器选择最佳版本
            var versionRangeParser = new VersionRange(versionRange);
            string bestVersion = versionRangeParser.SelectBestVersion(availableVersions);

            if (string.IsNullOrEmpty(bestVersion))
            {
                Debug.LogWarning($"无法找到满足 {versionRange} 的版本，将使用最新版本 {depPackage.version}");
                return depPackage.version;
            }

            Debug.Log($"版本范围 '{versionRange}' 解析为: {bestVersion}");
            return bestVersion;
        }

        /// <summary>
        /// 比较版本号（语义化版本）
        /// </summary>
        public static int CompareVersions(string v1, string v2)
        {
            return VersionComparer.CompareVersion(v1, v2);
        }
    }
}