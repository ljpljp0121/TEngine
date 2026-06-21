using System;

namespace PFGAS.Editor
{
    /// <summary>把 BaseValue 限制到静态最小值和动态最大属性当前值之间。</summary>
    [Serializable]
    public sealed class PFClampBaseValueProcessorConfig : PFAttributeBaseValueProcessorConfig
    {
        public float MinValue = 0f;

        [PFAttributeReference]
        public int MaxAttributeId = -1;

        public override string DisplayName => "Clamp Base";

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

            expression =
                $"new ClampBaseValueProcessor({FormatFloat(MinValue)}, {context.GetAttributeIdExpression(maxAttribute)})";
            return true;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
        }
    }
}
