using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFTreeView
{
    [Serializable]
    public class PFTreeNode<T> where T : new()
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private int depth;

        private T data = new T();
        private List<PFTreeNode<T>> children;
        private PFTreeNode<T> parent;

        [NonSerialized] private Dictionary<string, PFTreeNode<T>> m_childrenMap;

        public PFTreeNode(int id, string name, int depth)
        {
            this.id = id;
            this.name = name;
            this.depth = depth;
        }

        public int ID
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public int Depth
        {
            get => depth;
            set => depth = value;
        }

        public T Data
        {
            get => data;
            set => data = value;
        }

        public List<PFTreeNode<T>> Children
        {
            get => children;
            set => children = value;
        }

        public bool HasChildren => Children?.Count > 0;

        public PFTreeNode<T> Parent
        {
            get => parent;
            set => parent = value;
        }

        public IReadOnlyDictionary<string, PFTreeNode<T>> ChildrenMap
        {
            get
            {
                EnsureChildrenMap();
                return m_childrenMap;
            }
        }

        private void EnsureChildrenMap()
        {
            if (m_childrenMap != null)
                return;

            m_childrenMap = new Dictionary<string, PFTreeNode<T>>();
            if (children == null)
                return;

            foreach (var child in children)
            {
                if (child != null)
                    m_childrenMap[child.name] = child;
            }
        }

        public void AddChild(PFTreeNode<T> child)
        {
            children ??= new List<PFTreeNode<T>>();
            EnsureChildrenMap();

            children.Add(child);
            m_childrenMap[child.name] = child;
            child.parent = this;
            child.depth = depth + 1;
        }

        public void InsertChild(int index, PFTreeNode<T> child)
        {
            children ??= new List<PFTreeNode<T>>();
            EnsureChildrenMap();

            children.Insert(index, child);
            m_childrenMap[child.name] = child;
            child.parent = this;
            child.depth = depth + 1;
        }

        public void RemoveChild(PFTreeNode<T> child)
        {
            if (children == null || !children.Remove(child))
                return;

            if (m_childrenMap != null && m_childrenMap.TryGetValue(child.name, out var existing) && existing == child)
                m_childrenMap.Remove(child.name);

            child.parent = null;
        }

        internal void UpdateChildName(string oldName, PFTreeNode<T> child)
        {
            if (m_childrenMap != null)
            {
                m_childrenMap.Remove(oldName);
                m_childrenMap[child.Name] = child;
            }
        }

        public bool IsChildOf(PFTreeNode<T> node)
        {
            var tmp = this;
            while (tmp != null)
            {
                if (tmp == node)
                    return true;
                tmp = tmp.parent;
            }

            return false;
        }
    }
}
