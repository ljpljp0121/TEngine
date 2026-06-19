using System;

namespace PFGAS.Editor
{
    /// <summary>不改变原始聚合值的默认 Evaluator 配置。</summary>
    [Serializable]
    public sealed class PFDefaultAttributeEvaluatorConfig : PFAttributeEvaluatorConfig
    {
        public override string DisplayName => "Default";

        public override bool TryBuildEvaluatorExpression(
            PFAttributeEvaluatorCodeContext context,
            out string expression,
            out string error)
        {
            expression = "DefaultAttributeEvaluator.Instance";
            error = null;
            return true;
        }
    }
}