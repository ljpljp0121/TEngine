using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// PF Package Manager 主窗口 - 只负责协调UI组件
    /// </summary>
    public class PFPackageManagerWindow : EditorWindow
    {
        [MenuItem("Window/PFPackageManager",false,1500)]
        public static void ShowWindow()
        {
            var window = GetWindow<PFPackageManagerWindow>("PF Packages");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        // 视图组件
        private PFPackageListView listView;
        private PFPackageDetailView detailView;

        // 核心组件
        private PackageDataManager dataManager;
        private PFPackageOperationManager operationManager;
        private PackageDetailEventHandler detailEventHandler;

        // 配置
        private const string REGISTRY_URL = "https://pfpackage.peifengcoding.com";
        private const string INSTALL_PATH = "Assets/PFPackage";

        private void OnEnable()
        {
            InitializeComponents();
            SetupEventSubscriptions();

            // 加载数据
            dataManager.LoadPackagesFromRegistry();
        }

        /// <summary>
        /// 初始化所有组件
        /// </summary>
        private void InitializeComponents()
        {
            // 初始化视图
            listView = new PFPackageListView();
            detailView = new PFPackageDetailView();

            // 初始化核心组件
            var registryClient = new PFRegistryClient(REGISTRY_URL);
            var installer = new PFPackageInstaller(REGISTRY_URL, INSTALL_PATH);
            dataManager = new PackageDataManager(registryClient, installer);
            operationManager = new PFPackageOperationManager(installer, dataManager.GetAllPackages());
            detailEventHandler = new PackageDetailEventHandler(operationManager, NavigateToDependency);

            // 设置事件处理器
            detailView.SetEventHandler(detailEventHandler, dataManager.GetAllPackages());
        }

        /// <summary>
        /// 设置事件订阅
        /// </summary>
        private void SetupEventSubscriptions()
        {
            // 数据加载完成
            dataManager.OnPackagesLoaded += (packages) =>
            {
                if (packages.Count > 0)
                {
                    listView.SetSelectedPackage(packages[0]);
                }
                Repaint();
            };

            dataManager.OnPackageDetailUpdated += (pkg) => Repaint();

            // 操作状态更新
            operationManager.OnOperationStarted += () => EditorApplication.update += OnEditorUpdate;
            operationManager.OnOperationCompleted += () => EditorApplication.update -= OnEditorUpdate;
            operationManager.OnPackageUpdated += (pkg) => Repaint();
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
                    EditorGUILayout.LabelField(operationManager.CurrentOperation, PFPackageStyles.ProgressTitleStyle);
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), 0.5f, "Processing...");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.BeginHorizontal();
            {
                // 左侧列表
                var packages = dataManager.GetAllPackages();
                listView.Draw(packages);

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
        /// 导航到依赖包
        /// </summary>
        private void NavigateToDependency(PackageInfo package)
        {
            if (package != null)
            {
                listView.SetSelectedPackage(package);
                detailView.ResetTab();
                Repaint();
            }
        }
    }
}
