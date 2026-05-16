using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PFDebugger
{
    public enum TimestampDisplayMode
    {
        Hidden = 0,
        ExpandedOnly = 1,
        Always = 2
    }

    public class LogListView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("References")] [SerializeField]
        private RectTransform contentTransform;
        [SerializeField] private RectTransform viewportTransform;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private GameObject logItemPrefab;

        [Header("Item Colors")] [SerializeField]
        private Color logItemNormalColor1 = new Color(0.20f, 0.20f, 0.20f, 1f);
        [SerializeField] private Color logItemNormalColor2 = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color logItemSelectedColor = new Color(0.18f, 0.40f, 0.65f, 1f);

        [Header("Icons")] [SerializeField] private Sprite infoSprite;
        [SerializeField] private Sprite warningSprite;
        [SerializeField] private Sprite errorSprite;

        [Header("Text")] [SerializeField]
        private TimestampDisplayMode timestampDisplayMode = TimestampDisplayMode.ExpandedOnly;
        [SerializeField] private int maxCollapsedLogLength = 200;
        [SerializeField] private int maxExpandedLogLength = 10000;

        private readonly DynamicCircularBuffer<LogItem> visibleLogItems = new DynamicCircularBuffer<LogItem>(32);
        private readonly Stack<LogItem> pooledLogItems = new Stack<LogItem>(32);

        private LogManager manager;
        private UnityAction<Vector2> onScrollValueChanged;
        private Scrollbar verticalScrollbar;
        private EventTrigger scrollbarEventTrigger;
        private bool scrollbarListenersInstalled;
        private PointerEventData nullPointerEventData;

        private float logItemHeight = 32f;
        private LogEntry selectedLogEntry;
        private int indexOfSelectedLogEntry = int.MaxValue;
        private float heightOfSelectedLogEntry;
        private float DeltaHeightOfSelectedLogEntry => heightOfSelectedLogEntry - logItemHeight;

        private int currentTopIndex = -1;
        private int currentBottomIndex = -1;
        private bool viewportSizeChanged;
        private bool initialized;

        private const float SNAP_BOTTOM_THRESHOLD = 1E-6f;

        public bool SnapToBottom { get; set; } = true;
        public bool ShouldShowSnapToBottomButton =>
            !SnapToBottom &&
            scrollView != null &&
            scrollView.verticalNormalizedPosition > SNAP_BOTTOM_THRESHOLD &&
            scrollView.verticalNormalizedPosition < 0.9999f;
        public float ItemHeight => logItemHeight;
        public float SelectedItemHeight => heightOfSelectedLogEntry > 0f ? heightOfSelectedLogEntry : logItemHeight;
        public TimestampDisplayMode TimestampMode => timestampDisplayMode;
        public int MaxCollapsedLogLength => maxCollapsedLogLength;
        public int MaxExpandedLogLength => maxExpandedLogLength;

        private int VisibleEntryCount => manager != null ? manager.VisibleCount : 0;

        private void Awake()
        {
            if (contentTransform == null)
                contentTransform = transform as RectTransform;
        }

        private void OnDestroy()
        {
            if (scrollView != null && onScrollValueChanged != null)
                scrollView.onValueChanged.RemoveListener(onScrollValueChanged);
        }

        public void Initialize(LogManager manager, float logItemHeight)
        {
            this.manager = manager;
            this.logItemHeight = Mathf.Max(1f, logItemHeight);
            nullPointerEventData ??= new PointerEventData(null);
            onScrollValueChanged ??= OnScrollValueChanged;

            scrollView.onValueChanged.AddListener(onScrollValueChanged);
            SetupScrollbarDragListeners();

            initialized = true;
            CalculateContentHeight();
            UpdateItemsInTheList(true);
        }

        public void SetCollapseMode(bool collapse)
        {
            manager?.SetCollapse(collapse);
        }

        public void OnLogItemClicked(LogItem item)
        {
            if (item == null)
                return;

            OnLogItemClickedInternal(item.Index, item);
        }

        public void SelectAndFocusOnLogItemAtIndex(int itemIndex)
        {
            int totalCount = VisibleEntryCount;
            if (itemIndex < 0 || itemIndex >= totalCount)
                return;

            if (indexOfSelectedLogEntry != itemIndex)
                OnLogItemClickedInternal(itemIndex);

            if (scrollView == null || viewportTransform == null || contentTransform == null)
                return;

            float viewportHeight = viewportTransform.rect.height;
            float contentCenterYAtTop = viewportHeight * 0.5f;
            float contentCenterYAtBottom = contentTransform.sizeDelta.y - viewportHeight * 0.5f;
            float targetCenterY = itemIndex * logItemHeight + viewportHeight * 0.5f;

            if (Mathf.Approximately(contentCenterYAtTop, contentCenterYAtBottom))
                scrollView.verticalNormalizedPosition = 0.5f;
            else
                scrollView.verticalNormalizedPosition = Mathf.Clamp01(
                    Mathf.InverseLerp(contentCenterYAtBottom, contentCenterYAtTop, targetCenterY));

            SnapToBottom = false;
        }

        public void OnLogEntriesUpdated(bool fullRefresh = false)
        {
            if (!initialized || manager == null)
                return;

            RefreshSelectedLogEntry();
            CalculateContentHeight();
            UpdateItemsInTheList(fullRefresh);
            if (!fullRefresh && manager.CollapseEnabled)
                RefreshCollapsedLogEntryCounts();

            if (SnapToBottom)
                SetScrollToBottom();

            if (fullRefresh)
                ValidateScrollPosition();
        }

        public void OnCollapsedLogEntryAtIndexUpdated(int index)
        {
            if (index >= currentTopIndex && index <= currentBottomIndex && currentTopIndex >= 0)
            {
                LogItem logItem = GetLogItemAtIndex(index);
                if (logItem != null)
                    logItem.ShowCount();
            }
        }

        public void RefreshCollapsedLogEntryCounts()
        {
            for (int i = 0; i < visibleLogItems.Count; i++)
            {
                if (visibleLogItems[i] != null)
                    visibleLogItems[i].ShowCount();
            }
        }

        public void DeselectSelectedLogItem()
        {
            selectedLogEntry = null;
            indexOfSelectedLogEntry = int.MaxValue;
            heightOfSelectedLogEntry = 0f;
        }

        public void ScrollToBottom()
        {
            SnapToBottom = true;
            SetScrollToBottom();
        }

        public Sprite GetLogTypeSprite(LogType logType)
        {
            switch (logType)
            {
                case LogType.Warning:
                    return warningSprite;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return errorSprite;
                default:
                    return infoSprite;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            viewportSizeChanged = true;
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            if (viewportSizeChanged)
            {
                viewportSizeChanged = false;
                OnViewportSizeChanged();
            }
        }

        private void OnViewportSizeChanged()
        {
            int totalCount = VisibleEntryCount;
            if (indexOfSelectedLogEntry >= totalCount)
            {
                DeselectSelectedLogItem();
                UpdateItemsInTheList(false);
                return;
            }

            if (selectedLogEntry != null)
                CalculateSelectedLogEntryHeight();

            CalculateContentHeight();
            UpdateItemsInTheList(true);
            ValidateScrollPosition();
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            if (initialized)
            {
                UpdateItemsInTheList(false);
                SnapToBottom = IsScrollAtBottom();
            }
        }

        private void OnLogItemClickedInternal(int itemIndex, LogItem referenceItem = null)
        {
            int previousSelectedIndex = indexOfSelectedLogEntry;
            DeselectSelectedLogItem();

            if (previousSelectedIndex != itemIndex)
            {
                selectedLogEntry = manager.GetVisibleLog(itemIndex);
                indexOfSelectedLogEntry = itemIndex;
                CalculateSelectedLogEntryHeight(referenceItem);

                SnapToBottom = false;
            }

            CalculateContentHeight();
            UpdateItemsInTheList(true);
            ValidateScrollPosition();
        }

        private void RefreshSelectedLogEntry()
        {
            if (selectedLogEntry == null)
                return;

            int totalCount = VisibleEntryCount;
            if (totalCount <= 0)
            {
                DeselectSelectedLogItem();
                return;
            }

            if (indexOfSelectedLogEntry < totalCount &&
                manager.GetVisibleLog(indexOfSelectedLogEntry) == selectedLogEntry)
                return;

            int newIndex = FindIndexOfLogEntry(selectedLogEntry);
            if (newIndex < 0)
                DeselectSelectedLogItem();
            else
                indexOfSelectedLogEntry = newIndex;
        }

        private int FindIndexOfLogEntry(LogEntry entry)
        {
            for (int i = 0; i < VisibleEntryCount; i++)
            {
                if (manager.GetVisibleLog(i) == entry)
                    return i;
            }

            return -1;
        }

        private void ValidateScrollPosition()
        {
            if (scrollView == null)
                return;

            if (scrollView.verticalNormalizedPosition <= Mathf.Epsilon)
                scrollView.verticalNormalizedPosition = 0.0001f;

            scrollView.OnScroll(nullPointerEventData);
        }

        private bool IsScrollAtBottom()
        {
            return scrollView != null && scrollView.verticalNormalizedPosition <= SNAP_BOTTOM_THRESHOLD;
        }

        private void SetScrollToBottom()
        {
            if (scrollView == null)
                return;

            scrollView.verticalNormalizedPosition = 0f;
            SnapToBottom = true;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData != null && eventData.IsScrolling())
                SnapToBottom = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            SnapToBottom = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SnapToBottom = IsScrollAtBottom();
        }

        public void OnScrollbarDragStart(BaseEventData eventData)
        {
            SnapToBottom = false;
        }

        public void OnScrollbarDragEnd(BaseEventData eventData)
        {
            SnapToBottom = IsScrollAtBottom();
        }

        private void SetupScrollbarDragListeners()
        {
            if (scrollbarListenersInstalled)
                return;

            verticalScrollbar = scrollView.verticalScrollbar;
            if (verticalScrollbar == null)
                return;

            scrollbarEventTrigger = verticalScrollbar.GetComponent<EventTrigger>();
            if (scrollbarEventTrigger == null)
                scrollbarEventTrigger = verticalScrollbar.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(EventTriggerType.BeginDrag, OnScrollbarDragStart);
            AddEventTrigger(EventTriggerType.EndDrag, OnScrollbarDragEnd);
            scrollbarListenersInstalled = true;
        }

        private void AddEventTrigger(EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            if (scrollbarEventTrigger == null || callback == null)
                return;

            if (scrollbarEventTrigger.triggers == null)
                scrollbarEventTrigger.triggers = new List<EventTrigger.Entry>();

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(callback);
            scrollbarEventTrigger.triggers.Add(entry);
        }

        private void CalculateContentHeight()
        {
            float newHeight = Mathf.Max(1f, VisibleEntryCount * logItemHeight);
            if (selectedLogEntry != null)
                newHeight += DeltaHeightOfSelectedLogEntry;

            if (contentTransform != null)
                contentTransform.sizeDelta = new Vector2(0f, newHeight);
        }

        private void CalculateSelectedLogEntryHeight(LogItem referenceItem = null)
        {
            if (selectedLogEntry == null)
            {
                heightOfSelectedLogEntry = 0f;
                return;
            }

            if (referenceItem == null)
            {
                if (visibleLogItems.Count == 0)
                {
                    UpdateItemsInTheList(false);
                    if (visibleLogItems.Count == 0)
                        return;
                }

                referenceItem = visibleLogItems[0];
            }

            heightOfSelectedLogEntry = referenceItem.CalculateExpandedHeight(selectedLogEntry, indexOfSelectedLogEntry);
        }

        private void UpdateItemsInTheList(bool updateAllVisibleItemContents)
        {
            int totalCount = VisibleEntryCount;
            if (totalCount <= 0 || contentTransform == null || viewportTransform == null)
            {
                if (currentTopIndex != -1)
                {
                    visibleLogItems.TrimStart(visibleLogItems.Count, PoolLogItem);
                    currentTopIndex = -1;
                    currentBottomIndex = -1;
                }

                return;
            }

            float contentPosTop = contentTransform.anchoredPosition.y - 1f;
            float contentPosBottom = contentPosTop + viewportTransform.rect.height + 2f;
            float positionOfSelectedLogEntry = indexOfSelectedLogEntry * logItemHeight;

            if (positionOfSelectedLogEntry <= contentPosBottom)
            {
                if (positionOfSelectedLogEntry <= contentPosTop)
                {
                    contentPosTop = Mathf.Max(contentPosTop - DeltaHeightOfSelectedLogEntry,
                        positionOfSelectedLogEntry - 1f);
                    contentPosBottom = Mathf.Max(contentPosBottom - DeltaHeightOfSelectedLogEntry, contentPosTop + 2f);
                }
                else
                    contentPosBottom = Mathf.Max(contentPosBottom - DeltaHeightOfSelectedLogEntry,
                        positionOfSelectedLogEntry + 1f);
            }

            int newBottomIndex = Mathf.Min((int)(contentPosBottom / logItemHeight), totalCount - 1);
            int newTopIndex = Mathf.Clamp((int)(contentPosTop / logItemHeight), 0, newBottomIndex);

            if (currentTopIndex == -1)
            {
                updateAllVisibleItemContents = true;
                for (int i = 0, count = newBottomIndex - newTopIndex + 1; i < count; i++)
                    visibleLogItems.Add(PopLogItem());
            }
            else
            {
                if (newBottomIndex < currentTopIndex || newTopIndex > currentBottomIndex)
                {
                    updateAllVisibleItemContents = true;
                    visibleLogItems.TrimStart(visibleLogItems.Count, PoolLogItem);
                    for (int i = 0, count = newBottomIndex - newTopIndex + 1; i < count; i++)
                        visibleLogItems.Add(PopLogItem());
                }
                else
                {
                    if (newTopIndex > currentTopIndex)
                        visibleLogItems.TrimStart(newTopIndex - currentTopIndex, PoolLogItem);

                    if (newBottomIndex < currentBottomIndex)
                        visibleLogItems.TrimEnd(currentBottomIndex - newBottomIndex, PoolLogItem);

                    if (newTopIndex < currentTopIndex)
                    {
                        for (int i = 0, count = currentTopIndex - newTopIndex; i < count; i++)
                            visibleLogItems.AddFirst(PopLogItem());

                        if (!updateAllVisibleItemContents)
                            UpdateLogItemContentsBetweenIndices(newTopIndex, currentTopIndex - 1, newTopIndex);
                    }

                    if (newBottomIndex > currentBottomIndex)
                    {
                        for (int i = 0, count = newBottomIndex - currentBottomIndex; i < count; i++)
                            visibleLogItems.Add(PopLogItem());

                        if (!updateAllVisibleItemContents)
                            UpdateLogItemContentsBetweenIndices(currentBottomIndex + 1, newBottomIndex, newTopIndex);
                    }
                }
            }

            currentTopIndex = newTopIndex;
            currentBottomIndex = newBottomIndex;

            if (updateAllVisibleItemContents)
                UpdateLogItemContentsBetweenIndices(currentTopIndex, currentBottomIndex, newTopIndex);
        }

        private LogItem GetLogItemAtIndex(int index)
        {
            return visibleLogItems[index - currentTopIndex];
        }

        private void UpdateLogItemContentsBetweenIndices(int topIndex, int bottomIndex, int logItemOffset)
        {
            bool collapseEnabled = manager != null && manager.CollapseEnabled;

            for (int i = topIndex; i <= bottomIndex; i++)
            {
                LogItem logItem = visibleLogItems[i - logItemOffset];
                if (logItem == null)
                    continue;

                LogEntry entry = manager.GetVisibleLog(i);
                if (entry == null)
                    continue;

                logItem.SetContent(entry, i, i == indexOfSelectedLogEntry);
                RepositionLogItem(logItem);
                ColorLogItem(logItem);

                if (collapseEnabled)
                    logItem.ShowCount();
                else
                    logItem.HideCount();
            }
        }

        private void RepositionLogItem(LogItem logItem)
        {
            int index = logItem.Index;
            Vector2 anchoredPosition = new Vector2(1f, -index * logItemHeight);
            if (index > indexOfSelectedLogEntry)
                anchoredPosition.y -= DeltaHeightOfSelectedLogEntry;

            logItem.Transform.anchoredPosition = anchoredPosition;
        }

        private void ColorLogItem(LogItem logItem)
        {
            int index = logItem.Index;
            if (index == indexOfSelectedLogEntry)
                logItem.Image.color = logItemSelectedColor;
            else if (index % 2 == 0)
                logItem.Image.color = logItemNormalColor1;
            else
                logItem.Image.color = logItemNormalColor2;
        }

        private LogItem PopLogItem()
        {
            if (pooledLogItems.Count > 0)
            {
                LogItem pooledItem = pooledLogItems.Pop();
                if (pooledItem != null)
                {
                    pooledItem.gameObject.SetActive(true);
                    return pooledItem;
                }
            }

            if (logItemPrefab == null || contentTransform == null)
                return null;

            GameObject newObject = Instantiate(logItemPrefab, contentTransform);
            LogItem newItem = newObject.GetComponent<LogItem>();
            if (newItem != null)
                newItem.Initialize(this);
            return newItem;
        }

        private void PoolLogItem(LogItem logItem)
        {
            if (logItem == null)
                return;

            logItem.gameObject.SetActive(false);
            pooledLogItems.Push(logItem);
        }
    }
}
