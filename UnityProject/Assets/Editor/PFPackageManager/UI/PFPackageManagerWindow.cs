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
        private PFPackageOperationManager operationManager;

        // 配置
        private const string REGISTRY_URL = "https://pfpackage.peifengcoding.com";
        private const string INSTALL_PATH = "Assets/PFPackage";

        private void OnEnable()
        {
            listView = new PFPackageListView();
            detailView = new PFPackageDetailView();
            registryClient = new PFRegistryClient(REGISTRY_URL);
            installer = new PFPackageInstaller(REGISTRY_URL, INSTALL_PATH);
            operationManager = new PFPackageOperationManager(installer, allPackages);

            // 订阅 UI 事件
            detailView.OnInstallClicked += (pkg) => operationManager.InstallPackage(pkg);
            detailView.OnRemoveClicked += operationManager.UninstallPackage;
            detailView.OnInstallVersionClicked += (pkg, ver) => operationManager.InstallPackage(pkg, ver);
            detailView.OnDependencyClicked += NavigateToDependency;

            // 订阅操作事件
            operationManager.OnOperationStarted += () => EditorApplication.update += OnEditorUpdate;
            operationManager.OnOperationCompleted += () => EditorApplication.update -= OnEditorUpdate;
            operationManager.OnPackageUpdated += (pkg) => Repaint();

            // 从 Registry 加载包列表
            LoadPackagesFromRegistry();
        }

        private void OnDisable()
        {
            // 取消订阅
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 操作期间自动刷新窗口以显示进度
            Repaint();
        }

        private void OnGUI()
        {
            // 显示进度条
            if (operationManager.IsOperating)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField(operationManager.CurrentOperation, EditorStyles.boldLabel);
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
                            pkg.hasUpdate = PFPackageOperationManager.CompareVersions(pkg.version, pkg.localVersion) > 0;
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
