using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public interface IAttributeBaseValueProcessor
    {
        IReadOnlyList<PFAttributeId> Dependencies { get; }

        float Process(AttributeGraphContext context, PFAttributeId attributeId, float proposedBaseValue);
    }
}
