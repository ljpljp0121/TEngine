using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// Description Tab 组件 - 负责显示包的描述信息
    /// </summary>
    public class DescriptionTab
    {
        private PackageDetailEventHandler eventHandler;

        public DescriptionTab(PackageDetailEventHandler handler)
        {
            eventHandler = handler;
        }

        public void Draw(PackageInfo package)
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
    }
}