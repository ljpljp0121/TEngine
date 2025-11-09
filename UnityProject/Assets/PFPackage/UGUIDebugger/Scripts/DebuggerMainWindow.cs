/*
 ****************************************************
 * 文件：DebuggerMainWindow.cs
 * 作者：PeiFeng
 * 创建时间：2025/10/25 18:53:37 星期六
 * 功能：日志系统主窗口
 ****************************************************
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace PFDebugger
{
    public class DebuggerMainWindow : MonoBehaviour
    {
        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                gameObject.SetActive(isActive);
            }
        }

        public GameObject BtnLayoutPrefab;
        public GameObject BtnPrefab;

        public RectTransform TitleRoot;
        public RectTransform MainWindowRoot;
        public RectTransform WindowRoot;

        private WindowTreeBuilder windowTreeBuilder;
        private PathNode selectedNode; //当前选中的窗口节点

        private readonly List<GameObject> currentLayouts = new List<GameObject>();
        private readonly List<GameObject> currentButtons = new List<GameObject>();

        internal void OnAwake()
        {
            windowTreeBuilder = new WindowTreeBuilder();
            windowTreeBuilder.ScanAndBuildTree();
            ShowLevel(windowTreeBuilder.GetRoot());
        }

        internal void OnRelease()
        {
            ClearCurrentUI();
        }

        public void ShowLevel(PathNode node)
        {
            if (node == null)
            {
                Debug.LogError($"[DebuggerMainWindow] ShowLevel: node is null.");
                return;
            }
            ClearCurrentUI();

            var firstNode = windowTreeBuilder.GetFirstWindowNode(node);
            if (firstNode == null) return;
            var path = windowTreeBuilder.GetPathFromRoot(firstNode);

            foreach (var pathNode in path)
            {
                if (pathNode.IsLeaf) break;
                var layout = GameObjectPool.I.Get(BtnLayoutPrefab, TitleRoot);
                currentLayouts.Add(layout);

                foreach (var child in pathNode.Children)
                {
                    var btn = GameObjectPool.I.Get(BtnPrefab, layout.transform);
                    currentButtons.Add(btn);

                    var item = btn.GetComponent<MenuItem>();
                    item.SetText(child.Name);
                    item.SetHighlighted(path.Contains(child));
                    SetMenuAction(child, item);
                }
            }
            MainWindowRoot.offsetMax = new Vector2(0, -currentLayouts.Count * BtnLayoutPrefab.GetComponent<RectTransform>().rect.height);
        }

        private void SetMenuAction(PathNode child, MenuItem item)
        {
            if (child.NodeType == PathNodeType.Menu )
                item.AddListener(() => ShowLevel(child));
            else if (child.NodeType == PathNodeType.Method)
                item.AddListener(child.Method);
            else if (child.NodeType == PathNodeType.Window)
                item.AddListener(() =>
                {
                    ShowLevel(child);
                    
                });
        }

        /// <summary>
        /// 清理当前显示的UI，回收对象池
        /// </summary>
        private void ClearCurrentUI()
        {
            foreach (var btn in currentButtons)
            {
                if (btn != null)
                {
                    btn.GetComponent<MenuItem>().Reset();
                    GameObjectPool.I.Release(btn);
                }
            }
            currentButtons.Clear();

            foreach (var layout in currentLayouts)
            {
                if (layout != null)
                {
                    GameObjectPool.I.Release(layout);
                }
            }
            currentLayouts.Clear();
        }
    }
}