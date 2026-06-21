using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>用委托公式计算最终 CurrentValue 的 processor。</summary>
    public sealed class FormulaCurrentValueProcessor : IAttributeCurrentValueProcessor
    {
        private readonly Func<AttributeGraphContext, PFAttributeId, float, float> formula;

        public FormulaCurrentValueProcessor(
            IEnumerable<PFAttributeId> dependencies,
            Func<AttributeGraphContext, PFAttributeId, float, float> formula)
        {
            Dependencies = new List<PFAttributeId>(dependencies);
            this.formula = formula;
        }

        public IReadOnlyList<PFAttributeId> Dependencies { get; }

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue)
        {
            return formula(context, attributeId, rawCurrentValue);
        }
    }
}
