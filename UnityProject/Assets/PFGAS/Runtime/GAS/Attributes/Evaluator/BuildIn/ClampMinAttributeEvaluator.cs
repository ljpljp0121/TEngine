using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到另一个属性当前值以上的 Evaluator。</summary>
    public sealed class ClampMinAttributeEvaluator : IAttributeEvaluator
    {
        private readonly PFAttributeId[] dependencies;

        public ClampMinAttributeEvaluator(PFAttributeId minAttributeId)
        {
            MinAttributeId = minAttributeId;
            dependencies = new[] { minAttributeId };
        }

        public PFAttributeId MinAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
        {
            return Math.Max(rawValue, context.GetCurrentValue(MinAttributeId));
        }
    }
}
