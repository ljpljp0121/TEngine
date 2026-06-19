using System.IO;
using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    public sealed class PFAttributeConfigWindow : EditorWindow
    {
        private PFAttributeConfigAsset config;
        private UnityEditor.Editor configEditor;
        private Vector2 scrollPosition;

        public static string ConfigPath
        {
            get
            {
                var guids = AssetDatabase.FindAssets("t:Script PFAttributeConfigWindow");
                if (guids.Length == 0)
                {
                    return "Assets/PFPackage/PFGAS/Editor/Scripts/Attribue/PFAttributeConfig.asset";
                }

                var scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var dir = Path.GetDirectoryName(scriptPath);
                return Path.Combine(dir ?? string.Empty, "PFAttributeConfig.asset").Replace("\\", "/");
            }
        }

        [MenuItem("Game/AttributeConfig", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<PFAttributeConfigWindow>("Attribute Config");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnDisable()
        {
            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
                configEditor = null;
            }
        }

        private void OnGUI()
        {
            if (config == null) LoadOrCreateConfig();
            if (config == null) return;

            if (GUILayout.Button("GenAttribute", GUILayout.Height(30f)))
            {
                OnGenerateClicked();
            }
           
            EditorGUILayout.ObjectField(config, typeof(PFAttributeConfigAsset), false);
            UnityEditor.Editor.CreateCachedEditor(config, null, ref configEditor);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            configEditor.OnInspectorGUI();
            EnsureStableIds();
            EditorGUILayout.EndScrollView();
        }

        private void OnGenerateClicked()
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            PFAttributeCodeGenerator.GenerateCode(config);
        }

        private void EnsureStableIds()
        {
            if (!PFAttributeCodeGenerator.EnsureStableIds(config, out var changed, out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                return;
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private void LoadOrCreateConfig()
        {
            config = AssetDatabase.LoadAssetAtPath<PFAttributeConfigAsset>(ConfigPath);
            if (config != null)
            {
                return;
            }

            config = CreateInstance<PFAttributeConfigAsset>();
            
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
        }
    }
}
