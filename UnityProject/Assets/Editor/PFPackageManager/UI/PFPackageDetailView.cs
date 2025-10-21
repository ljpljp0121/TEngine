using System;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 右侧包详情视图 - 只负责UI渲染和Tab管理
    /// </summary>
    public class PFPackageDetailView
    {
        private Vector2 scrollPos;
        private int selectedTab = 0;  // 0=Description, 1=Versions, 2=Dependencies
        private string[] tabNames = { "Description", "Versions", "Dependencies" };

        // 事件处理器
        private PackageDetailEventHandler eventHandler;
        private System.Collections.Generic.List<PackageInfo> allPackages;

        // Tab 组件
        private DescriptionTab descriptionTab;
        private VersionsTab versionsTab;
        private DependenciesTab dependenciesTab;

        public void SetEventHandler(PackageDetailEventHandler handler, System.Collections.Generic.List<PackageInfo> packages)
        {
            eventHandler = handler;
            allPackages = packages;

            // 初始化 Tab 组件
            descriptionTab = new DescriptionTab(eventHandler);
            versionsTab = new VersionsTab(eventHandler);
            dependenciesTab = new DependenciesTab(eventHandler, allPackages);
        }

        public void Draw(PackageInfo package)
        {
            if (package == null)
            {
                EditorGUILayout.LabelField("Select a package to view details", PFPackageStyles.CenteredGreyMiniLabelStyle);
                return;
            }

            DrawPackageHeader(package);
            DrawPackageTabs();
            DrawPackageContent(package);
        }

        private void DrawPackageHeader(PackageInfo package)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    // 包名
                    GUILayout.Label(package.displayName, PFPackageStyles.DetailTitleStyle);

                    // 作者和版本
                    string versionText = $"by {package.author}    Version {package.version}";
                    if (package.isInstalled && !string.IsNullOrEmpty(package.localVersion))
                    {
                        versionText += $"  (Installed: {package.localVersion})";
                    }
                    GUILayout.Label(versionText, PFPackageStyles.DetailSubtitleStyle);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 操作按钮
                if (package.isInstalled)
                {
                    // 如果有更新，先显示 Update to {version} 按钮
                    if (package.hasUpdate)
                    {
                        if (GUILayout.Button($"Update to {package.version}", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.HeaderButtonOptions))
                        {
                            eventHandler?.HandleInstallPackage(package);
                        }
                    }

                    // 然后显示 Remove 按钮
                    if (GUILayout.Button("Remove", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.HeaderButtonOptions))
                    {
                        eventHandler?.HandleUninstallPackage(package);
                    }
                }
                else
                {
                    if (GUILayout.Button("Install", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.HeaderButtonOptions))
                    {
                        eventHandler?.HandleInstallPackage(package);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawPackageTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(5);
        }

        private void DrawPackageContent(PackageInfo package)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            {
                switch (selectedTab)
                {
                    case 0:
                        descriptionTab?.Draw(package);
                        break;
                    case 1:
                        versionsTab?.Draw(package);
                        break;
                    case 2:
                        dependenciesTab?.Draw(package);
                        break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public void ResetTab()
        {
            selectedTab = 0;
        }
    }
}