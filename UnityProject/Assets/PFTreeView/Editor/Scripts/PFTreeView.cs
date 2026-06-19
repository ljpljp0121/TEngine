using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PFTreeView
{
    public class PFTreeView<T> : TreeView where T : new()
    {
        protected PFTreeModel<T> Model;
        private TreeViewItemPool itemPool;
        private readonly List<PFTreeViewItem> activeItems = new List<PFTreeViewItem>();

        public PFTreeView(TreeViewState state, PFTreeModel<T> model)
            : base(state, CreateHeader())
        {
            Model = model;
            Model.ModelChanged += Reload;
            itemPool = new TreeViewItemPool();

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20f;
            Reload();
        }

        public PFTreeView(TreeViewState state, PFTreeModel<T> model, TreeViewItemPool sharedPool)
            : base(state, CreateHeader())
        {
            Model = model;
            Model.ModelChanged += Reload;
            itemPool = sharedPool;

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20f;
            Reload();
        }

        private static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("名称"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true,
                    allowToggleVisibility = false,
                }
            };

            var state = new MultiColumnHeaderState(columns);
            var header = new MultiColumnHeader(state);
            header.ResizeToFit();
            return header;
        }

        public override void OnGUI(Rect rect)
        {
            multiColumnHeader.state.columns[0].width = rect.width;
            base.OnGUI(rect);

            if (Event.current.type == EventType.KeyDown && HasFocus() && GetSelection().Count > 0)
            {
                bool mod = Event.current.command || Event.current.control;

                if (Event.current.keyCode == KeyCode.Delete)
                {
                    Event.current.Use();
                    DeleteSelectedNodes();
                }
                else if (Event.current.keyCode == KeyCode.D && mod)
                {
                    Event.current.Use();
                    DuplicateSelectedNodes();
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            foreach (var item in activeItems)
                itemPool.Recycle(item);
            activeItems.Clear();

            var root = new TreeViewItem(-1, -1, "Root");
            var allItems = new List<TreeViewItem>();

            foreach (var node in Model.GetData())
            {
                if (node.Depth < 0) continue;
                var item = itemPool.Spawn();
                item.id = node.ID;
                item.depth = node.Depth;
                item.displayName = node.Name;
                item.Node = node;
                allItems.Add(item);
                activeItems.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var cellRect = args.GetCellRect(0);
            CenterRectUsingSingleLineHeight(ref cellRect);
            cellRect.xMin += GetContentIndent(args.item);

            if (args.isRenaming)
            {
                base.RowGUI(args);
                return;
            }

            EditorGUI.LabelField(cellRect, args.label);
        }

        protected override bool CanRename(TreeViewItem item) => true;

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename) return;
            var node = Model.FindById(args.itemID);
            if (node == null) return;
            Model.RenameNode(node, string.IsNullOrWhiteSpace(args.newName) ? args.originalName : args.newName);
            Reload();
        }

        #region 拖拽

        const string kDragId = "PFTreeViewDrag";

        protected override bool CanStartDrag(CanStartDragArgs args) => true;

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (hasSearch) return;

            DragAndDrop.PrepareStartDrag();
            var dragItems = new List<PFTreeNode<T>>();
            foreach (var id in args.draggedItemIDs)
            {
                var node = Model.FindById(id);
                if (node != null) dragItems.Add(node);
            }

            DragAndDrop.SetGenericData(kDragId, dragItems);
            DragAndDrop.objectReferences = Array.Empty<Object>();
            DragAndDrop.StartDrag("PFTreeView");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition != DragAndDropPosition.UponItem &&
                args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            var dragData = DragAndDrop.GetGenericData(kDragId) as List<PFTreeNode<T>>;
            if (dragData == null) return DragAndDropVisualMode.None;

            if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
            {
                var targetNode = Model.FindById(args.parentItem.id);
                if (targetNode != null)
                {
                    foreach (var node in dragData)
                    {
                        if (targetNode.IsChildOf(node) || node == targetNode)
                            return DragAndDropVisualMode.Rejected;
                    }
                }
            }

            if (args.performDrop)
            {
                PFTreeNode<T> newParent;
                int insertIdx = -1;

                if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
                {
                    newParent = Model.FindById(args.parentItem.id);
                }
                else
                {
                    newParent = args.parentItem.id >= 0
                        ? Model.FindById(args.parentItem.id)
                        : Model.Root;
                    insertIdx = args.insertAtIndex;
                }

                foreach (var node in dragData)
                    Model.MoveNode(node, newParent, insertIdx);

                Reload();
            }

            return DragAndDropVisualMode.Move;
        }

        #endregion

        #region 选中

        protected override bool CanMultiSelect(TreeViewItem item) => true;

        public List<PFTreeNode<T>> GetSelectedNodes()
        {
            var result = new List<PFTreeNode<T>>();
            foreach (var id in GetSelection())
            {
                var node = Model.FindById(id);
                if (node != null) result.Add(node);
            }

            return result;
        }

        #endregion

        #region 右键菜单

        protected override void ContextClickedItem(int id)
        {
            var node = Model.FindById(id);
            if (node == null) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("添加子节点"), false, () => AddChildNode(node));
            menu.AddItem(new GUIContent("复制为兄弟节点"), false, () => DuplicateSelectedNodes());
            menu.AddItem(new GUIContent("重命名"), false, () => BeginRename(FindItem(id, rootItem)));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("删除"), false, () => DeleteNode(node));
            menu.ShowAsContext();
        }

        #endregion

        #region 操作

        public void AddChildNode(PFTreeNode<T> parent)
        {
            int newId = Model.GenerateUniqueId();
            var newNode = new PFTreeNode<T>(newId, "新节点", parent.Depth + 1);
            Model.AddNode(newNode, parent);
            Reload();

            SetExpanded(parent.ID, true);
            var item = FindItem(newId, rootItem);
            if (item != null)
            {
                SetSelection(new List<int> { newId });
                BeginRename(item);
            }
        }

        public void DeleteSelectedNodes()
        {
            var selected = GetSelectedNodes();
            if (selected.Count == 0) return;

            Model.BeginBatch();
            try
            {
                foreach (var node in selected)
                {
                    if (node != Model.Root)
                        Model.RemoveNode(node);
                }
            }
            finally
            {
                Model.EndBatch();
            }
        }

        private void DeleteNode(PFTreeNode<T> node)
        {
            if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除 \"{node.Name}\" 及其所有子节点吗？", "删除", "取消"))
            {
                Model.RemoveNode(node);
            }
        }

        public void DuplicateSelectedNodes()
        {
            var selected = GetSelectedNodes();
            bool changed = false;

            Model.BeginBatch();
            try
            {
                foreach (var node in selected)
                {
                    if (node == Model.Root) continue;
                    CloneSubtree(node, node.Parent);
                    changed = true;
                }
            }
            finally
            {
                Model.EndBatch();
            }

            if (changed) Reload();
        }

        private PFTreeNode<T> CloneSubtree(PFTreeNode<T> src, PFTreeNode<T> parent)
        {
            var clone = new PFTreeNode<T>(Model.GenerateUniqueId(), src.Name, parent.Depth + 1);
            Model.AddNode(clone, parent);

            if (src.HasChildren)
            {
                foreach (var child in src.Children)
                    CloneSubtree(child, clone);
            }

            return clone;
        }

        #endregion

        #region 搜索

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 内部类型

        public class PFTreeViewItem : TreeViewItem
        {
            public PFTreeNode<T> Node { get; set; }

            public PFTreeViewItem() { }

            public PFTreeViewItem(int id, int depth, string displayName, PFTreeNode<T> node)
                : base(id, depth, displayName)
            {
                Node = node;
            }

            public void Reset()
            {
                id = 0;
                depth = 0;
                displayName = string.Empty;
                icon = null;
                parent = null;
                Node = null;
                children?.Clear();
            }
        }

        public class TreeViewItemPool : ObjectPoolBase<PFTreeViewItem>
        {
            protected override PFTreeViewItem Create()
            {
                return new PFTreeViewItem();
            }

            protected override void OnRecycle(PFTreeViewItem item)
            {
                item.Reset();
                base.OnRecycle(item);
            }
        }

        #endregion
    }
}
