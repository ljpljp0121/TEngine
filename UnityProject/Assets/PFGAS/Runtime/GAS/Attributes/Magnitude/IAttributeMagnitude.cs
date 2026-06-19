using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>描述 AttributeModifier 修改量如何计算的表达式节点。</summary>
    public interface IAttributeMagnitude
    {
        /// <summary>该 Magnitude 会读取的属性；ModifierSource 加入 AttributeGraph 后依赖集合必须保持稳定。</summary>
        IReadOnlyList<PFAttributeId> Dependencies { get; }

        float Evaluate(AttributeGraphContext context);
    }
}
