using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    [DebuggerTab("LogPanel", 1)]
    public class LogPanel : MonoBehaviour, IDebuggerPanel
    {
        [SerializeField] private LogListView listView;

        [SerializeField] private Button clearButton;
        [SerializeField] private Button uploadButton;
        [SerializeField] private Button collapseButton;
        [SerializeField] private Button filterLogButton;
        [SerializeField] private Button filterWarningButton;
        [SerializeField] private Button filterErrorButton;
        [SerializeField] private Button snapToBottomButton;

        [SerializeField] private InputField searchbarInput;

        [SerializeField] private DebuggerText infoCountText;
        [SerializeField] private DebuggerText warningCountText;
        [SerializeField] private DebuggerText errorCountText;

        [SerializeField] private Color normalButtonColor = new Color(0.31617647f, 0.31617647f, 0.31617647f, 1f);
        [SerializeField] private Color selectedButtonColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color collapseNormalColor = new Color(0.31617647f, 0.31617647f, 0.31617647f, 1f);
        [SerializeField] private Color collapseSelectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);


        
        private int lastInfoCount = -1;
        private int lastWarningCount = -1;
        private int lastErrorCount = -1;
        private LogManager logManager;

        public void OnInitPanel()
        {
            logManager = Debugger.LogManager;
            listView.Initialize(logManager, 32f);
            listView.SetCollapseMode(logManager.CollapseEnabled);

            RegisterUIEvents();
            logManager.OnLogChanged += OnLogChanged;
        }

        public void OnDeinitPanel()
        {
            UnregisterUIEvents();
            logManager.OnLogChanged -= OnLogChanged;
        }

        public void OnPanelShow()
        {
            RefreshAllUI(fullRefresh: true);
        }

        public void OnPanelHide() { }

        public void OnPanelTick()
        {
            if (lastInfoCount != logManager.InfoCount)
            {
                lastInfoCount = logManager.InfoCount;
                infoCountText.text = logManager.InfoCount.ToString();
            }
            if (lastWarningCount != logManager.WarningCount)
            {
                lastWarningCount = logManager.WarningCount;
                warningCountText.text = logManager.WarningCount.ToString();
            }
            if (lastErrorCount != logManager.ErrorCount)
            {
                lastErrorCount = logManager.ErrorCount;
                errorCountText.text = logManager.ErrorCount.ToString();
            }
            RefreshButtonVisuals();
        }

        private void OnLogChanged()
        {
            RefreshAllUI(fullRefresh: false);
        }

        private void RefreshAllUI(bool fullRefresh)
        {
            if (logManager == null)
                return;

            listView?.OnLogEntriesUpdated(fullRefresh);
        }

        private void RefreshButtonVisuals()
        {
            if (logManager == null)
                return;

            bool infoOn = (logManager.Filter & LogLevel.Info) == LogLevel.Info;
            bool warningOn = (logManager.Filter & LogLevel.Warning) == LogLevel.Warning;
            bool errorOn = (logManager.Filter & LogLevel.Error) == LogLevel.Error;

            SetButtonColor(filterLogButton, infoOn ? selectedButtonColor : normalButtonColor);
            SetButtonColor(filterWarningButton, warningOn ? selectedButtonColor : normalButtonColor);
            SetButtonColor(filterErrorButton, errorOn ? selectedButtonColor : normalButtonColor);
            SetButtonColor(collapseButton, logManager.CollapseEnabled ? collapseSelectedColor : collapseNormalColor);

            bool shouldShow = listView.ShouldShowSnapToBottomButton;
            if (snapToBottomButton.gameObject.activeSelf != shouldShow)
                snapToBottomButton.gameObject.SetActive(shouldShow);
        }

        private void SetButtonColor(Button button, Color color)
        {
            if (button == null)
                return;

            if (button.targetGraphic is Image image)
                image.color = color;
        }

        private void RegisterUIEvents()
        {
            clearButton.onClick.AddListener(OnClearClicked);
            uploadButton.onClick.AddListener(OnUploadClicked);
            collapseButton.onClick.AddListener(OnCollapseClicked);
            filterLogButton.onClick.AddListener(OnFilterLogClicked);
            filterWarningButton.onClick.AddListener(OnFilterWarningClicked);
            filterErrorButton.onClick.AddListener(OnFilterErrorClicked);
            snapToBottomButton.onClick.AddListener(OnSnapToBottomClicked);
            searchbarInput.onValueChanged.AddListener(OnSearchValueChanged);
        }

        private void UnregisterUIEvents()
        {
            clearButton.onClick.RemoveListener(OnClearClicked);
            uploadButton.onClick.RemoveListener(OnUploadClicked);
            collapseButton.onClick.RemoveListener(OnCollapseClicked);
            filterLogButton.onClick.RemoveListener(OnFilterLogClicked);
            filterWarningButton.onClick.RemoveListener(OnFilterWarningClicked);
            filterErrorButton.onClick.RemoveListener(OnFilterErrorClicked);
            snapToBottomButton.onClick.RemoveListener(OnSnapToBottomClicked);
            searchbarInput.onValueChanged.RemoveListener(OnSearchValueChanged);
        }

        private void OnClearClicked()
        {
            if (logManager == null)
                return;

            logManager.Clear();
            if (searchbarInput != null && !string.IsNullOrEmpty(searchbarInput.text))
                searchbarInput.SetTextWithoutNotify(string.Empty);

            RefreshAllUI(fullRefresh: true);
        }
        private void OnCollapseClicked()
        {
            if (logManager == null)
                return;

            bool collapse = !logManager.CollapseEnabled;
            logManager.SetCollapse(collapse);
            listView?.SetCollapseMode(collapse);
            RefreshAllUI(fullRefresh: true);
        }

        private void OnFilterLogClicked()
        {
            ToggleFilter(LogLevel.Info);
        }

        private void OnFilterWarningClicked()
        {
            ToggleFilter(LogLevel.Warning);
        }

        private void OnFilterErrorClicked()
        {
            ToggleFilter(LogLevel.Error);
        }

        private void OnSearchValueChanged(string text)
        {
            logManager.SetSearchTerm(text);
            RefreshAllUI(fullRefresh: true);
        }

        private void OnSnapToBottomClicked()
        {
            listView?.ScrollToBottom();
            RefreshButtonVisuals();
        }

        private void ToggleFilter(LogLevel level)
        {
            logManager.ToggleFilter(level);
            RefreshAllUI(fullRefresh: true);
        }
        
        private void OnUploadClicked()
        {
            
        }
    }
}
