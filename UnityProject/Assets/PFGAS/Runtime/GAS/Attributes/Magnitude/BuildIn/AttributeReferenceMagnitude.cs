using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>读取另一个属性 CurrentValue 作为修改量的 Magnitude。</summary>
    internal sealed class AttributeReferenceMagnitude : IAttributeMagnitude
    {
        private readonly PFAttributeId[] dependencies;

        public AttributeReferenceMagnitude(PFAttributeId attributeId)
        {
            AttributeId = attributeId;
            dependencies = new[] { attributeId };
        }

        public PFAttributeId AttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            return context.GetCurrentValue(AttributeId);
        }
    }
}
