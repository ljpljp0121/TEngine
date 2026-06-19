using System;
using System.Collections.Generic;
using System.Linq;

namespace PFTreeView
{
    public interface IIdGenerator
    {
        int NextId();
        void SetLastId(int id);
        void Reset();
    }

    public class IdGenerator : IIdGenerator
    {
        private int lastItemId;

        public int NextId() => lastItemId++;

        public void SetLastId(int id)
        {
            if (id >= lastItemId)
                lastItemId = id + 1;
        }

        public void Reset() => lastItemId = 0;
    }

    public class PFTreeModel<T> where T : new()
    {
        private List<PFTreeNode<T>> data;
        private PFTreeNode<T> root;
        private Dictionary<int, PFTreeNode<T>> idMap = new Dictionary<int, PFTreeNode<T>>();
        private int batchDepth;

        public PFTreeNode<T> Root => root;

        public event Action ModelChanged;

        public virtual IIdGenerator IdGenerator { get; } = new IdGenerator();

        public PFTreeModel(List<PFTreeNode<T>> data)
        {
            SetData(data);
        }

        public void SetData(List<PFTreeNode<T>> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count == 0)
                throw new ArgumentException("数据列表不能为空，必须至少包含一个根节点");

            this.data = data;
            root = data[0];

            if (root.Depth != -1)
                throw new ArgumentException("数据列表第一个节点必须是深度为 -1 的根节点");

            idMap.Clear();
            BuildTree();
            RebuildIdMap();
            SyncIdGenerator();
        }

        private void BuildTree()
        {
            foreach (var node in data)
            {
                node.Children = null;
                node.Parent = null;
            }

            var stack = new Stack<PFTreeNode<T>>();
            stack.Push(data[0]);

            for (int i = 1; i < data.Count; i++)
            {
                var node = data[i];
                while (stack.Count > 1 && stack.Peek().Depth >= node.Depth)
                    stack.Pop();

                stack.Peek().AddChild(node);
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

        private void SyncIdGenerator()
        {
            if (data.Count > 0)
            {
                var maxId = data.Max(n => n.ID);
                IdGenerator.SetLastId(maxId);
            }
        }

        public int GenerateUniqueId() => IdGenerator.NextId();

        public IList<PFTreeNode<T>> GetData() => data;

        #region 查找

        public PFTreeNode<T> FindById(int id)
        {
            idMap.TryGetValue(id, out var node);
            return node;
        }

        public PFTreeNode<T> FindItem(string path, char separator = '/', bool split = true)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var parent = root;
            var result = (PFTreeNode<T>)null;

            if (split && path.IndexOf(separator) != -1)
            {
                var p = path.Split(separator);
                for (int i = 0; i < p.Length; i++)
                {
                    if (!parent.ChildrenMap.TryGetValue(p[i], out result))
                        break;
                    parent = result;
                }
            }
            else
            {
                parent.ChildrenMap.TryGetValue(path, out result);
            }

            return result;
        }

        #endregion

        #region 增删改

        public void RenameNode(PFTreeNode<T> node, string newName)
        {
            var parentNode = node.Parent;
            var oldName = node.Name;
            node.Name = newName;

            if (parentNode != null)
            {
                parentNode.UpdateChildName(oldName, node);
            }

            NotifyChanged();
        }

        public void AddNode(PFTreeNode<T> node, PFTreeNode<T> parent, int insertPosition = -1)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            parent ??= root;

            int insertIdx;
            if (insertPosition < 0 || insertPosition >= (parent.Children?.Count ?? 0))
            {
                parent.AddChild(node);
                insertIdx = FindLastChildIndex(parent) + 1;
            }
            else
            {
                var siblingBefore = parent.Children[insertPosition];
                parent.InsertChild(insertPosition, node);
                insertIdx = data.IndexOf(siblingBefore);
            }

            data.Insert(insertIdx, node);
            idMap[node.ID] = node;

            var descendants = GetChildren(node, true);
            for (int i = 0; i < descendants.Count; i++)
            {
                data.Insert(insertIdx + 1 + i, descendants[i]);
                idMap[descendants[i].ID] = descendants[i];
            }

            NotifyChanged();
        }

        public void RemoveNode(PFTreeNode<T> node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node == root) throw new InvalidOperationException("不能移除根节点");

            var descendants = GetChildren(node, true);
            descendants.Add(node);

            foreach (var n in descendants)
            {
                data.Remove(n);
                idMap.Remove(n.ID);
            }

            node.Parent?.RemoveChild(node);

