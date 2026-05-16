namespace PFDebugger
{
    public class TabClickBtns
    {
        [DebuggerTab("关闭",-100)]
        private static void CloseMainWindow()
        {
            Debugger.VisibleMiniWindow();
        }
    }
}