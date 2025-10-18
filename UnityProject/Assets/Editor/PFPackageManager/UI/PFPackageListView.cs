using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 左侧包列表视图
    /// </summary>
    public class PFPackageListView
    {
        private Vector2 scrollPos;
        private string searchText = "";

        public PackageInfo SelectedPackage { get; private set; }

        public void Draw(List<PackageInfo> packages)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            {
                // 搜索框
                searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);

                EditorGUILayout.Space(10);

                // 包列表
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                {
                    foreach (var package in packages)
                    {
                        if (!string.IsNullOrEmpty(searchText) &&
                            !package.displayName.ToLower().Contains(searchText.ToLower()))
                            continue;

                        DrawPackageItem(package);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPackageItem(PackageInfo package)
        {
            bool isSelected = SelectedPackage == package;

            GUIStyle itemStyle = isSelected ? "TV Selection" : "TV Line";

            Rect rect = EditorGUILayout.BeginHorizontal(itemStyle, GUILayout.Height(20));
            {
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    SelectedPackage = package;
                    Event.current.Use();
                    GUI.changed = true;
                }

                GUILayout.Label(package.displayName, PFPackageStyles.PackageNameStyle, GUILayout.ExpandWidth(true));

                // 版本号
                string version = package.isInstalled ? package.localVersion : package.version;
                GUILayout.Label(version, PFPackageStyles.VersionStyle, GUILayout.Width(50));

                // 状态图标(在最右边)
                string icon = "";
                if (package.hasUpdate)
                    icon = "⭘";  // 有更新
                else if (package.isInstalled)
                    icon = "⊙";  // 已安装

                GUILayout.Label(icon, GUILayout.Width(15));
            }
            EditorGUILayout.EndHorizontal();
        }

        public void SetSelectedPackage(PackageInfo package)
        {
            SelectedPackage = package;
        }
    }
}
