using System;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class InfoMenuItem : MonoBehaviour
    {
        private DebuggerText BtnName;
        private Button button;
        private Image backgroundImg;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        private bool isHighlighted = false;

        private void Awake()
        {
            BtnName = GetComponentInChildren<DebuggerText>();
            button = GetComponent<Button>();
            backgroundImg = GetComponent<Image>();
        }
        
        public void AddListener(Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        
        public void SetText(string text)
        {
            this.BtnName.text = text;
        }
        
        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateVisualState();
        }
        
        private void UpdateVisualState()
        {
            if (backgroundImg == null) return;
            backgroundImg.color = isHighlighted ? highlightColor : normalColor;
        }
        
        public void Reset()
        {
            isHighlighted = false;
            UpdateVisualState();
        }
    }
}