using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到另一个属性当前值以内的 CurrentValue processor。</summary>
    public sealed class ClampMaxCurrentValueProcessor : IAttributeCurrentValueProcessor
    {
        private readonly PFAttributeId[] dependencies;

        public ClampMaxCurrentValueProcessor(PFAttributeId maxAttributeId)
        {
            MaxAttributeId = maxAttributeId;
            dependencies = new[] { maxAttributeId };
        }

        public PFAttributeId MaxAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue)
        {
            return Math.Min(rawCurrentValue, context.GetCurrentValue(MaxAttributeId));
        }
    }
}
