using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>维护 AttributeGraph 的依赖边、引用计数和拓扑顺序。</summary>
    public sealed partial class AttributeGraph
    {
        /// <summary>标记拓扑缓存失效，下一次读取顺序时会重建。</summary>
        private void MarkTopologyDirty()
        {
            topologyDirty = true;
        }

        /// <summary>获取可复用拓扑缓存；缓存失效时自动重建。</summary>
        private IReadOnlyList<AttributeNode> GetCachedTopologicalOrder()
        {
            if (topologyDirty)
            {
                RebuildTopology();
            }

            return cachedTopologicalOrder;
        }

        /// <summary>重建拓扑顺序并刷新每个节点的拓扑索引。</summary>
        private void RebuildTopology()
        {
            cachedTopologicalOrder = BuildTopologicalOrder();
            for (var i = 0; i < cachedTopologicalOrder.Count; i++)
            {
                cachedTopologicalOrder[i].TopologyIndex = i;
            }

            topologyDirty = false;
        }

        /// <summary>替换 Evaluator 依赖边，保留其他来源的依赖引用计数。</summary>
        private void ReplaceEvaluatorDependencies(
            PFAttributeId attributeId,
            AttributeNode node,
            IReadOnlyList<PFAttributeId> dependencies,
            bool skipMissingDependencies)
        {
            foreach (var dependencyId in node.EvaluatorDependencies)
            {
                RemoveDependencyReference(attributeId, dependencyId);
            }

            node.EvaluatorDependencies.Clear();
            for (var i = 0; i < dependencies.Count; i++)
            {
                var dependencyId = dependencies[i];
                if (skipMissingDependencies && !nodes.ContainsKey(dependencyId))
                {
                    continue;
                }

                if (node.EvaluatorDependencies.Add(dependencyId))
                {
                    AddDependencyReference(attributeId, dependencyId);
                }
            }
        }

        /// <summary>增加一条依赖引用；同一依赖可由 Evaluator 和 Modifier 同时贡献。</summary>
        private void AddDependencyReference(PFAttributeId attributeId, PFAttributeId dependencyId)
        {
            var node = nodes[attributeId];
            if (node.DependencyReferenceCounts.TryGetValue(dependencyId, out var count))
            {
                node.DependencyReferenceCounts[dependencyId] = count + 1;
                return;
            }

            node.DependencyReferenceCounts.Add(dependencyId, 1);
            node.Dependencies.Add(dependencyId);
            nodes[dependencyId].Dependents.Add(attributeId);
            MarkTopologyDirty();
        }

        /// <summary>移除一条依赖引用；引用计数归零时才真正断开依赖边。</summary>
        private void RemoveDependencyReference(PFAttributeId attributeId, PFAttributeId dependencyId)
        {
            if (!nodes.TryGetValue(attributeId, out var node))
            {
                return;
            }

            if (!node.DependencyReferenceCounts.TryGetValue(dependencyId, out var count))
            {
                return;
            }

            if (count > 1)
            {
                node.DependencyReferenceCounts[dependencyId] = count - 1;
                return;
            }

            node.DependencyReferenceCounts.Remove(dependencyId);
            node.Dependencies.Remove(dependencyId);
            if (nodes.TryGetValue(dependencyId, out var dependency))
            {
                dependency.Dependents.Remove(attributeId);
            }

            MarkTopologyDirty();
        }

        /// <summary>节点被移除前，从其依赖者集合中解除自身。</summary>
        private void DetachNodeFromDependencies(PFAttributeId attributeId)
        {
            if (!nodes.TryGetValue(attributeId, out var node))
            {
                return;
            }

            foreach (var dependencyId in node.Dependencies)
            {
                if (nodes.TryGetValue(dependencyId, out var dependency))
                {
                    dependency.Dependents.Remove(attributeId);
                }
            }

            MarkTopologyDirty();
        }

        /// <summary>按拓扑索引排序 dirty 节点，确保依赖先于依赖者重算。</summary>
        private static int CompareTopologyIndex(AttributeNode a, AttributeNode b)
        {
            return a.TopologyIndex.CompareTo(b.TopologyIndex);
        }

        private List<AttributeNode> BuildTopologicalOrder()
        {
            var indegree = new Dictionary<PFAttributeId, int>();
            var queue = new Queue<AttributeNode>();
            foreach (var pair in nodes)
            {
                indegree[pair.Key] = pair.Value.Dependencies.Count;
                if (pair.Value.Dependencies.Count == 0)
                {
                    queue.Enqueue(pair.Value);
                }
            }

            var order = new List<AttributeNode>(nodes.Count);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                order.Add(node);
                foreach (var dependentId in node.Dependents)
                {
                    indegree[dependentId]--;
                    if (indegree[dependentId] == 0)
                    {
                        queue.Enqueue(nodes[dependentId]);
                    }
                }
            }

            if (order.Count != nodes.Count)
            {
                GASGuard.ThrowInvalidOperation("AttributeGraph contains a dependency cycle.");
            }

            return order;
        }
    }
}
