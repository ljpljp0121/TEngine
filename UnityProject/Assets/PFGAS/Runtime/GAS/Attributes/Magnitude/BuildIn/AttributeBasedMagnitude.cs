using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>读取另一个属性 CurrentValue，并按 attributeValue * K + B 计算修改量。</summary>
    internal sealed class AttributeBasedMagnitude : IAttributeMagnitude
    {
        private readonly PFAttributeId[] dependencies;
        private readonly float coefficient;
        private readonly float postAdd;

        public AttributeBasedMagnitude(
            PFAttributeId attributeId,
            float coefficient,
            float postAdd)
        {
            GASGuard.Finite(coefficient, nameof(coefficient), "Attribute magnitude coefficient must be finite.");
            GASGuard.Finite(postAdd, nameof(postAdd), "Attribute magnitude postAdd must be finite.");

            AttributeId = attributeId;
            this.coefficient = coefficient;
            this.postAdd = postAdd;
            dependencies = new[] { attributeId };
        }

        public PFAttributeId AttributeId { get; }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            var result = context.GetCurrentValue(AttributeId) * coefficient + postAdd;
            return AttributeMagnitude.ValidateFinite(result, nameof(AttributeBasedMagnitude));
        }
    }
}
