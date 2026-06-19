#if UNITY_EDITOR
using PFGraph;
using UnityEditor.Experimental.GraphView;

public class SampleConnectionView : BaseConnectionView
{
    protected override EdgeControl CreateEdgeControl()
    {
        return new BetterEdgeControl(this)
        {
            capRadius = 4f,
            interceptWidth = 6f
        };
    }
}
#endif