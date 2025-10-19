using System;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 右侧包详情视图 - 只负责UI渲染
    /// </summary>
    public class PFPackageDetailView
    {
        private Vector2 scrollPos;
        private int selectedTab = 0;  // 0=Description, 1=Versions, 2=Dependencies
        private string[] tabNames = { "Description", "Versions", "Dependencies" };

        // 事件处理器
        private PackageDetailEventHandler eventHandler;
        private System.Collections.Generic.List<PackageInfo> allPackages;

        public void SetEventHandler(PackageDetailEventHandler handler, System.Collections.Generic.List<PackageInfo> packages)
        {
            eventHandler = handler;
            allPackages = packages;
        }

        public void Draw(PackageInfo package)
        {
            if (package == null)
            {
                EditorGUILayout.LabelField("Select a package to view details", EditorStyles.centeredGreyMiniLabel);
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
                        DrawDescriptionTab(package);
                        break;
                    case 1:
                        DrawVersionsTab(package);
                        break;
                    case 2:
                        DrawDependenciesTab(package);
                        break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawDescriptionTab(PackageInfo package)
        {
            GUILayout.Label("Description", PFPackageStyles.SectionTitleStyle);
            EditorGUILayout.Space(5);

            GUILayout.Label(package.description, PFPackageStyles.ContentTextStyle);

            EditorGUILayout.Space(10);

            // Links
            if (!string.IsNullOrEmpty(package.documentationUrl))
            {
                if (GUILayout.Button("View documentation", PFPackageStyles.LinkStyle))
                {
                    eventHandler?.HandleOpenUrl(package.documentationUrl);
                }
            }

            if (!string.IsNullOrEmpty(package.changelogUrl))
            {
                if (GUILayout.Button("View changelog", PFPackageStyles.LinkStyle))
                {
                    eventHandler?.HandleOpenUrl(package.changelogUrl);
                }
            }

            EditorGUILayout.Space(10);

            // Author info
            GUILayout.Label("Author", PFPackageStyles.SectionTitleStyle);
            GUILayout.Label(package.author);
            if (!string.IsNullOrEmpty(package.authorUrl))
            {
                if (GUILayout.Button(package.authorUrl, PFPackageStyles.LinkStyle))
                {
                    eventHandler?.HandleOpenUrl(package.authorUrl);
                }
            }
        }

        private void DrawVersionsTab(PackageInfo package)
        {
            GUILayout.Label("Versions", PFPackageStyles.SectionTitleStyle);
            EditorGUILayout.Space(5);

            if (package.versions == null || package.versions.Count == 0)
            {
                GUILayout.Label("No version history available");
                return;
            }

            foreach (var version in package.versions)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        // 版本号
                        string label = version.version;
                        if (version.version == package.version)
                            label += " (Latest)";
                        if (version.isInstalled)
                            label = "● " + label;
                        else
                            label = "○ " + label;

                        GUILayout.Label(label, PFPackageStyles.VersionLabelStyle);

                        GUILayout.FlexibleSpace();

                        // 按钮
                        if (version.isInstalled)
                        {
                            // 当前安装的版本显示 Remove
                            if (GUILayout.Button("Remove", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.VersionButtonOptions))
                            {
                                eventHandler?.HandleUninstallPackage(package);
                            }
                        }
                        else if (package.isInstalled)
                        {
                            // 包已安装但不是这个版本，显示 Update（切换版本）
                            if (GUILayout.Button("Update", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.VersionButtonOptions))
                            {
                                eventHandler?.HandleInstallPackageVersion(package, version.version);
                            }
                        }
                        else
                        {
                            // 包未安装，显示 Install
                            if (GUILayout.Button("Install", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.VersionButtonOptions))
                            {
                                eventHandler?.HandleInstallPackageVersion(package, version.version);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 发布日期
                    GUILayout.Label(version.publishDate, PFPackageStyles.DetailSubtitleStyle);

                    // Changelog
                    if (!string.IsNullOrEmpty(version.changelog))
                    {
                        GUILayout.Label(version.changelog, PFPackageStyles.ContentTextStyle);
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);
            }
        }

        
        private void DrawDependenciesTab(PackageInfo package)
        {
            GUILayout.Label("Dependencies", PFPackageStyles.SectionTitleStyle);
            EditorGUILayout.Space(5);

            if (package.dependencies == null || package.dependencies.Count == 0)
            {
                GUILayout.Label("No dependencies", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var dep in package.dependencies)
                {
                    DrawDependencyItem(dep.Key, dep.Value);
                    EditorGUILayout.Space(3);
                }
            }
        }

        private void DrawDependencyItem(string packageName, string versionRange)
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.BeginVertical();
                {
                    // 获取依赖状态
                    var depStatus = UnityPackageDependencyChecker.CheckDependency(packageName, versionRange);

                    // 第一行：包名和状态
                    EditorGUILayout.BeginHorizontal();
                    {
                        // 包名 (可点击)
                        if (GUILayout.Button($"{packageName}", PFPackageStyles.LinkStyle))
                        {
                            eventHandler?.HandleClickDependency(packageName, allPackages);
                        }

                        GUILayout.FlexibleSpace();

                        // Unity包的特殊操作按钮
                        if (depStatus.isUnityPackage && !depStatus.isAvailable)
                        {
                            if (GUILayout.Button("安装", GUILayout.Width(60)))
                            {
                                UnityPackageDependencyChecker.InstallUnityPackage(packageName, versionRange);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 第二行：版本要求
                    GUILayout.Label($"需求: {versionRange}", PFPackageStyles.DetailSubtitleStyle);

                    // 第三行：状态信息
                    if (!string.IsNullOrEmpty(depStatus.installedVersion))
                    {
                        Color statusColor;
                        if (!depStatus.isVersionCompatible)
                        {
                            statusColor = Color.yellow;
                        }
                        else if (depStatus.source == PackageSource.BuiltIn)
                        {
                            statusColor = new Color(0.5f, 0.8f, 1f); // 浅蓝色表示内置
                        }
                        else
                        {
                            statusColor = Color.green;
                        }

                        GUILayout.Label($"状态: {depStatus.StatusText}", new GUIStyle(PFPackageStyles.DetailSubtitleStyle)
                        {
                            normal = { textColor = statusColor }
                        });
                    }
                    else
                    {
                        Color statusColor = depStatus.isUnityPackage ? Color.cyan : Color.red;
                        GUILayout.Label($"状态: {depStatus.StatusText}", new GUIStyle(PFPackageStyles.DetailSubtitleStyle)
                        {
                            normal = { textColor = statusColor }
                        });
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        public void ResetTab()
        {
            selectedTab = 0;
        }
    }
}
