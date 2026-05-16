using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
#endif

namespace PFDebugger
{
    public class LogItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform transformComponent;
        public RectTransform Transform => transformComponent;

        [SerializeField] private Image imageComponent;
        public Image Image => imageComponent;

        [SerializeField] private CanvasGroup canvasGroupComponent;

        [SerializeField] private DebuggerText logText;
        [SerializeField] private Image logTypeImage;

        [SerializeField] private GameObject logCountParent;
        [SerializeField] private DebuggerText logCountText;

        [SerializeField] private Button copyLogButton;

        private LogEntry logEntry;
        public LogEntry Entry => logEntry;

        [System.NonSerialized] public int Index;

        private bool isExpanded;
        private LogListView listView;

        private Vector2 logTextOriginalPosition;
        private Vector2 logTextOriginalSize;
        private float copyLogButtonHeight;

        private readonly StringBuilder textBuilder = new StringBuilder(512);

        public void Initialize(LogListView listView)
        {
            this.listView = listView;

            transformComponent = transform as RectTransform;
            imageComponent = GetComponent<Image>();

            logTextOriginalPosition = logText.rectTransform.anchoredPosition;
            logTextOriginalSize = logText.rectTransform.sizeDelta;

            RectTransform buttonTransform = copyLogButton.transform as RectTransform;
            copyLogButtonHeight = buttonTransform.anchoredPosition.y + buttonTransform.sizeDelta.y + 2f;

            copyLogButton.onClick.RemoveListener(CopyLog);
            copyLogButton.onClick.AddListener(CopyLog);
        }

        public void SetContent(LogEntry logEntry, int entryIndex, bool isExpanded)
        {
            this.logEntry = logEntry;
            this.Index = entryIndex;
            this.isExpanded = isExpanded;

            Vector2 size = transformComponent.sizeDelta;
            if (isExpanded)
            {
                size.y = listView.SelectedItemHeight;
                EnableCopyButtonLayout();
            }
            else
            {
                size.y = listView.ItemHeight;
                DisableCopyButtonLayout();
            }

            transformComponent.sizeDelta = size;

            SetText(logEntry, isExpanded);

            logTypeImage.sprite = listView.GetLogTypeSprite(logEntry.LogType);
        }

        public void RefreshCollapseCount()
        {
            if (logEntry.Count > 1)
                ShowCount();
            else
                HideCount();
        }

        public void ShowCount()
        {
            logCountText.text = logEntry.Count.ToString();

            if (logCountParent != null && !logCountParent.activeSelf)
                logCountParent.SetActive(true);
        }

        public void HideCount()
        {
            if (logCountParent != null && logCountParent.activeSelf)
                logCountParent.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (logEntry == null || listView == null)
                return;

#if UNITY_EDITOR
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Match regex = Regex.Match(logEntry.StackTrace ?? string.Empty, @"\(at .*\.cs:[0-9]+\)$",
                    RegexOptions.Multiline);
                if (regex.Success)
                {
                    string line = logEntry.StackTrace.Substring(regex.Index + 4, regex.Length - 5);
                    int lineSeparator = line.IndexOf(':');
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(line.Substring(0, lineSeparator));
                    if (script != null)
                        AssetDatabase.OpenAsset(script, int.Parse(line.Substring(lineSeparator + 1)));
                }
            }
            else
                listView.OnLogItemClicked(this);
#else
            listView.OnLogItemClicked(this);
#endif
        }

        public float CalculateExpandedHeight(LogEntry logEntry)
        {
            return CalculateExpandedHeight(logEntry, Index);
        }

        public float CalculateExpandedHeight(LogEntry logEntry, int entryIndex)
        {
            if (transformComponent == null || this.logText == null || logEntry == null)
                return listView.ItemHeight;

            string text = BuildDisplayText(logEntry, true);
            float textWidth = Mathf.Max(1f, logText.rectTransform.rect.width);
            TextGenerationSettings settings = logText.GetGenerationSettings(new Vector2(textWidth, 0f));
            float preferredHeight = logText.cachedTextGeneratorForLayout.GetPreferredHeight(text, settings) /
                                    logText.pixelsPerUnit;
            float result = preferredHeight + copyLogButtonHeight;
            return Mathf.Max(listView.ItemHeight, result);
        }

        private void EnableCopyButtonLayout()
        {
            if (copyLogButton == null)
                return;

            if (!copyLogButton.gameObject.activeSelf)
            {
                copyLogButton.gameObject.SetActive(true);

                if (logText != null)
                {
                    logText.rectTransform.anchoredPosition = new Vector2(
                        logTextOriginalPosition.x,
                        logTextOriginalPosition.y + copyLogButtonHeight * 0.5f);
                    logText.rectTransform.sizeDelta = logTextOriginalSize - new Vector2(0f, copyLogButtonHeight);
                }
            }
        }

        private void DisableCopyButtonLayout()
        {
            if (copyLogButton == null)
                return;

            if (copyLogButton.gameObject.activeSelf)
            {
                copyLogButton.gameObject.SetActive(false);

                if (logText != null)
                {
                    logText.rectTransform.anchoredPosition = logTextOriginalPosition;
                    logText.rectTransform.sizeDelta = logTextOriginalSize;
                }
            }
        }

        private void SetText(LogEntry entry, bool expanded)
        {
            if (logText == null || entry == null)
                return;

            logText.text = BuildDisplayText(entry, expanded);
        }

        private string BuildDisplayText(LogEntry entry, bool expanded)
        {
            string text = expanded ? entry.ToString() : entry.LogString;
            text ??= string.Empty;
            int maxLength = expanded ? listView.MaxExpandedLogLength : listView.MaxCollapsedLogLength;

            if (maxLength > 0 && text.Length > maxLength)
                text = text.Substring(0, maxLength);

            bool showTimestamp = listView.TimestampMode switch
            {
                TimestampDisplayMode.Hidden => false,
                TimestampDisplayMode.ExpandedOnly => expanded,
                TimestampDisplayMode.Always => true,
                _ => expanded
            };
            if (!showTimestamp)
                return text;

            textBuilder.Length = 0;
            if (expanded)
            {
                textBuilder.Append(entry.Timestamp.dateTime.ToString("HH:mm:ss.fff"));
                textBuilder.Append(" [F");
                textBuilder.Append(entry.Timestamp.frameCount);
                textBuilder.Append(" E");
                textBuilder.Append(entry.Timestamp.elapsedSeconds.ToString("F3"));
                textBuilder.Append("] ");
            }
            else
            {
                textBuilder.Append(entry.Timestamp.dateTime.ToString("HH:mm:ss.fff"));
                textBuilder.Append(' ');
            }

            textBuilder.Append(text);
            return textBuilder.ToString();
        }

        private void CopyLog()
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            string log = GetCopyContent();
            if (!string.IsNullOrEmpty(log))
                GUIUtility.systemCopyBuffer = log;
#endif
        }

        internal string GetCopyContent()
        {
            if (logEntry == null)
                return string.Empty;

            textBuilder.Length = 0;
            textBuilder.Append(logEntry.Timestamp.dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            textBuilder.Append(" [F");
            textBuilder.Append(logEntry.Timestamp.frameCount);
            textBuilder.Append(" E");
            textBuilder.Append(logEntry.Timestamp.elapsedSeconds.ToString("F3"));
            textBuilder.Append("] ");
            textBuilder.Append(logEntry.ToString());
            return textBuilder.ToString();
        }

        public override string ToString()
        {
            return logEntry != null ? logEntry.ToString() : string.Empty;
        }
    }
}
