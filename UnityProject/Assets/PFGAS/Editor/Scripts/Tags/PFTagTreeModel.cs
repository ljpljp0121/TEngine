using System;
using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Editor
{
    public sealed class PFTagTreeModel
    {
        private readonly Dictionary<int, PFTagTreeNode> idMap = new Dictionary<int, PFTagTreeNode>();
        private List<PFTagTreeNode> data;
        private PFTagTreeNode root;
        private int nextId;
        private int batchDepth;

        public PFTagTreeModel(List<PFTagTreeNode> data)
        {
            SetData(data);
        }

        public event Action ModelChanged;
        public PFTagTreeNode Root => root;

        public void SetData(List<PFTagTreeNode> data)
        {
            if (data == null || data.Count == 0)
            {
                data = CreateEmptyData();
            }

            this.data = data;
            root = data[0];
            BuildTreeFromDepths();
            RebuildIdMap();
            nextId = Math.Max(1, data.Max(n => n.ID) + 1);
        }

        public IList<PFTagTreeNode> GetData() => data;

        public PFTagTreeNode FindById(int id)
        {
            idMap.TryGetValue(id, out var node);
            return node;
        }

        public int GenerateUniqueId()
        {
            while (idMap.ContainsKey(nextId))
            {
                nextId++;
            }

            return nextId++;
        }

        public void RenameNode(PFTagTreeNode node, string newName)
        {
            if (node == null || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            node.Name = newName.Trim();
            node.SyncDataFromNode();
            NotifyChanged();
        }

        public void AddNode(PFTagTreeNode node, PFTagTreeNode parent, int insertPosition = -1)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            parent ??= root;
            var insertIndex = GetInsertIndex(parent, insertPosition);

            if (insertPosition >= 0 && insertPosition < parent.Children.Count)
            {
                parent.InsertChild(insertPosition, node);
            }
            else
            {
                parent.AddChild(node);
            }

            data.Insert(insertIndex, node);
            idMap[node.ID] = node;
            node.SyncDataFromNode();
            NotifyChanged();
        }

        public void RemoveNode(PFTagTreeNode node)
        {
            if (node == null || node == root)
            {
                return;
            }

            var allToRemove = GetChildren(node, true);
            allToRemove.Add(node);

            foreach (var item in allToRemove)
            {
                data.Remove(item);
                idMap.Remove(item.ID);
            }

            node.Parent?.RemoveChild(node);
            NotifyChanged();
        }

        public void MoveNode(PFTagTreeNode node, PFTagTreeNode newParent, int insertPosition = -1)
        {
            if (node == null || node == root)
            {
                return;
            }

            newParent ??= root;
            if (newParent == node || newParent.IsChildOf(node))
            {
                return;
            }

            var allToMove = new List<PFTagTreeNode> { node };
            allToMove.AddRange(GetChildren(node, true));
            foreach (var item in allToMove)
            {
                data.Remove(item);
            }

            node.Parent?.RemoveChild(node);
            var insertIndex = GetInsertIndex(newParent, insertPosition);
            if (insertPosition >= 0 && insertPosition < newParent.Children.Count)
            {
                newParent.InsertChild(insertPosition, node);
            }
            else
            {
                newParent.AddChild(node);
            }

            UpdateDepthRecursive(node);
            data.Insert(insertIndex, node);
            var descendants = GetChildren(node, true);
            for (var i = 0; i < descendants.Count; i++)
            {
                data.Insert(insertIndex + 1 + i, descendants[i]);
            }

            NotifyChanged();
        }

        public List<PFTagTreeNode> GetChildren(PFTagTreeNode parent, bool recursive)
        {
            var result = new List<PFTagTreeNode>();
            if (parent == null)
            {
                return result;
            }

            foreach (var child in parent.Children)
            {
                result.Add(child);
                if (recursive)
                {
                    result.AddRange(GetChildren(child, true));
                }
            }

            return result;
        }

        public void BeginBatch()
        {
            batchDepth++;
        }

        public void EndBatch()
        {
            if (batchDepth > 0)
            {
                batchDepth--;
            }

            if (batchDepth == 0)
            {
                NotifyChanged();
            }
        }

        public static List<PFTagTreeNode> CreateEmptyData()
        {
            return new List<PFTagTreeNode>
            {
                new PFTagTreeNode(PFTagExcelRow.RootParentId, "Root", -1),
            };
        }

        private void BuildTreeFromDepths()
        {
            foreach (var node in data)
            {
                node.Children.Clear();
                node.Parent = null;
            }

            var stack = new Stack<PFTagTreeNode>();
            stack.Push(root);

            for (var i = 1; i < data.Count; i++)
            {
                var node = data[i];
                while (stack.Count > 1 && stack.Peek().Depth >= node.Depth)
                {
                    stack.Pop();
                }

                stack.Peek().AddChild(node);
                node.SyncDataFromNode();
                stack.Push(node);
            }
        }

        private void RebuildIdMap()
        {
            idMap.Clear();
            foreach (var node in data)
            {
                idMap[node.ID] = node;
            }
        }

        private int GetInsertIndex(PFTagTreeNode parent, int insertPosition)
        {
            if (insertPosition >= 0 && insertPosition < parent.Children.Count)
            {
                return data.IndexOf(parent.Children[insertPosition]);
            }

            return FindLastDescendantIndex(parent) + 1;
        }

        private int FindLastDescendantIndex(PFTagTreeNode parent)
        {
            var parentIndex = data.IndexOf(parent);
            var lastIndex = parentIndex;
            for (var i = parentIndex + 1; i < data.Count; i++)
            {
                if (data[i].Depth > parent.Depth)
                {
                    lastIndex = i;
                    continue;
                }

                break;
            }

            return lastIndex;
        }

        private void UpdateDepthRecursive(PFTagTreeNode node)
        {
            node.Depth = node.Parent?.Depth + 1 ?? -1;
            node.SyncDataFromNode();
            foreach (var child in node.Children)
            {
                UpdateDepthRecursive(child);
            }
        }

        private void NotifyChanged()
        {
            if (batchDepth == 0)
            {
                ModelChanged?.Invoke();
            }
        }
    }
}
