using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PFDebugger
{
    public class DebuggerMainWindow : MonoBehaviour
    {
        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                gameObject.SetActive(isActive);
            }
        }

        internal void OnAwake() { }

        internal void OnUpdate() { }
    }
}