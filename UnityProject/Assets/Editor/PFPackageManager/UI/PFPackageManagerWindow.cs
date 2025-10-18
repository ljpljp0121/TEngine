using System.Collections.Generic;
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

        // 配置
        private const string REGISTRY_URL = "https://pfpackage.peifengcoding.com";

        private void OnEnable()
        {
            listView = new PFPackageListView();
            detailView = new PFPackageDetailView();
            registryClient = new PFRegistryClient(REGISTRY_URL);

            // 订阅事件
            detailView.OnInstallClicked += (pkg) => InstallPackage(pkg);
            detailView.OnRemoveClicked += UninstallPackage;
            detailView.OnInstallVersionClicked += InstallPackage;

            // 从 Registry 加载包列表
            LoadPackagesFromRegistry();
        }

        private void OnGUI()
        {
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
            Debug.Log($"Installing {package.name} {version ?? package.version}");
            // TODO: 实现安装逻辑
        }

        private void UninstallPackage(PackageInfo package)
        {
            Debug.Log($"Uninstalling {package.name}");
            // TODO: 实现卸载逻辑
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
