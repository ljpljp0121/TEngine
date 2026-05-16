using RuntimeInspectorNamespace;
using UnityEngine;

namespace PFDebugger
{
    [DebuggerTab("RuntimeInspectorPanel",2)]
    public class RuntimeInspectorPanel : MonoBehaviour, IDebuggerPanel
    {
        [SerializeField] private RuntimeHierarchy runtimeHierarchy;
        [SerializeField] private RuntimeInspector runtimeInspector;

        public void OnInitPanel()
        {
            runtimeHierarchy.SetFont(DebuggerManager.I.Font);
            runtimeInspector.SetFont(DebuggerManager.I.Font);
        }
        public void OnDeinitPanel() { }
        public void OnPanelShow() { }
        public void OnPanelHide() { }
        public void OnPanelTick() { }
    }
}