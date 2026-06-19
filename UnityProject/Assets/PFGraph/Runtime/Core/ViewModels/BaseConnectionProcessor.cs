using System;
using System.Collections.Generic;

namespace PFGraph
{
    [ViewModel(typeof(BaseConnection))]
    public class BaseConnectionProcessor : ViewModel, IGraphElementProcessor
    {
        /// <summary>
        /// 数据
        /// </summary>
        private readonly BaseConnection model;

        /// <summary>
        /// 数据类型
        /// </summary>
        private readonly Type modelType;

        /// <summary>
        /// 所在Graph
        /// </summary>
        private BaseGraphProcessor owner;

        /// <summary>
        /// 起点Port
        /// </summary>
        private PortProcessor from;

        /// <summary>
        /// 终点Port
        /// </summary>
        private PortProcessor to;

        public BaseConnectionProcessor(BaseConnection model)
        {
            this.model = model;
            this.modelType = model.GetType();
        }

        public BaseConnection Model => model;

        public Type ModelType => modelType;

        object IGraphElementProcessor.Model => model;

        Type IGraphElementProcessor.ModelType => modelType;

        public long FromNodeID => Model.fromNode;

        public long ToNodeID => Model.toNode;

        public string FromPortName => Model.fromPort;

        public string ToPortName => Model.toPort;

        public BaseNodeProcessor FromNode => from.Owner;

        public PortProcessor FromPort => from;

        public BaseNodeProcessor ToNode => to.Owner;

        public PortProcessor ToPort => to;

        public BaseGraphProcessor Owner
        {
            get => owner;
            internal set => owner = value;
        }

        internal void Enable()
        {
            if (Owner == null)
                throw new InvalidOperationException("Connection owner cannot be null when enabling");

            if (!Owner.Nodes.TryGetValue(Model.fromNode, out var fromNode))
                throw new KeyNotFoundException($"From node with ID {Model.fromNode} not found");

            if (!fromNode.Ports.TryGetValue(Model.fromPort, out var fromPort))
                throw new KeyNotFoundException($"From port '{Model.fromPort}' not found in node {Model.fromNode}");

            if (!Owner.Nodes.TryGetValue(Model.toNode, out var toNode))
                throw new KeyNotFoundException($"To node with ID {Model.toNode} not found");

            if (!toNode.Ports.TryGetValue(Model.toPort, out var toPort))
                throw new KeyNotFoundException($"To port '{Model.toPort}' not found in node {Model.toNode}");

            this.from = fromPort;
            this.to = toPort;
            OnEnabled();
        }

        protected virtual void OnEnabled() { }
    }

    public enum ConnectionSortMode
    {
        InPort,
        OutPort,
    }

    public static class ConnectionProcessorComparer
    {
        public static Predicate<BaseConnectionProcessor> EmptyComparer = EmptyComparerFunc;

        private static bool EmptyComparerFunc(BaseConnectionProcessor obj)
        {
            return obj == null;
        }
    }

    public class ConnectionProcessorHorizontalComparer : IComparer<BaseConnectionProcessor>
    {
        public static readonly ConnectionProcessorHorizontalComparer FromPortSortDefault =
            new(ConnectionSortMode.OutPort);
        public static readonly ConnectionProcessorHorizontalComparer ToPortSortDefault = new(ConnectionSortMode.InPort);

        private ConnectionSortMode mode;

        public ConnectionProcessorHorizontalComparer(ConnectionSortMode mode)
        {
            this.mode = mode;
        }

        public int Compare(BaseConnectionProcessor x, BaseConnectionProcessor y)
        {
            // 若需要重新排序的是input接口，则根据FromNode排序
            // 若需要重新排序的是output接口，则根据ToNode排序
            var nodeX = mode == ConnectionSortMode.InPort ? x.FromNode : x.ToNode;
            var nodeY = mode == ConnectionSortMode.InPort ? y.FromNode : y.ToNode;

            // 则使用y坐标比较排序
            // 遵循从上到下
            if (nodeX.Position.y < nodeY.Position.y)
                return -1;
            if (nodeX.Position.y > nodeY.Position.y)
                return 1;

            // 若节点的y坐标相同，则使用x坐标比较排序
            // 遵循从左到右
            if (nodeX.Position.x < nodeY.Position.x)
                return -1;
            if (nodeX.Position.x > nodeY.Position.x)
                return 1;

            return 0;
        }
    }

    public class ConnectionProcessorVerticalComparer : IComparer<BaseConnectionProcessor>
    {
        public static readonly ConnectionProcessorVerticalComparer InPortSortDefault =
            new ConnectionProcessorVerticalComparer(ConnectionSortMode.InPort);
        public static readonly ConnectionProcessorVerticalComparer OutPortSortDefault =
            new ConnectionProcessorVerticalComparer(ConnectionSortMode.OutPort);

        private ConnectionSortMode m_mode;

        public ConnectionProcessorVerticalComparer(ConnectionSortMode mode)
        {
            this.m_mode = mode;
        }

        public int Compare(BaseConnectionProcessor x, BaseConnectionProcessor y)
        {
            // 若需要重新排序的是input接口，则根据FromNode排序
            // 若需要重新排序的是output接口，则根据ToNode排序
            var nodeX = m_mode == ConnectionSortMode.InPort ? x.FromNode : x.ToNode;
            var nodeY = m_mode == ConnectionSortMode.InPort ? y.FromNode : y.ToNode;

            // 则使用x坐标比较排序
            // 遵循从左到右
            if (nodeX.Position.x < nodeY.Position.x)
                return -1;
            if (nodeX.Position.x > nodeY.Position.x)
                return 1;

            // 若节点的x坐标相同，则使用y坐标比较排序
            // 遵循从上到下
            if (nodeX.Position.y < nodeY.Position.y)
                return -1;
            if (nodeX.Position.y > nodeY.Position.y)
                return 1;

            return 0;
        }
    }
}