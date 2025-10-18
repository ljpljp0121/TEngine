using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// PF Package Manager 主窗口
    /// </summary>
    public class PFPackageManagerWindow : EditorWindow
    {
        [MenuItem("PF/Package Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<PFPackageManagerWindow>("PF Packages");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        // 视图
        private PFPackageListView listView;
        private PFPackageDetailView detailView;

        // 数据
        private List<PackageInfo> allPackages = new List<PackageInfo>();
        private PFRegistryClient registryClient;
        private PFPackageInstaller installer;

        // 配置
        private const string REGISTRY_URL = "https://pfpackage.peifengcoding.com";
        private const string INSTALL_PATH = "Assets/PFPackage";

        // 进度提示
        private string currentOperation = "";
        private bool isOperating = false;

        private void OnEnable()
        {
            listView = new PFPackageListView();
            detailView = new PFPackageDetailView();
            registryClient = new PFRegistryClient(REGISTRY_URL);
            installer = new PFPackageInstaller(REGISTRY_URL, INSTALL_PATH);

            // 订阅事件
            detailView.OnInstallClicked += (pkg) => InstallPackage(pkg);
            detailView.OnRemoveClicked += UninstallPackage;
            detailView.OnInstallVersionClicked += InstallPackage;
            detailView.OnDependencyClicked += NavigateToDependency;

            // 从 Registry 加载包列表
            LoadPackagesFromRegistry();
        }

        private void OnGUI()
        {
            // 显示进度条
            if (isOperating)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField(currentOperation, EditorStyles.boldLabel);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), 0.5f, "Processing...");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.BeginHorizontal();
            {
                // 左侧列表
                listView.Draw(allPackages);

                // 检测选中变化
                if (GUI.changed)
                {
                    detailView.ResetTab();
                    Repaint();
                }

                // 右侧详情
                EditorGUILayout.BeginVertical();
                {
                    detailView.Draw(listView.SelectedPackage);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        // 操作方法
        private void InstallPackage(PackageInfo package, string version = null)
        {
            string targetVersion = version ?? package.version;

            // 检查并安装依赖
            if (package.dependencies != null && package.dependencies.Count > 0)
            {
                Debug.Log($"检查依赖: {package.displayName} 需要 {package.dependencies.Count} 个依赖");

                bool hasMissingDeps = false;
                foreach (var dep in package.dependencies)
                {
                    string depName = dep.Key;
                    if (!installer.IsPackageInstalled(depName))
                    {
                        hasMissingDeps = true;
                        break;
                    }
                }

                // 如果有缺失的依赖，提示用户
                if (hasMissingDeps)
                {
                    var missingDepsList = package.dependencies
                        .Where(dep => !installer.IsPackageInstalled(dep.Key))
                        .Select(dep => $"• {dep.Key}@{dep.Value}");

                    if (EditorUtility.DisplayDialog("缺少依赖",
                        $"{package.displayName} 需要安装以下依赖：\n\n" +
                        string.Join("\n", missingDepsList),
                        "自动安装依赖", "取消"))
                    {
                        InstallDependencies(package, targetVersion);
                        return; // 依赖安装完成后会继续安装主包
                    }
                    else
                    {
                        return; // 用户取消安装
                    }
                }
            }

            installer.InstallPackage(package.name, targetVersion,
                onProgress: (msg) => Debug.Log(msg),
                onSuccess: () =>
                {
                    Debug.Log($"✓ 安装成功: {package.displayName} v{targetVersion}");

                    // 更新包状态
                    package.isInstalled = true;
                    package.localVersion = targetVersion;
                    package.hasUpdate = CompareVersions(package.version, targetVersion) > 0;

                    // 更新版本列表的 isInstalled 状态
                    foreach (var ver in package.versions)
                    {
                        ver.isInstalled = (ver.version == targetVersion);
                    }

                    Repaint();
                },
                onError: (error) =>
                {
                    Debug.LogError($"✗ 安装失败: {error}");
                    EditorUtility.DisplayDialog("安装失败", error, "OK");
                }
            );
        }

        /// <summary>
        /// 安装依赖包（递归）
        /// </summary>
        private void InstallDependencies(PackageInfo package, string targetVersion)
        {
            var missingDeps = package.dependencies
                .Where(dep => !installer.IsPackageInstalled(dep.Key))
                .ToList();

            if (missingDeps.Count == 0)
            {
                // 所有依赖已安装，继续安装主包
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
                EditorUtility.DisplayDialog("安装失败",
                    $"依赖包 {depName} 不存在", "OK");
                return;
            }

            // 获取符合版本要求的版本
            string depVersion = ResolveVersion(depVersionRange, depPackage.version);

            // 递归安装依赖包
            InstallPackageInternal(depName, depVersion, depPackage,
                onSuccess: () =>
                {
                    // 依赖安装完成，继续安装下一个依赖
                    InstallDependencies(package, targetVersion);
                });
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
        /// 安装包内部实现（支持回调链）
        /// </summary>
        private void InstallPackageInternal(string packageName, string version, PackageInfo package, Action onSuccess = null)
        {
            // 显示进度提示
            isOperating = true;
            currentOperation = $"正在安装 {package.displayName} v{version}...";
            Repaint();

            installer.InstallPackage(packageName, version,
                onProgress: (msg) =>
                {
                    currentOperation = msg;
                    Repaint();
                    Debug.Log(msg);
                },
                onSuccess: () =>
                {
                    Debug.Log($"✓ 安装成功: {package.displayName} v{version}");

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

                    // 隐藏进度提示
                    isOperating = false;
                    currentOperation = "";
                    Repaint();

                    onSuccess?.Invoke();
                },
                onError: (error) =>
                {
                    Debug.LogError($"✗ 安装失败: {error}");

                    // 隐藏进度提示
                    isOperating = false;
                    currentOperation = "";
                    Repaint();

                    EditorUtility.DisplayDialog("安装失败", error, "OK");
                }
            );
        }

        private void UninstallPackage(PackageInfo package)
        {
            if (!EditorUtility.DisplayDialog("确认卸载",
                $"确定要卸载 {package.displayName} 吗？", "卸载", "取消"))
            {
                return;
            }

            // 显示进度提示
            isOperating = true;
            currentOperation = $"正在卸载 {package.displayName}...";
            Repaint();

            installer.UninstallPackage(package.name,
                onSuccess: () =>
                {
                    Debug.Log($"✓ 卸载成功: {package.displayName}");

                    // 更新包状态
                    package.isInstalled = false;
                    package.localVersion = null;
                    package.hasUpdate = false;

                    // 更新版本列表的 isInstalled 状态
                    foreach (var ver in package.versions)
                    {
                        ver.isInstalled = false;
                    }

                    // 隐藏进度提示
                    isOperating = false;
                    currentOperation = "";
                    Repaint();
                },
                onError: (error) =>
                {
                    Debug.LogError($"✗ 卸载失败: {error}");

                    // 隐藏进度提示
                    isOperating = false;
                    currentOperation = "";
                    Repaint();

                    EditorUtility.DisplayDialog("卸载失败", error, "OK");
                }
            );
        }

        /// <summary>
        /// 比较版本号（语义化版本）
        /// </summary>
        private int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < System.Math.Max(parts1.Length, parts2.Length); i++)
            {
                int n1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                int n2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                if (n1 != n2)
                    return n1.CompareTo(n2);
            }

            return 0;
        }

        /// <summary>
        /// 从 Registry 加载包列表
        /// </summary>
        private void LoadPackagesFromRegistry()
        {
            Debug.Log($"正在从 {REGISTRY_URL} 加载包列表...");

            registryClient.GetAllPackages(
                onSuccess: (packages) =>
                {
                    allPackages = packages;
                    Debug.Log($"成功加载 {packages.Count} 个包");

                    // 检测本地已安装的包
                    foreach (var pkg in allPackages)
                    {
                        pkg.isInstalled = installer.IsPackageInstalled(pkg.name);
                        if (pkg.isInstalled)
                        {
                            pkg.localVersion = installer.GetInstalledVersion(pkg.name);
                            pkg.hasUpdate = CompareVersions(pkg.version, pkg.localVersion) > 0;
                        }
                    }

                    // 为每个包加载详细信息（包含版本列表）
                    foreach (var pkg in allPackages)
                    {
                        LoadPackageDetail(pkg);
                    }

                    if (allPackages.Count > 0)
                    {
                        listView.SetSelectedPackage(allPackages[0]);
                    }

                    Repaint();
                },
                onError: (error) =>
                {
                    Debug.LogError($"加载包列表失败: {error}");
                    allPackages = new List<PackageInfo>();
                    Repaint();
                }
            );
        }

        /// <summary>
        /// 导航到依赖包
        /// </summary>
        private void NavigateToDependency(string packageName)
        {
            // 查找包
            var package = allPackages.Find(p => p.name == packageName);
            if (package != null)
            {
                listView.SetSelectedPackage(package);
                detailView.ResetTab();
                Repaint();
            }
            else
            {
                Debug.LogWarning($"依赖包 {packageName} 不在当前包列表中");
            }
        }

        /// <summary>
        /// 加载包详细信息（包含版本历史）
        /// </summary>
        private void LoadPackageDetail(PackageInfo package)
        {
            registryClient.GetPackageDetail(package.name,
                onSuccess: (detailedPkg) =>
                {
                    // 更新包信息
                    package.displayName = detailedPkg.displayName;  // 更新 displayName
                    package.versions = detailedPkg.versions;
                    package.authorUrl = detailedPkg.authorUrl;
                    package.dependencies = detailedPkg.dependencies;

                    // 同步版本列表的 isInstalled 状态
                    if (package.isInstalled && !string.IsNullOrEmpty(package.localVersion))
                    {
                        foreach (var ver in package.versions)
                        {
                            ver.isInstalled = (ver.version == package.localVersion);
                        }
                    }

                    Repaint();
                },
                onError: (error) =>
                {
                    Debug.LogWarning($"加载 {package.name} 详情失败: {error}");
                }
            );
        }
    }
}
