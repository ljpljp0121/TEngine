using UnityEditor.Experimental.GraphView;

namespace PFGraph
{
    public abstract partial class BasePortView
    {
        public BasePortView(PortProcessor port, IEdgeConnectorListener connectorListener) : this(
            orientation: (port.Direction == BasePort.Direction.Left || port.Direction == BasePort.Direction.Right)
                ? Orientation.Horizontal
                : Orientation.Vertical,
            direction: (port.Direction == BasePort.Direction.Left || port.Direction == BasePort.Direction.Top)
                ? Direction.Input
                : Direction.Output,
            capacity: port.Capacity == BasePort.Capacity.Single ? Capacity.Single : Capacity.Multi,
            port.PortType, connectorListener) { }

        protected virtual void DoInit() { }

        protected virtual void DoUnInit() { }
    }
}