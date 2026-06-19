using System;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class GmSuggestionItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private DebuggerText commandText;
        [SerializeField] private DebuggerText descriptionText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.82f, 0.82f, 0.82f, 1f);

        private GmCommandInfo commandInfo;
        private Action<GmCommandInfo> clickCallback;

        public GmCommandInfo CommandInfo => commandInfo;

        private void Awake()
        {
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        public void Bind(GmCommandInfo info, Action<GmCommandInfo> onClick)
        {
            ResetItem();
            commandInfo = info;
            clickCallback = onClick;
            commandText.text = info.Signature;
            descriptionText.text = info.Description;
            SetSelected(false);
        }

        public void ResetItem()
        {
            commandInfo = null;
            clickCallback = null;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = selected ? selectedColor : normalColor;
        }

        private void OnClicked()
        {
            clickCallback?.Invoke(commandInfo);
        }
    }
}
