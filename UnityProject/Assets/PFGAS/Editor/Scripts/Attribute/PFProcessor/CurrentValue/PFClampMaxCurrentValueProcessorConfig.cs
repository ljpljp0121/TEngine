using System;

namespace PFGAS.Editor
{
    /// <summary>把最终值限制到指定最大属性当前值以内的 CurrentValue processor 配置。</summary>
    [Serializable]
    public sealed class PFClampMaxCurrentValueProcessorConfig : PFAttributeCurrentValueProcessorConfig
    {
        [PFAttributeReference]
        public int MaxAttributeId = -1;

        public override string DisplayName => "Clamp Max";

        public override bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error)
        {
            if (!TryResolveDependency(context, MaxAttributeId, out var maxAttribute, out error))
            {
                expression = null;
                return false;
            }

            expression = $"new ClampMaxCurrentValueProcessor({context.GetAttributeIdExpression(maxAttribute)})";
            return true;
        }
    }
}
