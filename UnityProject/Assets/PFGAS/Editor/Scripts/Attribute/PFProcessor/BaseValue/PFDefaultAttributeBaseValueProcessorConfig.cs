using System;

namespace PFGAS.Editor
{
    /// <summary>不改变 BaseValue 的默认处理器配置。</summary>
    [Serializable]
    public sealed class PFDefaultAttributeBaseValueProcessorConfig : PFAttributeBaseValueProcessorConfig
    {
        public override string DisplayName => "Default";

        public override bool TryBuildProcessorExpression(
            PFAttributeProcessorCodeContext context,
            out string expression,
            out string error)
        {
            expression = "DefaultAttributeBaseValueProcessor.Instance";
            error = null;
            return true;
        }
    }
}
