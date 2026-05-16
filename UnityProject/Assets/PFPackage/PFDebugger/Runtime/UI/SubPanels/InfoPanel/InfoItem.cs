using System;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class InfoItem : MonoBehaviour
    {
        public DebuggerText TitleText;
        public DebuggerText DescriptionText;
        private Image img;

        private void Awake()
        {
            img = GetComponent<Image>();
        }

        public void UpdateValue(string value)
        {
            if (DescriptionText.text != value)
                DescriptionText.text = value;
        }

        public void SetText(string displayName, string value)
        {
            TitleText.text = displayName;
            DescriptionText.text = value;
        }

        public void SetColor(Color color)
        {
            img.color = color;
        }
    }
}