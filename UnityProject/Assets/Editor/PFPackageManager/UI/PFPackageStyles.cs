using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// Package Manager 样式管理
    /// </summary>
    public static class PFPackageStyles
    {
        // 列表项样式
        private static GUIStyle _packageNameStyle;
        public static GUIStyle PackageNameStyle
        {
            get
            {
                _packageNameStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15,
                    alignment = TextAnchor.LowerLeft
                };
                return _packageNameStyle;
            }
        }

        private static GUIStyle _versionStyle;
        public static GUIStyle VersionStyle
        {
            get
            {
                _versionStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleRight
                };
                return _versionStyle;
            }
        }

        // 详情页标题样式
        private static GUIStyle _detailTitleStyle;
        public static GUIStyle DetailTitleStyle
        {
            get
            {
                _detailTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 25,
                    alignment = TextAnchor.LowerLeft
                };
                return _detailTitleStyle;
            }
        }

        // 详情页作者/版本信息样式
        private static GUIStyle _detailSubtitleStyle;
        public static GUIStyle DetailSubtitleStyle
        {
            get
            {
                _detailSubtitleStyle ??= new GUIStyle(EditorStyles.label)
                {
                    fontSize = 15
                };

                return _detailSubtitleStyle;
            }
        }

        // Section 标题样式（Description、Author等）
        private static GUIStyle _sectionTitleStyle;
        public static GUIStyle SectionTitleStyle
        {
            get
            {
                _sectionTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                };
                return _sectionTitleStyle;
            }
        }

        // 内容文本样式
        private static GUIStyle _contentTextStyle;
        public static GUIStyle ContentTextStyle
        {
            get
            {
                _contentTextStyle ??= new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14
                };
                return _contentTextStyle;
            }
        }

        // 链接样式
        private static GUIStyle _linkStyle;
        public static GUIStyle LinkStyle
        {
            get
            {
                _linkStyle ??= new GUIStyle(EditorStyles.linkLabel)
                {
                    fontSize = 14
                };
                return _linkStyle;
            }
        }

        // 版本号标签样式（Versions标签页）
        private static GUIStyle _versionLabelStyle;
        public static GUIStyle VersionLabelStyle
        {
            get
            {
                _versionLabelStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15
                };
                return _versionLabelStyle;
            }
        }

        // 按钮样式
        private static GUIStyle _primaryButtonStyle;
        public static GUIStyle PrimaryButtonStyle
        {
            get
            {
                _primaryButtonStyle ??= new GUIStyle(GUI.skin.button)
                {
                    fontSize = 15
                };
                return _primaryButtonStyle;
            }
        }

        // 操作按钮布局选项
        public static readonly GUILayoutOption[] HeaderButtonOptions =
        {
            GUILayout.Height(30)
        };

        public static readonly GUILayoutOption[] VersionButtonOptions =
        {
            GUILayout.Width(80)
        };
    }
}
