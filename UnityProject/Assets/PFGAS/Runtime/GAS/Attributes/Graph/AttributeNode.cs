using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>AttributeGraph 内部节点，保存单个属性的值、processor 和依赖边状态。</summary>
    internal sealed class AttributeNode
    {
        public AttributeNode(PFAttributeId id, AttributeValue value)
        {
            Id = id;
            Value = value;
            BaseValueProcessor = DefaultAttributeBaseValueProcessor.Instance;
            CurrentValueProcessor = DefaultAttributeCurrentValueProcessor.Instance;
        }

        public PFAttributeId Id { get; }

        public AttributeValue Value;

        public IAttributeBaseValueProcessor BaseValueProcessor;

        public IAttributeCurrentValueProcessor CurrentValueProcessor;

        public int TopologyIndex;

        public readonly HashSet<PFAttributeId> BaseValueProcessorDependencies = new();

        public readonly HashSet<PFAttributeId> CurrentValueProcessorDependencies = new();

        public readonly HashSet<PFAttributeId> Dependencies = new();

        public readonly HashSet<PFAttributeId> Dependents = new();

        public readonly Dictionary<PFAttributeId, int> DependencyReferenceCounts = new();
    }
}
