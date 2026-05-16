using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PFDebugger
{
    public class DebuggerMainWindow : MonoBehaviour
    {
        [SerializeField] private GameObject[] panelPrefabs;
        [SerializeField] private Transform tabBarContainer;
        [SerializeField] private Slider sizeSlider;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private Transform bottomContainer;
        [SerializeField] private GameObject tabButtonPrefab;
        [SerializeField] private GmBar gmBar;
        [SerializeField] private DragResizeBtn dragResizeBtn;
        [SerializeField] private float minimumWidth = 64f;
        [SerializeField] private float minimumHeight = 36f;
        
        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                gameObject.SetActive(isActive);
                if (isActive) OnShow();
                else OnHide();
            }
        }

        private readonly List<TabEntry> tabEntries = new();
        private readonly Dictionary<Type, GameObject> panelInstanceMap = new();
        private readonly Dictionary<int, IDebuggerPanel> panelCache = new();
        private readonly List<DebuggerTabButton> tabButtons = new();
        private int activePanelIndex = -1;

        private RectTransform titleRect;
        private RectTransform sizeSliderRect;
        private RectTransform contentRect;
        private RectTransform bottomRect;
        private RectTransform rootRect;
        private RectTransform windowRect;
        private float resizeDragStartPointerX;
        private float resizeDragStartPointerY;
        private float resizeDragStartRight;
        private float resizeDragStartBottom;

        internal void OnInit()
        {
            rootRect = DebuggerManager.I.RectTransform;
            windowRect = transform as RectTransform;
            titleRect = tabBarContainer as RectTransform;
            sizeSliderRect = sizeSlider.GetComponent<RectTransform>();
            contentRect = contentContainer as RectTransform;
            bottomRect = bottomContainer as RectTransform;

            sizeSlider.value = 1 - DebuggerManager.I.CanvasScaler.matchWidthOrHeight;
            sizeSlider.onValueChanged.AddListener(OnSizeSliderChange);
            GenerateWindow();
            GenerateTabButtons();
            gmBar.OnInitBar();
        }

        internal void OnDeInit()
        {
            sizeSlider.onValueChanged.RemoveListener(OnSizeSliderChange);
            gmBar.OnDeinitBar();

            foreach (var kvp in panelInstanceMap)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.TryGetComponent<IDebuggerPanel>(out var panel))
                        panel.OnDeinitPanel();
                    Destroy(kvp.Value);
                }
            }
            panelInstanceMap.Clear();
            panelCache.Clear();
            tabEntries.Clear();

            foreach (var btn in tabButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            tabButtons.Clear();
            activePanelIndex = -1;
        }

        private void OnSizeSliderChange(float value)
        {
            DebuggerManager.I.CanvasScaler.matchWidthOrHeight =1 - value;
            RefreshContentOffset();
        }

        internal void Tick()
        {
            if (activePanelIndex < 0 || activePanelIndex >= tabEntries.Count) return;
            var entry = tabEntries[activePanelIndex];
            if (entry.Type != TabEntryType.Panel) return;
            panelCache[activePanelIndex].OnPanelTick();
        }

        private void OnShow()
        {
            RefreshContentOffset();
            gmBar.OnBarShow();
            if (activePanelIndex >= 0) return;
            for (int i = 0; i < tabEntries.Count; i++)
            {
                if (tabEntries[i].Type == TabEntryType.Panel)
                {
                    ShowPanel(i);
                    return;
                }
            }
        }

        private void OnHide()
        {
            gmBar.OnBarHide();
        }

        private void ShowPanel(int index)
        {
            if (activePanelIndex == index) return;
            if (activePanelIndex >= 0)
                HidePanel(activePanelIndex);

            activePanelIndex = index;

            for (int i = 0; i < tabButtons.Count; i++)
                tabButtons[i].SetSelected(i == index);

            var panelObj = panelInstanceMap[tabEntries[index].PanelType];
            panelObj.SetActive(true);
            panelCache[index].OnPanelShow();
        }

        private void HidePanel(int index)
        {
            var entry = tabEntries[index];
            if (entry.Type != TabEntryType.Panel) return;

            var panelObj = panelInstanceMap[entry.PanelType];
            panelCache[index].OnPanelHide();
            panelObj.SetActive(false);
        }

        private void GenerateWindow()
        {
            var prefabMap = new Dictionary<Type, GameObject>();
            if (panelPrefabs != null)
            {
                foreach (var prefab in panelPrefabs)
                {
                    if (prefab == null) continue;
                    var panel = prefab.GetComponent<IDebuggerPanel>();
                    if (panel != null)
                        prefabMap[panel.GetType()] = prefab;
                }
            }

            tabEntries.AddRange(TabDiscovery.DiscoverTabs());

            for (int i = tabEntries.Count - 1; i >= 0; i--)
            {
                var entry = tabEntries[i];
                if (entry.Type == TabEntryType.Panel)
                {
                    if (!prefabMap.TryGetValue(entry.PanelType, out var prefab))
                    {
                        tabEntries.RemoveAt(i);
                        continue;
                    }

                    var panelObj = Instantiate(prefab, contentContainer);
                    panelObj.transform.localPosition = Vector3.zero;
                    panelObj.transform.localScale = Vector3.one;
                    var panel = panelObj.GetComponent<IDebuggerPanel>();
                    panel?.OnInitPanel();
                    panelObj.SetActive(false);
                    panelInstanceMap[entry.PanelType] = panelObj;
                    panelCache[i] = panel;
                }
            }
        }

        private void GenerateTabButtons()
        {
            for (int i = 0; i < tabEntries.Count; i++)
            {
                var btnObj = Instantiate(tabButtonPrefab, tabBarContainer);
                btnObj.name = $"Tab_{tabEntries[i].Name}";

                var tabBtn = btnObj.GetComponent<DebuggerTabButton>();
                if (tabBtn == null)
                    tabBtn = btnObj.AddComponent<DebuggerTabButton>();

                tabBtn.Initialize(tabEntries[i].Name, OnTabButtonClicked, tabEntries[i].Type);
                tabButtons.Add(tabBtn);
            }

            if (titleRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(titleRect);
        }

        private void OnTabButtonClicked(DebuggerTabButton button)
        {
            int index = tabButtons.IndexOf(button);
            if (index < 0) return;

            var entry = tabEntries[index];

            if (entry.Type == TabEntryType.Method)
            {
                entry.Method?.Invoke(null, null);
                return;
            }

            if (index != activePanelIndex)
                ShowPanel(index);
        }

        private void RefreshContentOffset()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleRect);
            contentRect.offsetMax = new Vector2(0, -titleRect.rect.height - sizeSliderRect.rect.height);
            contentRect.offsetMin = new Vector2(0, bottomRect.rect.height);
        }

        internal void BeginResize(PointerEventData eventData)
        {
            if (rootRect == null || windowRect == null)
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position,
                    eventData.pressEventCamera, out localPoint))
                return;

            resizeDragStartPointerX = localPoint.x;
            resizeDragStartPointerY = localPoint.y;
            resizeDragStartRight = rootRect.rect.width + windowRect.offsetMax.x;
            resizeDragStartBottom = windowRect.offsetMin.y;
        }

        internal void Resize(PointerEventData eventData)
        {
            if (rootRect == null || windowRect == null)
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, eventData.position,
                    eventData.pressEventCamera, out localPoint))
                return;

            float deltaX = localPoint.x - resizeDragStartPointerX;
            float deltaY = localPoint.y - resizeDragStartPointerY;
            float rootWidth = rootRect.rect.width;
            float rootHeight = rootRect.rect.height;
            float left = windowRect.offsetMin.x;
            float top = -windowRect.offsetMax.y;
            float minimumRight = left + Mathf.Min(minimumWidth, rootWidth - left);
            float right = Mathf.Clamp(resizeDragStartRight + deltaX, minimumRight, rootWidth);
            float maxBottom = Mathf.Max(0f, rootHeight - top - minimumHeight);
            float bottom = Mathf.Clamp(resizeDragStartBottom + deltaY, 0f, maxBottom);

            Vector2 offsetMin = windowRect.offsetMin;
            Vector2 offsetMax = windowRect.offsetMax;

            offsetMin.y = bottom;
            offsetMax.x = right - rootWidth;

            windowRect.offsetMin = offsetMin;
            windowRect.offsetMax = offsetMax;
            
            RefreshContentOffset();
        }
    }
}
