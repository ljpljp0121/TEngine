using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PFTreeView
{
    public abstract class PFTreeEditor<T> : EditorWindow where T : PFTreeNodeConfig, new()
    {
        [SerializeField] protected TreeViewState treeViewState;
        protected PFTreeView<T> treeView;
        protected PFTreeModel<T> treeModel;
        protected SearchField searchField;
        protected PFTreeConfig<T> config;

        protected abstract string GetConfigPath();

        protected abstract PFTreeConfig<T> CreateConfig();

        protected virtual void LoadConfig()
        {
            string path = GetConfigPath();
            config = AssetDatabase.LoadAssetAtPath<PFTreeConfig<T>>(path);
            if (config == null)
            {
                config = CreateConfig();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnEnable()
        {
            LoadConfig();
            InitTreeView();
        }

        private void OnGUI()
        {
            DrawToolbar();
            var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            treeView.OnGUI(treeRect);
        }

        private void InitTreeView()
        {
            treeViewState ??= new TreeViewState();

            var data = ConfigToTreeData(config);
            treeModel = new PFTreeModel<T>(data);

            treeView = new PFTreeView<T>(treeViewState, treeModel);
            treeView.ExpandAll();

            treeModel.ModelChanged += AutoSave;

            searchField = new SearchField();
            searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
        }

        protected virtual void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Space(4);
                    var searchRect = GUILayoutUtility.GetRect(80, 200,
                        EditorStyles.toolbarSearchField.fixedHeight,
                        EditorStyles.toolbarSearchField.fixedHeight);
                    treeView.searchString = searchField.OnToolbarGUI(searchRect, treeView.searchString);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("添加", EditorStyles.toolbarButton))
                    {
                        var selected = treeView.GetSelectedNodes();
                        var parent = selected.Count > 0 ? selected[0] : treeModel.Root;
                        treeView.AddChildNode(parent);
                    }

                    if (GUILayout.Button("删除", EditorStyles.toolbarButton))
                        treeView.DeleteSelectedNodes();

                    GUILayout.Space(4);

                    if (GUILayout.Button("展开", EditorStyles.toolbarButton))
                        treeView.ExpandAll();
                    if (GUILayout.Button("折叠", EditorStyles.toolbarButton))
                        treeView.CollapseAll();

                    DrawToolbarExtension();
                }

                DrawCodeGenPathToolbar();
            }
        }

        protected virtual void DrawToolbarExtension() { }

        protected virtual string GetDefaultCodeGenPath()
        {
            return "Assets/Scripts/Generated";
        }

        protected virtual void DrawCodeGenPathOption()
        {
            GUILayout.Label("生成路径", EditorStyles.miniLabel, GUILayout.Width(48));

            var displayPath = GetEffectiveCodeGenPath();
            EditorGUI.BeginChangeCheck();
            var codeGenPath = EditorGUILayout.DelayedTextField(
                displayPath,
                EditorStyles.toolbarTextField,
                GUILayout.MinWidth(220),
                GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
                SetCodeGenPath(codeGenPath);

            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(24)))
                SelectCodeGenPath();
        }

        private void DrawCodeGenPathToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(4);
                DrawCodeGenPathOption();
                GUILayout.Space(4);
            }
        }

        protected void SetCodeGenPath(string path)
        {
            var normalizedPath = NormalizeCodeGenPath(path);
            if (config.CodeGenPath == normalizedPath)
                return;

            config.CodeGenPath = normalizedPath;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        protected string GetEffectiveCodeGenPath()
        {
            return string.IsNullOrWhiteSpace(config.CodeGenPath)
                ? NormalizeCodeGenPath(GetDefaultCodeGenPath())
                : NormalizeCodeGenPath(config.CodeGenPath);
        }

        private void SelectCodeGenPath()
        {
            var startPath = GetCodeGenPathBrowserStartPath();
            var selectedPath = EditorUtility.OpenFolderPanel("选择生成代码路径", startPath, "");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            if (TryGetAssetsRelativePath(selectedPath, out var assetsRelativePath))
            {
                SetCodeGenPath(assetsRelativePath);
            }
            else
            {
                EditorUtility.DisplayDialog("路径无效", "请选择当前 Unity 项目 Assets 目录下的文件夹。", "确定");
            }
        }

        private string GetCodeGenPathBrowserStartPath()
        {
            var codeGenPath = GetEffectiveCodeGenPath();
            if (!string.IsNullOrWhiteSpace(codeGenPath))
            {
                var projectPath = GetProjectPath();
                var configPath = Path.GetFullPath(Path.Combine(projectPath, codeGenPath));
                if (Directory.Exists(configPath))
                    return configPath;
            }

            return Application.dataPath;
        }

        private static string NormalizeCodeGenPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace("\\", "/").TrimEnd('/');
        }

        private static bool TryGetAssetsRelativePath(string path, out string assetsRelativePath)
        {
            var assetsPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/").TrimEnd('/');
            var fullPath = Path.GetFullPath(path).Replace("\\", "/").TrimEnd('/');

            if (string.Equals(fullPath, assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                assetsRelativePath = "Assets";
                return true;
            }

            if (fullPath.StartsWith(assetsPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                assetsRelativePath = "Assets" + fullPath.Substring(assetsPath.Length);
                return true;
            }

            assetsRelativePath = string.Empty;
            return false;
        }

        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        protected virtual void AutoSave()
        {
            foreach (var node in treeModel.GetData())
            {
                if (node.Depth < 0) continue;
                node.Data.Id = node.ID;
                node.Data.Name = node.Name;
                node.Data.Depth = node.Depth;
                node.Data.ParentId = node.Parent?.ID ?? -1;
            }

            config.Nodes = treeModel.GetData()
                .Where(n => n.Depth >= 0)
                .Select(n => n.Data)
                .ToList();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        protected virtual List<PFTreeNode<T>> ConfigToTreeData(PFTreeConfig<T> cfg)
        {
            var result = new List<PFTreeNode<T>>();

            var root = new PFTreeNode<T>(-1, "Root", -1);
            result.Add(root);

            foreach (var nodeCfg in cfg.Nodes)
            {
                var node = new PFTreeNode<T>(nodeCfg.Id, nodeCfg.Name, nodeCfg.Depth);
                node.Data = nodeCfg;
                result.Add(node);
            }
            return result;
        }
    }
}
