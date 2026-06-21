using System;

namespace PFGAS.Editor
{
    /// <summary>把最终值限制到指定最小和最大属性当前值之间的 CurrentValue processor 配置。</summary>
    [Serializable]
    public sealed class PFClampRangeCurrentValueProcessorConfig : PFAttributeCurrentValueProcessorConfig
    {
        [PFAttributeReference]
        public int MinAttributeId = -1;
        [PFAttributeReference]
        public int MaxAttributeId = -1;

        public override string DisplayName => "Clamp Range";

        public override bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error)
        {
            if (!TryResolveDependency(context, MinAttributeId, out var minAttribute, out error) ||
                !TryResolveDependency(context, MaxAttributeId, out var maxAttribute, out error))
            {
                expression = null;
                return false;
            }

            if (minAttribute.Id == maxAttribute.Id)
            {
                expression = null;
                error = "Clamp Range min and max cannot reference the same attribute.";
                return false;
            }

            expression =
                $"new ClampRangeCurrentValueProcessor({context.GetAttributeIdExpression(minAttribute)}, {context.GetAttributeIdExpression(maxAttribute)})";
            return true;
        }
    }
}
