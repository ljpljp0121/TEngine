using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    [DebuggerTab("InfoPanel", 3)]
    public class InfoPanel : MonoBehaviour, IDebuggerPanel
    {
        //预制体
        public GameObject BtnLayoutPrefab; //菜单每行容器预制体
        public GameObject BtnPrefab; //菜单预制体
        public GameObject InfoItemPrefab; //信息Item预制体

        public RectTransform MainRect;
        public RectTransform ItemContent;

        public Color ItemColor1;
        public Color ItemColor2;

        private InfoMenuBuilder infoMenuBuilder;
        private InfoPathNode currentLeafNode;
        private readonly List<InfoItem> currentInfoItems = new List<InfoItem>();

        private readonly List<GameObject> currentLayouts = new List<GameObject>();
        private readonly List<GameObject> currentButtons = new List<GameObject>();

        public void OnInitPanel()
        {
            infoMenuBuilder = new InfoMenuBuilder();
            infoMenuBuilder.ScanAndBuildTree();
            ShowMenu(infoMenuBuilder.GetRoot());
        }

        public void OnDeinitPanel()
        {
            ClearCurrentUI();
        }

        public void OnPanelShow() { }
        public void OnPanelHide() { }

        public void OnPanelTick()
        {
            if (currentLeafNode == null || currentInfoItems.Count == 0) return;

            var items = currentLeafNode.Info.GetInfoItems();
            for (int i = 0; i < currentInfoItems.Count; i++)
                currentInfoItems[i].UpdateValue(items[i].Value);
        }

        private void ShowMenu(InfoPathNode node)
        {
            if (node == null)
            {
                Debug.LogError($"[PFDebugger] ShowMenu: node is null.");
                return;
            }
            ClearCurrentUI();

            var firstNode = infoMenuBuilder.GetFirstWindowNode(node);
            if (firstNode == null) return;
            var path = infoMenuBuilder.GetPathFromRoot(firstNode);

            for (var i = 0; i < path.Count; i++)
            {
                var pathNode = path[i];
                if (pathNode.IsLeaf) break;
                var layout = PfGameObjPool.I.Get(BtnLayoutPrefab, transform);
                currentLayouts.Add(layout);

                var layoutRect = layout.GetComponent<RectTransform>();
                layoutRect.offsetMax = new Vector2(0, i * -28);
                layoutRect.offsetMin = new Vector2(0, (i + 1) * -28);

                foreach (var child in pathNode.Children)
                {
                    var btn = PfGameObjPool.I.Get(BtnPrefab, layout.transform);
                    currentButtons.Add(btn);

                    var item = btn.GetComponent<InfoMenuItem>();
                    item.SetText(child.Name);
                    item.SetHighlighted(path.Contains(child));
                    SetMenuAction(child, item);
                }
            }
            MainRect.SetAsLastSibling();
            MainRect.offsetMax = new Vector2(0, (path.Count - 1) * -28);

            ShowInfo(firstNode);
        }

        private void SetMenuAction(InfoPathNode child, InfoMenuItem item)
        {
            if (child.NodeType == InfoPathNodeType.Menu)
                item.AddListener(() => ShowMenu(child));
            else if (child.NodeType == InfoPathNodeType.Leaf)
                item.AddListener(() => ShowMenu(child));
        }

        private void ShowInfo(InfoPathNode leafNode)
        {
            ClearInfoItems();
            if (leafNode == null || leafNode.Info == null) return;

            currentLeafNode = leafNode;
            var items = leafNode.Info.GetInfoItems();
            if (items == null || items.Count == 0) return;

            for (int i = 0; i < items.Count; i++)
            {
                var obj = PfGameObjPool.I.Get(InfoItemPrefab, ItemContent);
                var infoItem = obj.GetComponent<InfoItem>();
                infoItem.SetText(items[i].DisplayName, items[i].Value);
                infoItem.SetColor(i % 2 == 0 ? ItemColor1 : ItemColor2);
                currentInfoItems.Add(infoItem);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(ItemContent);
        }

        private void ClearInfoItems()
        {
            currentLeafNode = null;

            foreach (var item in currentInfoItems)
            {
                if (item != null)
                    PfGameObjPool.I.Release(item.gameObject);
            }
            currentInfoItems.Clear();
        }

        private void ClearCurrentUI()
        {
            ClearInfoItems();

            foreach (var btn in currentButtons)
            {
                if (btn != null)
                {
                    btn.GetComponent<InfoMenuItem>().Reset();
                    PfGameObjPool.I.Release(btn);
                }
            }
            currentButtons.Clear();

            foreach (var layout in currentLayouts)
            {
                if (layout != null)
                {
                    PfGameObjPool.I.Release(layout);
                }
            }
            currentLayouts.Clear();
        }
    }
}