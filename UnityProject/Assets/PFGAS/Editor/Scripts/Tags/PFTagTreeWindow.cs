using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PFGAS.Editor
{
    public sealed class PFTagTreeWindow : EditorWindow
    {
        [SerializeField] private TreeViewState treeViewState;

        private readonly PFTagExcelService excelService = new PFTagExcelService();
        private PFTagExcelDocument excelDocument;
        private PFTagTreeModel treeModel;
        private PFTagEditorTreeView treeView;
        private SearchField searchField;
        private bool hasUnsavedChanges;

        [MenuItem("Game/Tag系统", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<PFTagTreeWindow>("Tag Excel 树配置");
            window.minSize = new Vector2(360, 420);
            window.Show();
        }

        private void OnEnable()
        {
            treeViewState ??= new TreeViewState();
            LoadExcelIntoTree(false);
        }

        private void OnGUI()
        {
            EnsureTreeCreated(PFTagTreeModel.CreateEmptyData());
            DrawToolbar();

            var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            treeView.OnGUI(treeRect);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Space(4);
                    var searchRect = GUILayoutUtility.GetRect(
                        80,
                        220,
                        EditorStyles.toolbarSearchField.fixedHeight,
                        EditorStyles.toolbarSearchField.fixedHeight);
                    treeView.searchString = searchField.OnToolbarGUI(searchRect, treeView.searchString);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("添加", EditorStyles.toolbarButton))
                    {
                        var selected = treeView.GetSelectedNodes();
                        treeView.AddChildNode(selected.Count > 0 ? selected[0] : treeModel.Root);
                    }

                    if (GUILayout.Button("删除", EditorStyles.toolbarButton))
                    {
                        treeView.DeleteSelectedNodes();
                    }

                    GUILayout.Space(4);

                    if (GUILayout.Button("刷新", EditorStyles.toolbarButton))
                    {
                        RefreshFromExcel();
                    }

                    GUI.color = hasUnsavedChanges ? Color.yellow : Color.white;
                    if (GUILayout.Button("保存", EditorStyles.toolbarButton))
                    {
                        SaveToExcel();
                    }

                    GUI.color = Color.white;
                    if (GUILayout.Button("生成适配", EditorStyles.toolbarButton))
                    {
                        PFTagCodeGenerator.GenerateCode();
                    }

                    GUILayout.Space(4);

                    if (GUILayout.Button("展开", EditorStyles.toolbarButton))
                    {
                        treeView.ExpandAll();
                    }

                    if (GUILayout.Button("折叠", EditorStyles.toolbarButton))
                    {
                        treeView.CollapseAll();
                    }
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Excel", EditorStyles.miniLabel, GUILayout.Width(34));
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField(
                            PFTagExcelPaths.TagExcelProjectPath,
                            EditorStyles.toolbarTextField,
                            GUILayout.MinWidth(260),
                            GUILayout.ExpandWidth(true));
                    }
                }
            }
        }

        private void RefreshFromExcel()
        {
            if (hasUnsavedChanges &&
                !EditorUtility.DisplayDialog(
                    "刷新 Tag Excel",
                    "当前树有未保存修改，刷新会丢弃这些修改。继续吗？",
                    "刷新",
                    "取消"))
            {
                return;
            }

            LoadExcelIntoTree(true);
            Repaint();
        }

        private bool SaveToExcel()
        {
            var rows = PFTagExcelTreeConverter.FromTree(treeModel);
            var validation = new PFTagExcelValidator().Validate(rows);
            if (!validation.IsValid)
            {
                var message = validation.FormatErrors();
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Tag 校验失败", message, "确定");
                return false;
            }

            try
            {
                excelDocument ??= excelService.Read();
                excelService.Save(excelDocument, rows);
                PFTagCodeGenerator.GenerateCode(rows);
                hasUnsavedChanges = false;
                Debug.Log($"PFTag Excel 保存完成：{PFTagExcelPaths.TagExcelPath}");
                return true;
            }
            catch (PFTagExcelFileLockedException ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("PFTag Excel 被占用", "保存失败。请关闭正在占用 PFTag.xlsx 的外部程序后重试。", "确定");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("PFTag Excel 保存失败", ex.Message, "确定");
                return false;
            }
        }

        private void LoadExcelIntoTree(bool showDialog)
        {
            var treeData = PFTagTreeModel.CreateEmptyData();
            try
            {
                excelDocument = excelService.Read();
                var validation = new PFTagExcelValidator().Validate(excelDocument.Rows);
                if (!validation.IsValid)
                {
                    Debug.LogError(validation.FormatErrors());
                }

                treeData = ConfigToTreeData(PFTagExcelTreeConverter.ToNodeConfigs(excelDocument.Rows));
                hasUnsavedChanges = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("PFTag Excel 加载失败", ex.Message, "确定");
                }
            }

            EnsureTreeCreated(treeData);
            treeModel.SetData(treeData);
            treeView.Reload();
            treeView.ExpandAll();
        }

        private void EnsureTreeCreated(List<PFTagTreeNode> data)
        {
            if (treeModel != null && treeView != null && searchField != null)
            {
                return;
            }

            treeModel = new PFTagTreeModel(data);
            treeModel.ModelChanged += OnTreeModelChanged;
            treeView = new PFTagEditorTreeView(treeViewState, treeModel);
            treeView.ExpandAll();

            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
        }

        private void OnTreeModelChanged()
        {
            hasUnsavedChanges = true;
            Repaint();
        }

        private static List<PFTagTreeNode> ConfigToTreeData(IEnumerable<PFTagNodeConfig> configs)
        {
            var result = PFTagTreeModel.CreateEmptyData();
            foreach (var config in configs)
            {
                var node = new PFTagTreeNode(config.Id, config.Name, config.Depth)
                {
                    Data = config,
                };
                result.Add(node);
            }

            return result;
        }
    }
}
