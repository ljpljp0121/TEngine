using UnityEditor.Experimental.GraphView;

namespace PFGraph
{
    public partial class BaseConnectionView
    {
        protected override EdgeControl CreateEdgeControl()
        {
            return new BetterEdgeControl(this);
        }

        protected virtual void BindProperties() { }

        protected virtual void UnbindProperties() { }

        protected virtual void OnInitialized() { }
    }
}