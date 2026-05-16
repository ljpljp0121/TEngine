using System;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class DebuggerTabButton : MonoBehaviour
    {
        [SerializeField] private Color buttonColor;
        [SerializeField] private Color panelSelectedColor;
        [SerializeField] private Color panelUnselectedColor;

        private Image backgroundImage;
        private DebuggerText label;
        private Button btn;
        private Action<DebuggerTabButton> onDebuggerTabBtn;
        private TabEntryType entryType;

        private void Awake()
        {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(OnClick);
        }
        
        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnClick);
        }
        
        private void OnClick()
        {
            onDebuggerTabBtn?.Invoke(this);
        }

        internal void Initialize(string tabName, Action<DebuggerTabButton> onClickCallback, TabEntryType type)
        {
            label = GetComponentInChildren<DebuggerText>();
            label.text = tabName;

            onDebuggerTabBtn = onClickCallback;
            entryType = type;

            ApplyColor(false);
        }

        internal void SetSelected(bool selected)
        {
            if (entryType == TabEntryType.Method) return;
            ApplyColor(selected);
        }

        private void ApplyColor(bool selected)
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                if (entryType == TabEntryType.Method)
                    backgroundImage.color = buttonColor;
                else
                    backgroundImage.color = selected ? panelSelectedColor : panelUnselectedColor;
            }
        }
    }
}
