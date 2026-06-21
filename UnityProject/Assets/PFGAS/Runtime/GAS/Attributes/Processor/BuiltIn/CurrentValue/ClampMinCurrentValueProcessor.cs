using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到另一个属性当前值以上的 CurrentValue processor。</summary>
    public sealed class ClampMinCurrentValueProcessor : IAttributeCurrentValueProcessor
    {
        private readonly PFAttributeId[] dependencies;

        public ClampMinCurrentValueProcessor(PFAttributeId minAttributeId)
        {
            MinAttributeId = minAttributeId;
            dependencies = new[] { minAttributeId };
        }

        public PFAttributeId MinAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue)
        {
            return Math.Max(rawCurrentValue, context.GetCurrentValue(MinAttributeId));
        }
    }
}
