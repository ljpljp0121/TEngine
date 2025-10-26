/*
 ****************************************************
 * 文件：WindowTreeBuilder.cs
 * 作者：PeiFeng
 * 创建时间：2025/10/25 20:02:38 星期六
 * 功能：窗口菜单树构建器
 ****************************************************
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PFDebugger
{
    public class WindowTreeBuilder
    {
        private readonly Dictionary<string, PathNode> pathToNode = new Dictionary<string, PathNode>();
        private PathNode rootNode;

        public void ScanAndBuildTree()
        {
            rootNode = new PathNode()
            {
                NodeType = PathNodeType.Menu,
                Children = new List<PathNode>(),
                Parent = null,
            };
            var types = typeof(DebuggerMainWindow).Assembly.GetTypes();

            foreach (var type in types)
            {
                //查看窗口
                var windowAttr = type.GetCustomAttribute<DebuggerWindowAttribute>();
                if (windowAttr != null && typeof(IWindowBase).IsAssignableFrom(type))
                {
                    AddNode(windowAttr.Path, windowAttr.Order, PathNodeType.Window, windowType: type);
                }

                //查看方法
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var methodAttr = method.GetCustomAttribute<DebuggerWindowAttribute>();
                    if (methodAttr != null)
                    {
                        AddNode(methodAttr.Path, methodAttr.Order, PathNodeType.Method, method: () => method.Invoke(null, null));
                    }
                }
            }

            SortNodeTree(rootNode);
        }

        private void AddNode(string path, int order, PathNodeType nodeType, Type windowType = null, Action method = null)
        {
            string[] parts = path.Split('/');
            PathNode parent = rootNode;
            string currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath += (i == 0 ? "" : "/") + parts[i];

                //已存在节点
                if (pathToNode.TryGetValue(currentPath, out PathNode node))
                {
                    if (i == parts.Length - 1)
                    {
                        Debug.LogError($"[DebuggerSystem] 叶子节点已存在，路径：{currentPath},");
                        break;
                    }

                    if (node.NodeType != PathNodeType.Menu)
                    {
                        Debug.LogError($"[DebuggerSystem] 非叶子节点必须是菜单节点，路径：{currentPath}");
                        break;
                    }
                    parent = node;
                    continue;
                }

                node = new PathNode
                {
                    Name = parts[i],
                    Path = currentPath,
                    NodeType = (i == parts.Length - 1) ? nodeType : PathNodeType.Menu,
                    Children = new List<PathNode>(),
                    Order = order,
                    Parent = parent
                };

                if (i == parts.Length - 1)
                {
                    if (nodeType == PathNodeType.Window && windowType != null)
                    {
                        // node.Window = Activator.CreateInstance(windowType) as IWindowBase;
                    }
                    else if (nodeType == PathNodeType.Method)
                    {
                        node.NodeType = PathNodeType.Method;
                        node.Method = method;
                    }
                }

                parent.Children.Add(node);
                parent = node;
                pathToNode[currentPath] = node;
            }
        }
        
        private void SortNodeTree(PathNode root)
        {
            if (root == null) return;

            Stack<PathNode> stack = new Stack<PathNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                PathNode currentNode = stack.Pop();

                if (currentNode.Children != null && currentNode.Children.Count > 1)
                {
                    currentNode.Children.Sort((a, b) => b.Order.CompareTo(a.Order));
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

        public PathNode GetRoot() => rootNode;
        public PathNode GetNode(string path) => pathToNode.GetValueOrDefault(path);

        /// <summary>
        /// 获取第一个窗口节点
        /// </summary>
        public PathNode GetFirstWindowNode(PathNode startNode)
        {
            if (startNode == null)
            {
                Debug.LogError($"[DebuggerSystem] startNode is null.");
                return null;
            }
            var stack = new Stack<PathNode>();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if (node.NodeType == PathNodeType.Window)
                    return node;

                for (int i = node.Children.Count - 1; i >= 0; i--)
                    stack.Push(node.Children[i]);
            }
            return null;
        }

        public List<PathNode> GetPathFromRoot(PathNode node)
        {
            var path = new List<PathNode>();
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