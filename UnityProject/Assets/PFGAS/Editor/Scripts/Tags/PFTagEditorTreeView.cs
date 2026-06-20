using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PFGAS.Editor
{
    public sealed class PFTagEditorTreeView : TreeView
    {
        private const string DragId = "PFGAS.PFTagTreeDrag";
        private readonly PFTagTreeModel model;

        public PFTagEditorTreeView(TreeViewState state, PFTagTreeModel model)
            : base(state, CreateHeader())
        {
            this.model = model;
            this.model.ModelChanged += Reload;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20f;
            Reload();
        }

        public IReadOnlyList<PFTagTreeNode> GetSelectedNodes()
        {
            var result = new List<PFTagTreeNode>();
            foreach (var id in GetSelection())
            {
                var node = model.FindById(id);
                if (node != null)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        public void AddChildNode(PFTagTreeNode parent)
        {
            parent ??= model.Root;
            var newId = model.GenerateUniqueId();
            var node = new PFTagTreeNode(newId, "NewTag", parent.Depth + 1);
            model.AddNode(node, parent);
            SetExpanded(parent.ID, true);

            var item = FindItem(newId, rootItem);
            if (item != null)
            {
                SetSelection(new[] { newId });
                BeginRename(item);
            }
        }

        public void DeleteSelectedNodes()
        {
            var selected = GetSelectedNodes();
            if (selected.Count == 0)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Tag", "Delete selected Tag nodes and their children?", "Delete", "Cancel"))
            {
                return;
            }

            model.BeginBatch();
            try
            {
                foreach (var node in selected)
                {
                    model.RemoveNode(node);
                }
            }
            finally
            {
                model.EndBatch();
            }
        }

        public void DuplicateSelectedNodes()
        {
            var selected = GetSelectedNodes();
            if (selected.Count == 0)
            {
                return;
            }

            model.BeginBatch();
            try
            {
                foreach (var node in selected)
                {
                    if (node != model.Root)
                    {
                        CloneSubtree(node, node.Parent);
                    }
                }
            }
            finally
            {
                model.EndBatch();
            }
        }

        public override void OnGUI(Rect rect)
        {
            multiColumnHeader.state.columns[0].width = rect.width;
            base.OnGUI(rect);

            if (Event.current.type != EventType.KeyDown || !HasFocus() || GetSelection().Count == 0)
            {
                return;
            }

            var mod = Event.current.command || Event.current.control;
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

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(PFTagExcelRow.RootParentId, -1, "Root");
            var allItems = new List<TreeViewItem>();

            foreach (var node in model.GetData())
            {
                if (node.Depth < 0)
                {
                    continue;
                }

                allItems.Add(new PFTagTreeViewItem(node));
            }

            if (allItems.Count > 0)
            {
                SetupParentsAndChildrenFromDepths(root, allItems);
            }
            else
            {
                root.children = new List<TreeViewItem>();
            }

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

        protected override bool CanRename(TreeViewItem item) => item.id != PFTagExcelRow.RootParentId;

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
            {
                return;
            }

            var node = model.FindById(args.itemID);
            if (node == null)
            {
                return;
            }

            model.RenameNode(node, string.IsNullOrWhiteSpace(args.newName) ? args.originalName : args.newName);
        }

        protected override bool CanMultiSelect(TreeViewItem item) => true;
        protected override bool CanStartDrag(CanStartDragArgs args) => !hasSearch;

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var dragItems = new List<PFTagTreeNode>();
            foreach (var id in args.draggedItemIDs)
            {
                var node = model.FindById(id);
                if (node != null)
                {
                    dragItems.Add(node);
                }
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DragId, dragItems);
            DragAndDrop.objectReferences = Array.Empty<Object>();
            DragAndDrop.StartDrag("PFTag Tree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition != DragAndDropPosition.UponItem &&
                args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
            {
                return DragAndDropVisualMode.None;
            }

            var dragData = DragAndDrop.GetGenericData(DragId) as List<PFTagTreeNode>;
            if (dragData == null || dragData.Count == 0)
            {
                return DragAndDropVisualMode.None;
            }

            PFTagTreeNode newParent;
            var insertIndex = -1;
            if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
            {
                newParent = model.FindById(args.parentItem.id) ?? model.Root;
            }
            else
            {
                newParent = args.parentItem.id >= 0 ? model.FindById(args.parentItem.id) : model.Root;
                newParent ??= model.Root;
                insertIndex = args.insertAtIndex;
            }

            foreach (var node in dragData)
            {
                if (newParent == node || newParent.IsChildOf(node))
                {
                    return DragAndDropVisualMode.Rejected;
                }
            }

            if (args.performDrop)
            {
                model.BeginBatch();
                try
                {
                    foreach (var node in dragData)
                    {
                        model.MoveNode(node, newParent, insertIndex);
                    }
                }
                finally
                {
                    model.EndBatch();
                }
            }

            return DragAndDropVisualMode.Move;
        }

        protected override void ContextClickedItem(int id)
        {
            var node = model.FindById(id);
            if (node == null)
            {
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Child"), false, () => AddChildNode(node));
            menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSelectedNodes);
            menu.AddItem(new GUIContent("Rename"), false, () => BeginRename(FindItem(id, rootItem)));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedNodes);
            menu.ShowAsContext();
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true,
                    allowToggleVisibility = false,
                },
            };

            var header = new MultiColumnHeader(new MultiColumnHeaderState(columns));
            header.ResizeToFit();
            return header;
        }

        private PFTagTreeNode CloneSubtree(PFTagTreeNode source, PFTagTreeNode parent)
        {
            var newId = model.GenerateUniqueId();
            var clone = new PFTagTreeNode(newId, source.Name, parent.Depth + 1)
            {
                Data = new PFTagNodeConfig
                {
                    Id = newId,
                    Name = source.Name,
                    Depth = parent.Depth + 1,
                    ParentId = parent.ID,
                    Desc = source.Data?.Desc ?? string.Empty,
                    FullPath = string.Empty,
                },
            };
            model.AddNode(clone, parent);

            foreach (var child in source.Children)
            {
                CloneSubtree(child, clone);
            }

            return clone;
        }

        private sealed class PFTagTreeViewItem : TreeViewItem
        {
            public PFTagTreeViewItem(PFTagTreeNode node)
                : base(node.ID, node.Depth, node.Name)
            {
                Node = node;
            }

            public PFTagTreeNode Node { get; }
        }
    }
}
