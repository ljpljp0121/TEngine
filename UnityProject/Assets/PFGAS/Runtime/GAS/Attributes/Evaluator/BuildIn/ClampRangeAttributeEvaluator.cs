using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到两个属性当前值组成的动态范围内的 Evaluator。</summary>
    public sealed class ClampRangeAttributeEvaluator : IAttributeEvaluator
    {
        private readonly PFAttributeId[] dependencies;

        public ClampRangeAttributeEvaluator(PFAttributeId minAttributeId, PFAttributeId maxAttributeId)
        {
            MinAttributeId = minAttributeId;
            MaxAttributeId = maxAttributeId;
            dependencies = new[] { minAttributeId, maxAttributeId };
        }

        public PFAttributeId MinAttributeId { get; }

        public PFAttributeId MaxAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
        {
            var minValue = context.GetCurrentValue(MinAttributeId);
            var maxValue = context.GetCurrentValue(MaxAttributeId);
            if (minValue > maxValue)
            {
                GASGuard.ThrowInvalidOperation(
                    $"Attribute '{attributeId}' dynamic clamp min is greater than max.");
            }

            if (rawValue < minValue)
            {
                return minValue;
            }

            return rawValue > maxValue ? maxValue : rawValue;
        }
    }
}
