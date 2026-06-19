using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将目标属性最终值限制到另一个属性当前值以内的 Evaluator。</summary>
    public sealed class ClampMaxAttributeEvaluator : IAttributeEvaluator
    {
        private readonly PFAttributeId[] dependencies;

        public ClampMaxAttributeEvaluator(PFAttributeId maxAttributeId)
        {
            MaxAttributeId = maxAttributeId;
            dependencies = new[] { maxAttributeId };
        }

        public PFAttributeId MaxAttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
        {
            return Math.Min(rawValue, context.GetCurrentValue(MaxAttributeId));
        }
    }
}
