using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到两个属性当前值组成的动态范围内的 CurrentValue processor。</summary>
    public sealed class ClampRangeCurrentValueProcessor : IAttributeCurrentValueProcessor
    {
        private readonly PFAttributeId[] dependencies;

        public ClampRangeCurrentValueProcessor(PFAttributeId minAttributeId, PFAttributeId maxAttributeId)
        {
            MinAttributeId = minAttributeId;
            MaxAttributeId = maxAttributeId;
            dependencies = new[] { minAttributeId, maxAttributeId };
        }

        public PFAttributeId MinAttributeId { get; }

        public PFAttributeId MaxAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue)
        {
            var minValue = context.GetCurrentValue(MinAttributeId);
            var maxValue = context.GetCurrentValue(MaxAttributeId);
            if (minValue > maxValue)
            {
                GASGuard.ThrowInvalidOperation(
                    $"Attribute '{attributeId}' dynamic clamp min is greater than max.");
            }

            if (rawCurrentValue < minValue)
            {
                return minValue;
            }

            return rawCurrentValue > maxValue ? maxValue : rawCurrentValue;
        }
    }
}
