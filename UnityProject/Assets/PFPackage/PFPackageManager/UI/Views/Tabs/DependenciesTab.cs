using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// Dependencies Tab 组件 - 负责显示依赖项信息
    /// </summary>
    public class DependenciesTab
    {
        private PackageDetailEventHandler eventHandler;
        private System.Collections.Generic.List<PackageInfo> allPackages;

        public DependenciesTab(PackageDetailEventHandler handler, System.Collections.Generic.List<PackageInfo> packages)
        {
            eventHandler = handler;
            allPackages = packages;
        }

        public void Draw(PackageInfo package)
        {
            GUILayout.Label("Dependencies", PFPackageStyles.SectionTitleStyle);
            EditorGUILayout.Space(5);

            if (package.dependencies == null || package.dependencies.Count == 0)
            {
                GUILayout.Label("No dependencies", PFPackageStyles.CenteredGreyMiniLabelStyle);
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

                        // 根据不同状态显示不同的按钮
                        if (depStatus.isUnityPackage)
                        {
                            // Unity 官方包
                            if (!depStatus.isAvailable)
                            {
                                if (GUILayout.Button("安装", GUILayout.Width(60)))
                                {
                                    UnityPackageDependencyChecker.InstallUnityPackage(packageName, versionRange);
                                }
                            }
                            else if (!depStatus.isVersionCompatible)
                            {
                                if (GUILayout.Button("升级", GUILayout.Width(60)))
                                {
                                    UnityPackageDependencyChecker.InstallUnityPackage(packageName, versionRange);
                                }
                            }
                        }
                        else
                        {
                            // 第三方包
                            if (!depStatus.isAvailable)
                            {
                                // 未安装
                                if (GUILayout.Button("安装", GUILayout.Width(60)))
                                {
                                    eventHandler?.HandleUpgradeDependency(packageName, versionRange, allPackages);
                                }
                            }
                            else if (!depStatus.isVersionCompatible)
                            {
                                // 已安装但版本不匹配
                                if (GUILayout.Button("升级", GUILayout.Width(60)))
                                {
                                    eventHandler?.HandleUpgradeDependency(packageName, versionRange, allPackages);
                                }
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
    }
}