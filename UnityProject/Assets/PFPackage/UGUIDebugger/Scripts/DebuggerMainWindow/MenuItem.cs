/*
 ****************************************************
 * 文件：MenuItem.cs
 * 作者：PeiFeng
 * 创建时间：2025/10/26 12:49:20 星期日
 * 功能：菜单按钮
 ****************************************************
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class MenuItem : MonoBehaviour
    {
        private Text BtnName;
        private Button button;
        private Image backgroundImage;

        // 颜色配置
        [SerializeField]
        private Color normalColor = Color.white;
        [SerializeField]
        private Color highlightColor = Color.yellow;

        // 状态
        private bool isHighlighted = false;

        private void Awake()
        {
            BtnName = GetComponentInChildren<Text>();
            button = GetComponent<Button>();
            backgroundImage = GetComponent<Image>();
            
            if (backgroundImage != null)
                normalColor = backgroundImage.color;
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
            if (backgroundImage == null) return;
            backgroundImage.color = isHighlighted ? highlightColor : normalColor;
        }
        
        public void Reset()
        {
            isHighlighted = false;
            UpdateVisualState();
        }
    }
}