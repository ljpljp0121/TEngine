using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>全局属性定义规则，描述属性默认值、聚合规则、静态范围和最终 Evaluator。</summary>
    public sealed class AttributeRule
    {
        private readonly PFAttributeId[] requiredAttributes;

        public AttributeRule(
            PFAttributeId id,
            float defaultValue,
            AggregationMode aggregationMode = AggregationMode.Stacking,
            float minValue = float.MinValue,
            float maxValue = float.MaxValue,
            IAttributeEvaluator evaluator = null)
        {
            Id = id;
            DefaultValue = defaultValue;
            AggregationMode = aggregationMode;
            MinValue = minValue;
            MaxValue = maxValue;
            Evaluator = evaluator ?? DefaultAttributeEvaluator.Instance;
            requiredAttributes = CopyUniqueDependencies(Evaluator.Dependencies);
        }

        public PFAttributeId Id { get; }
        public float DefaultValue { get; }
        public AggregationMode AggregationMode { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public IAttributeEvaluator Evaluator { get; }
        public IReadOnlyList<PFAttributeId> RequiredAttributes => requiredAttributes;

        public AttributeValue CreateValue()
        {
            return CreateValue(DefaultValue);
        }

        public AttributeValue CreateValue(float baseValue)
        {
            return new AttributeValue(baseValue, AggregationMode, MinValue, MaxValue);
        }

        private static PFAttributeId[] CopyUniqueDependencies(IReadOnlyList<PFAttributeId> dependencies)
        {
            if (dependencies.Count == 0)
            {
                return Array.Empty<PFAttributeId>();
            }

            var result = new List<PFAttributeId>(dependencies.Count);
            PFGASHelper.AddRangeUnique(result, dependencies);
            return result.ToArray();
        }
    }
}
