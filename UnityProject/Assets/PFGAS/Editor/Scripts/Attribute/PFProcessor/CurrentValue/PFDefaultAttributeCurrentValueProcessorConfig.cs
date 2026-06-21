using System;

namespace PFGAS.Editor
{
    /// <summary>不改变 raw current value 的默认 CurrentValue processor 配置。</summary>
    [Serializable]
    public sealed class PFDefaultAttributeCurrentValueProcessorConfig : PFAttributeCurrentValueProcessorConfig
    {
        public override string DisplayName => "Default";

        public override bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error)
        {
            expression = "DefaultAttributeCurrentValueProcessor.Instance";
            error = null;
            return true;
        }
    }
}
