using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>返回固定数值的 Magnitude。</summary>
    internal sealed class FixedAttributeMagnitude : IAttributeMagnitude
    {
        private static readonly PFAttributeId[] EmptyDependencies = Array.Empty<PFAttributeId>();

        private readonly float value;

        public FixedAttributeMagnitude(float value)
        {
            GASGuard.Finite(value, nameof(value), "Attribute magnitude value must be finite.");

            this.value = value;
        }

        public IReadOnlyList<PFAttributeId> Dependencies => EmptyDependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            return value;
        }
    }
}
