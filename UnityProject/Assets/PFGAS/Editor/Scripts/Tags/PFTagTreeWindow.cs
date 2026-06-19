using System.IO;
using UnityEditor;
using UnityEngine;
using PFTreeView;

namespace PFGAS.Editor
{
    /// <summary>
    /// 编辑 PFTag 树并触发 Tag 代码生成的编辑器窗口。
    /// </summary>
    public class PFTagTreeWindow : PFTreeEditor<PFTagNodeConfig>
    {
        public static string ConfigPath
        {
            get
            {
                var guids = AssetDatabase.FindAssets("t:Script PFTagTreeWindow");
                if (guids.Length == 0)
                    return "Assets/Editor/LJPTools/PFTag/PFTagConfig.asset";

                var scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var dir = Path.GetDirectoryName(scriptPath);
                return Path.Combine(dir, "PFTagConfig.asset").Replace("\\", "/");
            }
        }

        [MenuItem("Game/Tag系统", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<PFTagTreeWindow>("Tag 树配置");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        protected override string GetConfigPath() => ConfigPath;

        protected override PFTreeConfig<PFTagNodeConfig> CreateConfig() => CreateInstance<PFTagConfig>();

        protected override string GetDefaultCodeGenPath() => PFTagCodeGenerator.GetDefaultOutputPath();

        protected override void DrawToolbarExtension()
        {
            GUILayout.Space(8);

            GUI.color = Color.cyan;
            if (GUILayout.Button("生成代码", EditorStyles.toolbarButton))
                PFTagCodeGenerator.GenerateCode();
            GUI.color = Color.white;
        }
    }
}
