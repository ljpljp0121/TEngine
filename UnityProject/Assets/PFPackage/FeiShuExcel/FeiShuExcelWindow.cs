// using UnityEditor;
// using UnityEngine;
//
// namespace PFPackage.FeiShuExcel
// {
//     public class FeiShuExcelWindow : EditorWindow
//     {
//         public static void OpenWindow()
//         {
//             var window = GetWindow<FeiShuExcelWindow>();
//             window.minSize = new Vector2(600, 400);
//         }
//
//         private void OnGUI()
//         {
//             GUILayout.BeginVertical(GUI.skin.box);
//             {
//                 GUILayout.Label("线上表文件夹:", EditorStyles.boldLabel);
//                 FeiShuExcelSetting.I.FeiShuFolderRootToken = EditorGUILayout.TextField(FeiShuExcelSetting.I.FeiShuFolderRootToken);
//                 if (GUILayout.Button("打开表文件夹"))
//                 {
//                     Application.OpenURL($"");
//                 }
//                 if (GUILayout.Button("本地配置表同步到远端")) { }
//                 if (GUILayout.Button("远端配置表导入")) { }
//             }
//         }
//     }
// }