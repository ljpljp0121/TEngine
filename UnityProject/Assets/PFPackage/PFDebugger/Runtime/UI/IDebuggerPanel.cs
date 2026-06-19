namespace PFDebugger
{
    public interface IDebuggerPanel
    {
        void OnInitPanel();
        void OnDeinitPanel();
        void OnPanelShow();
        void OnPanelHide();
        void OnPanelTick();
    }
}