            NotifyChanged();
        }

        public void MoveNode(PFTreeNode<T> node, PFTreeNode<T> newParent, int insertPosition = -1)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node == root) throw new InvalidOperationException("不能移动根节点");
            newParent ??= root;

            if (newParent.IsChildOf(node) || node == newParent) return;

            var descendants = GetChildren(node, true);
            var allToRemove = new List<PFTreeNode<T>>(descendants) { node };
            foreach (var n in allToRemove)
            {
                data.Remove(n);
                idMap.Remove(n.ID);
            }

            node.Parent?.RemoveChild(node);

            int insertIdx;
            if (insertPosition < 0 || insertPosition >= (newParent.Children?.Count ?? 0))
            {
                newParent.AddChild(node);
                insertIdx = FindLastChildIndex(newParent) + 1;
            }
            else
            {
                var siblingBefore = newParent.Children[insertPosition];
                newParent.InsertChild(insertPosition, node);
                insertIdx = data.IndexOf(siblingBefore);
            }

            UpdateDepthRecursive(node);

            data.Insert(insertIdx, node);
            idMap[node.ID] = node;

            var updatedDescendants = GetChildren(node, true);
            for (int i = 0; i < updatedDescendants.Count; i++)
            {
                data.Insert(insertIdx + 1 + i, updatedDescendants[i]);
                idMap[updatedDescendants[i].ID] = updatedDescendants[i];
            }

            NotifyChanged();
        }

        #endregion

        #region 路径式 API

        public PFTreeNode<T> AddMenuItem(string path, char separator = '/', bool split = true)
        {
            return AddMenuItemTo(root, path, default, separator, split);
        }

        public PFTreeNode<T> AddMenuItemTo(PFTreeNode<T> parent, string path, T data = default,
            char separator = '/', bool split = true)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            parent ??= root;
            var name = path;

            if (split && path.IndexOf(separator) != -1)
            {
                var p = path.Split(separator);
                name = p[^1];
                for (int i = 0; i < p.Length - 1; i++)
                {
                    if (!parent.ChildrenMap.TryGetValue(p[i], out var tmpParent))
                    {
                        tmpParent = new PFTreeNode<T>(IdGenerator.NextId(), p[i], parent.Depth + 1);
                        AddNode(tmpParent, parent);
                    }
                    parent = tmpParent;
                }
            }

            var item = new PFTreeNode<T>(IdGenerator.NextId(), name, parent.Depth + 1);
            if (data != null)
                item.Data = data;
            AddNode(item, parent);
            return item;
        }

        public PFTreeNode<T> GetOrAddMenuItem(string path, T data = default,
            char separator = '/', bool split = true)
        {
            return GetOrAddMenuItemTo(root, path, data, separator, split);
        }

        public PFTreeNode<T> GetOrAddMenuItemTo(PFTreeNode<T> parent, string path, T data = default,
            char separator = '/', bool split = true)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            parent ??= root;
            var name = path;

            if (split && path.IndexOf(separator) != -1)
            {
                var p = path.Split(separator);
                name = p[^1];
                for (int i = 0; i < p.Length - 1; i++)
                {
                    if (!parent.ChildrenMap.TryGetValue(p[i], out var tmpParent))
                    {
                        tmpParent = new PFTreeNode<T>(IdGenerator.NextId(), p[i], parent.Depth + 1);
                        AddNode(tmpParent, parent);
                    }
                    parent = tmpParent;
                }
            }

            if (!parent.ChildrenMap.TryGetValue(name, out var child))
            {
                child = new PFTreeNode<T>(IdGenerator.NextId(), name, parent.Depth + 1);
                if (data != null)
                    child.Data = data;
                AddNode(child, parent);
            }

            return child;
        }

        #endregion

        #region 排序

        public void Sort(Func<PFTreeNode<T>, PFTreeNode<T>, int> comparer)
        {
            SortRecursive(root);

            void SortRecursive(PFTreeNode<T> node)
            {
                if (!node.HasChildren)
                    return;

                node.Children.Sort((a, b) => comparer(a, b));
                foreach (var child in node.Children)
                {
                    SortRecursive(child);
                }
            }

            RebuildFlatList();
            NotifyChanged();
        }

        public void Sort(PFTreeNode<T> sortRoot, Func<PFTreeNode<T>, PFTreeNode<T>, int> comparer)
        {
            SortRecursive(sortRoot);

            void SortRecursive(PFTreeNode<T> node)
            {
                if (!node.HasChildren)
                    return;

                node.Children.Sort((a, b) => comparer(a, b));
                foreach (var child in node.Children)
                {
                    SortRecursive(child);
                }
            }

            RebuildFlatList();
            NotifyChanged();
        }

        #endregion

        public List<PFTreeNode<T>> GetChildren(PFTreeNode<T> parent, bool recursive)
        {
            var list = new List<PFTreeNode<T>>();
            if (parent.Children == null) return list;

            foreach (var child in parent.Children)
            {
                list.Add(child);
                if (recursive)
                    list.AddRange(GetChildren(child, true));
            }

            return list;
        }

        private void UpdateDepthRecursive(PFTreeNode<T> node)
        {
            if (node.Children == null) return;
            foreach (var child in node.Children)
            {
                child.Depth = node.Depth + 1;
                UpdateDepthRecursive(child);
            }
        }

        private int FindLastChildIndex(PFTreeNode<T> parent)
        {
            int parentIdx = data.IndexOf(parent);
            int depth = parent.Depth;
            int lastIdx = parentIdx;

            for (int i = parentIdx + 1; i < data.Count; i++)
            {
                if (data[i].Depth > depth)
                    lastIdx = i;
                else
                    break;
            }

            return lastIdx;
        }

        private void RebuildFlatList()
        {
            data.Clear();
            data.Add(root);
            FlattenRecursive(root);

            void FlattenRecursive(PFTreeNode<T> node)
            {
                if (node.Children == null) return;
                foreach (var child in node.Children)
                {
                    data.Add(child);
                    FlattenRecursive(child);
                }
            }
        }

        private void NotifyChanged()
        {
            if (batchDepth == 0)
                ModelChanged?.Invoke();
        }

        public void BeginBatch()
        {
            batchDepth++;
        }

        public void EndBatch()
        {
            if (batchDepth > 0)
                batchDepth--;
            if (batchDepth == 0)
                ModelChanged?.Invoke();
        }
    }
}
