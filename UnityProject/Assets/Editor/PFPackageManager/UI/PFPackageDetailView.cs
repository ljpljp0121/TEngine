using System;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 右侧包详情视图
    /// </summary>
    public class PFPackageDetailView
    {
        private Vector2 scrollPos;
        private int selectedTab = 0;  // 0=Description, 1=Versions, 2=Dependencies
        private string[] tabNames = { "Description", "Versions", "Dependencies" };

        public event Action<PackageInfo> OnInstallClicked;
        public event Action<PackageInfo> OnRemoveClicked;
        public event Action<PackageInfo, string> OnInstallVersionClicked;

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
                    GUILayout.Label($"by {package.author}    Version {package.version}", PFPackageStyles.DetailSubtitleStyle);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 操作按钮
                if (package.isInstalled)
                {
                    if (GUILayout.Button("Remove", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.HeaderButtonOptions))
                    {
                        OnRemoveClicked?.Invoke(package);
                    }
                }
                else
                {
                    if (GUILayout.Button("Install", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.HeaderButtonOptions))
                    {
                        OnInstallClicked?.Invoke(package);
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
                    Application.OpenURL(package.documentationUrl);
                }
            }

            if (!string.IsNullOrEmpty(package.changelogUrl))
            {
                if (GUILayout.Button("View changelog", PFPackageStyles.LinkStyle))
                {
                    Application.OpenURL(package.changelogUrl);
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
                    Application.OpenURL(package.authorUrl);
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
                                OnRemoveClicked?.Invoke(package);
                            }
                        }
                        else if (package.isInstalled)
                        {
                            // 包已安装但不是这个版本，显示 Update（切换版本）
                            if (GUILayout.Button("Update", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.VersionButtonOptions))
                            {
                                OnInstallVersionClicked?.Invoke(package, version.version);
                            }
                        }
                        else
                        {
                            // 包未安装，显示 Install
                            if (GUILayout.Button("Install", PFPackageStyles.PrimaryButtonStyle, PFPackageStyles.VersionButtonOptions))
                            {
                                OnInstallVersionClicked?.Invoke(package, version.version);
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
                GUILayout.Label("No dependencies");
            }
            else
            {
                GUILayout.Label("Required:", PFPackageStyles.SectionTitleStyle);
                foreach (var dep in package.dependencies)
                {
                    GUILayout.Label($"• {dep.Key}@{dep.Value}");
                }
            }
        }

        public void ResetTab()
        {
            selectedTab = 0;
        }
    }
}
