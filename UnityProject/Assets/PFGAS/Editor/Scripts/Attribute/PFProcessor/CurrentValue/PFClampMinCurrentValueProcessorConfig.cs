using System;

namespace PFGAS.Editor
{
    /// <summary>把最终值限制到指定最小属性当前值以上的 CurrentValue processor 配置。</summary>
    [Serializable]
    public sealed class PFClampMinCurrentValueProcessorConfig : PFAttributeCurrentValueProcessorConfig
    {
        [PFAttributeReference]
        public int MinAttributeId = -1;

        public override string DisplayName => "Clamp Min";

        public override bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error)
        {
            if (!TryResolveDependency(context, MinAttributeId, out var minAttribute, out error))
            {
                expression = null;
                return false;
            }

            expression = $"new ClampMinCurrentValueProcessor({context.GetAttributeIdExpression(minAttribute)})";
            return true;
        }
    }
}
