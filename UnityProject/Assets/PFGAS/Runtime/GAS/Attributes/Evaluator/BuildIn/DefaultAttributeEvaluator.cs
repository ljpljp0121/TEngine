using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>不改变原始聚合值的默认 Evaluator。</summary>
    public sealed class DefaultAttributeEvaluator : IAttributeEvaluator
    {
        public static readonly DefaultAttributeEvaluator Instance = new();

        public IReadOnlyList<PFAttributeId> Dependencies => Array.Empty<PFAttributeId>();

        public float Evaluate(AttributeGraphContext context, PFAttributeId attributeId, float rawValue)
        {
            return rawValue;
        }
    }
}
