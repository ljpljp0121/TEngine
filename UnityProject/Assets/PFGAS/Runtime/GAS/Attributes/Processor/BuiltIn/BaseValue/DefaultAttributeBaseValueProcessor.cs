using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public sealed class DefaultAttributeBaseValueProcessor : IAttributeBaseValueProcessor
    {
        public static readonly DefaultAttributeBaseValueProcessor Instance = new();

        public IReadOnlyList<PFAttributeId> Dependencies => Array.Empty<PFAttributeId>();

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float proposedBaseValue)
        {
            return proposedBaseValue;
        }
    }
}
