#if UNITY_EDITOR
using PFGraph;
using UnityEngine.UIElements;

[CustomView(typeof(LogNode))]
public class LogNodeView : BaseNodeView
{
    Button btnDebug;

    public LogNodeView() : base()
    {
        btnDebug = new Button();
        btnDebug.text = "Log";
        btnDebug.clicked += OnClick;
        this.controls.Add(btnDebug);
    }

    protected override void DoInit()
    {
        base.DoInit();
        PortViews[ConstValues.FLOW_IN_PORT_NAME].PortLabel.AddToClassList("hidden");
    }

    private void OnClick()
    {
        (ViewModel as LogNodeProcessor).DebugInput();
    }
}
#endif