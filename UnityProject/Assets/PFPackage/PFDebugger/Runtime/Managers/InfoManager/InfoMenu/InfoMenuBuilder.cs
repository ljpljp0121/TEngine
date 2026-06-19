using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PFDebugger
{
    public class InfoMenuBuilder
    {
        private readonly Dictionary<string, InfoPathNode> pathToNode = new Dictionary<string, InfoPathNode>();
        private InfoPathNode rootNode;

        public void ScanAndBuildTree()
        {
            rootNode = new InfoPathNode()
            {
                NodeType = InfoPathNodeType.Menu,
                Children = new List<InfoPathNode>(),
                Parent = null,
            };

            var types = typeof(DebuggerManager).Assembly.GetTypes();

            foreach (var type in types)
            {
                //查看窗口
                var windowAttr = type.GetCustomAttribute<InfoMenuAttribute>();
                if (windowAttr != null && typeof(InfoBase).IsAssignableFrom(type))
                {
                    AddNode(windowAttr.Path, windowAttr.Order, windowType: type);
                }
            }
            
            SortNodeTree(rootNode);
        }

        private void AddNode(string path, int order, Type windowType = null)
        {
            string[] parts = path.Split('/');
            InfoPathNode parent = rootNode;
            string currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath += (i == 0 ? "" : "/") + parts[i];

                //已存在节点
                if (pathToNode.TryGetValue(currentPath, out InfoPathNode node))
                {
                    if (i == parts.Length - 1)
                    {
                        Debug.LogError($"[PFDebugger] 叶子节点已存在，路径：{currentPath},");
                        break;
                    }

                    if (node.NodeType != InfoPathNodeType.Menu)
                    {
                        Debug.LogError($"[PFDebugger] 非叶子节点必须是菜单节点，路径：{currentPath}");
                        break;
                    }
                    parent = node;
                    continue;
                }

                node = new InfoPathNode
                {
                    Name = parts[i],
                    Path = currentPath,
                    NodeType = (i == parts.Length - 1) ? InfoPathNodeType.Leaf : InfoPathNodeType.Menu,
                    Children = new List<InfoPathNode>(),
                    Order = order,
                    Parent = parent
                };

                if (i == parts.Length - 1 && windowType != null)
                {
                    node.Info = Activator.CreateInstance(windowType) as InfoBase;
                }

                parent.Children.Add(node);
                parent = node;
                pathToNode[currentPath] = node;
            }
        }

        private void SortNodeTree(InfoPathNode root)
        {
            if (root == null) return;

            Stack<InfoPathNode> stack = new Stack<InfoPathNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                InfoPathNode currentNode = stack.Pop();

                if (currentNode.Children != null && currentNode.Children.Count > 1)
                {
                    currentNode.Children.Sort((a, b) => a.Order.CompareTo(b.Order));
                }

                if (currentNode.Children != null)
                {
                    foreach (var child in currentNode.Children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public InfoPathNode GetRoot() => rootNode;
        public InfoPathNode GetNode(string path) => pathToNode.GetValueOrDefault(path);
        
        public InfoPathNode GetFirstWindowNode(InfoPathNode startNode)
        {
            if (startNode == null)
            {
                Debug.LogError($"[PFDebugger] startNode is null.");
                return null;
            }
            var stack = new Stack<InfoPathNode>();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if (node.NodeType == InfoPathNodeType.Leaf)
                    return node;

                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push(node.Children[i]);
            }
            return null;
        }
        
        public List<InfoPathNode> GetPathFromRoot(InfoPathNode node)
        {
            var path = new List<InfoPathNode>();
            while (node != null)
            {
                path.Add(node);
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}