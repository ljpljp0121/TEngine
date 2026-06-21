using UnityEditor;
using UnityEngine;

namespace PFGAS.Editor
{
    public sealed class PFAttributeSetConfigWindow : EditorWindow
    {
        private PFAttributeConfigAsset config;
        private SerializedObject serializedConfig;
        private Vector2 scrollPosition;

        [MenuItem("Game/AttributeSet", false, 102)]
        public static void ShowWindow()
        {
            var window = GetWindow<PFAttributeSetConfigWindow>("AttributeSet");
            window.minSize = new Vector2(640f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            EnsureConfig();
            if (config == null || serializedConfig == null)
            {
                return;
            }

            DrawToolbar();
            serializedConfig.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var attributeSets = serializedConfig.FindProperty(nameof(PFAttributeConfigAsset.AttributeSets));
            EditorGUILayout.PropertyField(attributeSets, new GUIContent("AttributeSets"), true);
            EditorGUILayout.EndScrollView();

            serializedConfig.ApplyModifiedProperties();
            EnsureStableIds();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Generate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    OnGenerateClicked();
                }

                if (GUILayout.Button("Open Attribute", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    PFAttributeConfigWindow.ShowWindow();
                }
            }
        }

        private void OnGenerateClicked()
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            PFAttributeCodeGenerator.GenerateCode(config);
        }

        private void EnsureConfig()
        {
            if (config == null || serializedConfig == null)
            {
                LoadConfig();
            }
        }

        private void LoadConfig()
        {
            config = PFAttributeConfigWindow.LoadOrCreateConfigAsset();
            serializedConfig = config == null ? null : new SerializedObject(config);
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
            serializedConfig.Update();
        }
    }
}
