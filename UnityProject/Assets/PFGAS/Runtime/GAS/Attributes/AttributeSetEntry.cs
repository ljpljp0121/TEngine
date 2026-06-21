using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>AttributeSet 中单个 Attribute 的初始化值、聚合规则、范围和两阶段后处理器。</summary>
    public sealed class AttributeSetEntry
    {
        private readonly PFAttributeId[] requiredAttributes;

        public AttributeSetEntry(
            PFAttributeId id,
            float defaultValue,
            AggregationMode aggregationMode = AggregationMode.Stacking,
            float minValue = float.MinValue,
            float maxValue = float.MaxValue,
            IAttributeBaseValueProcessor baseValueProcessor = null,
            IAttributeCurrentValueProcessor currentValueProcessor = null)
        {
            Id = id;
            DefaultValue = defaultValue;
            AggregationMode = aggregationMode;
            MinValue = minValue;
            MaxValue = maxValue;
            BaseValueProcessor = baseValueProcessor ?? DefaultAttributeBaseValueProcessor.Instance;
            CurrentValueProcessor = currentValueProcessor ?? DefaultAttributeCurrentValueProcessor.Instance;
            requiredAttributes = CopyUniqueDependencies(
                BaseValueProcessor.Dependencies,
                CurrentValueProcessor.Dependencies);
        }

        public PFAttributeId Id { get; }
        public float DefaultValue { get; }
        public AggregationMode AggregationMode { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public IAttributeBaseValueProcessor BaseValueProcessor { get; }
        public IAttributeCurrentValueProcessor CurrentValueProcessor { get; }
        public IReadOnlyList<PFAttributeId> RequiredAttributes => requiredAttributes;

        public AttributeValue CreateValue()
        {
            return CreateValue(DefaultValue);
        }

        public AttributeValue CreateValue(float baseValue)
        {
            return new AttributeValue(baseValue, AggregationMode, MinValue, MaxValue);
        }

        private static PFAttributeId[] CopyUniqueDependencies(
            IReadOnlyList<PFAttributeId> baseValueDependencies,
            IReadOnlyList<PFAttributeId> currentValueDependencies)
        {
            if (baseValueDependencies.Count == 0 && currentValueDependencies.Count == 0)
            {
                return Array.Empty<PFAttributeId>();
            }

            var result = new List<PFAttributeId>(
                baseValueDependencies.Count + currentValueDependencies.Count);
            PFGASHelper.AddRangeUnique(result, baseValueDependencies);
            PFGASHelper.AddRangeUnique(result, currentValueDependencies);
            return result.ToArray();
        }
    }
}
