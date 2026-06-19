using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>用委托公式计算最终 CurrentValue 的 Evaluator。</summary>
    public sealed class FormulaAttributeEvaluator : IAttributeEvaluator
    {
        private readonly Func<AttributeGraphContext, PFAttributeId, float, float> formula;

        public FormulaAttributeEvaluator(
            IEnumerable<PFAttributeId> dependencies,
            Func<AttributeGraphContext, PFAttributeId, float, float> formula)
        {
            Dependencies = new List<PFAttributeId>(dependencies);
            this.formula = formula;
        }

        public IReadOnlyList<PFAttributeId> Dependencies { get; }

        public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
        {
            return formula(context, attributeId, rawValue);
        }
    }
}
