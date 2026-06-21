using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>不改变 raw current value 的默认 CurrentValue processor。</summary>
    public sealed class DefaultAttributeCurrentValueProcessor : IAttributeCurrentValueProcessor
    {
        public static readonly DefaultAttributeCurrentValueProcessor Instance = new();

        public IReadOnlyList<PFAttributeId> Dependencies => Array.Empty<PFAttributeId>();

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float rawCurrentValue)
        {
            return rawCurrentValue;
        }
    }
}
