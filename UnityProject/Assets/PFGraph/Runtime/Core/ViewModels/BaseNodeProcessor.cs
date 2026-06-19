using System;
using System.Collections.Generic;

namespace PFGraph
{
    [ViewModel(typeof(BaseNode))]
    public class BaseNodeProcessor : ViewModel, IGraphElementProcessor, IGraphElementProcessor_Scope
    {
        private BaseNode data;
        private Type dataType;
        private string title;
        private string tooltip;
        private InternalColor titleColor;

        private readonly List<PortProcessor> inPorts;
        private readonly List<PortProcessor> outPorts;
        private readonly Dictionary<string, PortProcessor> ports;

        private BaseGraphProcessor owner;
        private int index;

        public BaseNodeProcessor(BaseNode model)
        {
            data = model;
            data.position = model.position == default ? InternalVector2Int.zero : model.position;
            dataType = model.GetType();

            inPorts = new List<PortProcessor>();
            outPorts = new List<PortProcessor>();
            ports = new Dictionary<string, PortProcessor>();

            var nodeStaticInfo = GraphProcessorUtil.GetNodeStaticInfo(dataType);
            title = nodeStaticInfo.Title;
            tooltip = nodeStaticInfo.Tooltip;
            titleColor = nodeStaticInfo.CustomTitleColor.Active
                ? nodeStaticInfo.CustomTitleColor.Value
                : this.titleColor;
        }

        public event Action<PortProcessor> onPortAdded;
        public event Action<PortProcessor> onPortRemoved;
        public event Action<int, int> onIndexChanged;

        public BaseNode Model => data;

        public Type ModelType => dataType;

        object IGraphElementProcessor.Model => data;

        /// <summary> 唯一标识 </summary>
        public long ID => Model.id;

        public virtual InternalVector2Int Position
        {
            get => Model.position;
            set => SetFieldValue(ref Model.position, value, nameof(BaseNode.position));
        }

        public virtual string Title
        {
            get => title;
            set => SetFieldValue(ref title, value, ConstValues.NODE_TITLE_NAME);
        }

        public virtual InternalColor TitleColor
        {
            get => titleColor;
            set => SetFieldValue(ref titleColor, value, ConstValues.NODE_TITLE_COLOR_NAME);
        }

        public virtual string Tooltip
        {
            get => tooltip;
            set => SetFieldValue(ref tooltip, value, ConstValues.NODE_TOOLTIP_NAME);
        }

        public IReadOnlyList<PortProcessor> InPorts => inPorts;

        public IReadOnlyList<PortProcessor> OutPorts => outPorts;

        public IReadOnlyDictionary<string, PortProcessor> Ports => ports;

        public BaseGraphProcessor Owner
        {
            get => owner;
            internal set => owner = value;
        }

        public int Index
        {
            get => index;
            set
            {
                if (index == value)
                    return;

                var oldIndex = index;
                index = value;
                onIndexChanged?.Invoke(oldIndex, index);
            }
        }

        internal void Enable()
        {
            foreach (var port in ports.Values)
            {
                if (port.connections.Count > 1)
                    port.Trim();
            }

            OnEnabled();
        }

        internal void Disable()
        {
            OnDisabled();
        }

        #region API

        public PortProcessor GetPort(string portName)
        {
            return ports.GetValueOrDefault(portName);
        }

        public IEnumerable<BaseNodeProcessor> GetPortConnections(string portName)
        {
            var port = GetPort(portName);
            if (port == null)
            {
                yield break;
            }

            foreach (var connection in port.Connections)
            {
                yield return port.Direction == BasePort.Direction.Left ? connection.FromNode : connection.ToNode;
            }
        }

        public PortProcessor AddPort(BasePort port)
        {
            var portVM = ViewModelFactory.ProduceViewModel(port) as PortProcessor;
            AddPort(portVM);
            return portVM;
        }

        public void AddPort(PortProcessor port)
        {
            ports.Add(port.Name, port);
            switch (port.Direction)
            {
                case BasePort.Direction.Left:
                case BasePort.Direction.Top:
                {
                    inPorts.Add(port);
                    break;
                }
                case BasePort.Direction.Right:
                case BasePort.Direction.Bottom:
                {
                    outPorts.Add(port);
                    break;
                }
            }

            port.Owner = this;
            onPortAdded?.Invoke(port);
        }

        public void RemovePort(string portName)
        {
            if (!ports.TryGetValue(portName, out var port))
                return;

            RemovePort(port);
        }

        public void RemovePort(PortProcessor port)
        {
            if (port.Owner != this)
                return;
            if (Owner != null)
                Owner.Disconnect(port);
            ports.Remove(port.Name);
            switch (port.Direction)
            {
                case BasePort.Direction.Left:
                {
                    inPorts.Remove(port);
                    break;
                }
                case BasePort.Direction.Right:
                {
                    outPorts.Remove(port);
                    break;
                }
            }

            onPortRemoved?.Invoke(port);
        }

        public void SortPort(Func<PortProcessor, PortProcessor, int> comparer)
        {
            inPorts.QuickSort(comparer);
            outPorts.QuickSort(comparer);
        }

        #endregion

        protected virtual void OnEnabled() { }

        protected virtual void OnDisabled() { }
    }

    public class BaseNodeVM<T> : BaseNodeProcessor where T : BaseNode
    {
        public T T_Model { get; }

        public BaseNodeVM(BaseNode model) : base(model)
        {
            T_Model = model as T;
        }
    }
}