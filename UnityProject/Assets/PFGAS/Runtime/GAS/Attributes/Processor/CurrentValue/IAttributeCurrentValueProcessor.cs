using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>属性原始聚合值到最终 CurrentValue 的后处理器。</summary>
    public interface IAttributeCurrentValueProcessor
    {
        IReadOnlyList<PFAttributeId> Dependencies { get; }

        float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue);
    }
}
