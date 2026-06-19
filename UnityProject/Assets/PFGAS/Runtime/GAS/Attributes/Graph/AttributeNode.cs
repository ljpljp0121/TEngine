using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>AttributeGraph 内部节点，保存单个属性的值、Evaluator 和依赖边状态。</summary>
    internal sealed class AttributeNode
    {
        public AttributeNode(PFAttributeId id, AttributeValue value)
        {
            Id = id;
            Value = value;
            Evaluator = DefaultAttributeEvaluator.Instance;
        }

        public PFAttributeId Id { get; }

        public AttributeValue Value;

        public IAttributeEvaluator Evaluator;

        public int TopologyIndex;

        public readonly HashSet<PFAttributeId> EvaluatorDependencies = new();

        public readonly HashSet<PFAttributeId> Dependencies = new();

        public readonly HashSet<PFAttributeId> Dependents = new();

        public readonly Dictionary<PFAttributeId, int> DependencyReferenceCounts = new();
    }
}
