using System;
using System.Collections.Generic;

namespace PFGraph
{
    public partial class BaseGraphProcessor
    {
        private Dictionary<long, BaseNodeProcessor> nodes;

        public IReadOnlyDictionary<long, BaseNodeProcessor> Nodes => nodes;

        private void BeginInitNodes()
        {
            this.nodes = new Dictionary<long, BaseNodeProcessor>(Model.nodes.Count);
            for (var index = 0; index < Model.nodes.Count; index++)
            {
                var node = Model.nodes[index];
                if (node == null)
                {
                    ReportDiagnostic($"[MissingNode] Null node at index {index} removed.");
                    Model.nodes.RemoveAt(index--);
                    continue;
                }
                // 容错：历史数据或外部合并可能产生重复 id，保留首个并剔除后续重复项
                if (nodes.ContainsKey(node.id))
                {
                    ReportDiagnostic($"[DuplicateNode] Node id={node.id} duplicated, later entry removed.");
                    Model.nodes.RemoveAt(index--);
                    continue;
                }
                var nodeProcessor = (BaseNodeProcessor)ViewModelFactory.ProduceViewModel(node);
                nodeProcessor.Owner = this;
                nodeProcessor.Index = index;
                nodes.Add(node.id, nodeProcessor);
            }
        }

        private void EndInitNodes()
        {
            foreach (var node in nodes.Values)
            {
                node.Enable();
            }
        }

        #region API

        public BaseNodeProcessor AddNode<T>(InternalVector2Int position) where T : BaseNode, new()
        {
            return AddNode(TypeCache<T>.TYPE, position);
        }

        public BaseNodeProcessor AddNode(Type nodeType, InternalVector2Int position)
        {
            var nodeVM = NewNode(nodeType, position);
            AddNode(nodeVM);
            return nodeVM;
        }

        public BaseNodeProcessor AddNode(BaseNode nodeData)
        {
            var nodeVM = ViewModelFactory.ProduceViewModel(nodeData) as BaseNodeProcessor;
            AddNode(nodeVM);
            return nodeVM;
        }

        public void AddNode(BaseNodeProcessor node)
        {
            nodes.Add(node.ID, node);
            model.nodes.Add(node.Model);
            node.Owner = this;
            node.Index = model.nodes.Count - 1;
            node.Enable();
            graphEvents.Publish(new AddNodeEventArgs(node));
        }

        public void RemoveNode(long nodeId)
        {
            RemoveNode(Nodes[nodeId]);
        }

        public void RemoveNode(BaseNodeProcessor node)
        {
            if (node.Owner != this)
                throw new InvalidOperationException("节点不属于此 Graph");

            if (groups.NodeGroupMap.ContainsKey(node.ID))
                groups.RemoveNodeFromGroup(node);

            Disconnect(node);
            var removedIndex = node.Index;
            nodes.Remove(node.ID);
            model.nodes.Remove(node.Model);
            node.Disable();
            // 只更新被删除节点之后的节点 Index，避免全量遍历
            for (int index = removedIndex; index < model.nodes.Count; index++)
            {
                var nodeData = model.nodes[index];
                nodes[nodeData.id].Index = index;
            }
            graphEvents.Publish(new RemoveNodeEventArgs(node));
        }

        public virtual BaseNodeProcessor NewNode(Type nodeType, InternalVector2Int position)
        {
            var node = Activator.CreateInstance(nodeType) as BaseNode;
            node.id = GraphProcessorUtil.GenerateId();
            node.position = position;
            return ViewModelFactory.ProduceViewModel(node) as BaseNodeProcessor;
        }

        public virtual BaseNodeProcessor NewNode<TNode>(InternalVector2Int position) where TNode : BaseNode, new()
        {
            var node = new TNode()
            {
                id = GraphProcessorUtil.GenerateId(),
                position = position
            };
            return ViewModelFactory.ProduceViewModel(node) as BaseNodeProcessor;
        }

        #endregion
    }
}