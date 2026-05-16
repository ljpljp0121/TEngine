using UnityEngine.UI;

namespace PFDebugger
{
    public class DebuggerText : Text
    {
        protected override void Awake()
        {
            base.Awake();
            font = DebuggerManager.I.Font;
        }
    }
}