using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// Versions Tab 组件 - 负责显示版本历史
    /// </summary>
    public class VersionsTab
    {
        private PackageDetailEventHandler eventHandler;

        public VersionsTab(PackageDetailEventHandler handler)
        {
            eventHandler = handler;
        }

        public void Draw(PackageInfo package)
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
    }
}