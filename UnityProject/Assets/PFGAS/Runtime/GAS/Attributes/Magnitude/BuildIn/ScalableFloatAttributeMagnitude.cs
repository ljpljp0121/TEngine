using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>使用固定基础值，并按 baseValue * K + B 计算修改量。</summary>
    internal sealed class ScalableFloatAttributeMagnitude : IAttributeMagnitude
    {
        private static readonly PFAttributeId[] EmptyDependencies = Array.Empty<PFAttributeId>();

        private readonly float baseValue;
        private readonly float coefficient;
        private readonly float postAdd;

        public ScalableFloatAttributeMagnitude(
            float baseValue,
            float coefficient,
            float postAdd)
        {
            GASGuard.Finite(baseValue, nameof(baseValue), "Attribute magnitude baseValue must be finite.");
            GASGuard.Finite(coefficient, nameof(coefficient), "Attribute magnitude coefficient must be finite.");
            GASGuard.Finite(postAdd, nameof(postAdd), "Attribute magnitude postAdd must be finite.");

            this.baseValue = baseValue;
            this.coefficient = coefficient;
            this.postAdd = postAdd;
        }

        public IReadOnlyList<PFAttributeId> Dependencies => EmptyDependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            var result = baseValue * coefficient + postAdd;
            return AttributeMagnitude.ValidateFinite(result, nameof(ScalableFloatAttributeMagnitude));
        }
    }

    /// <summary>对已有 Magnitude 做线性变换，保留原 Magnitude 的依赖声明。</summary>
    internal sealed class TransformedAttributeMagnitude : IAttributeMagnitude
    {
        private readonly IAttributeMagnitude magnitude;
        private readonly float coefficient;
        private readonly float postAdd;

        public TransformedAttributeMagnitude(
            IAttributeMagnitude magnitude,
            float coefficient,
            float postAdd)
        {
            this.magnitude = magnitude;
            GASGuard.Finite(coefficient, nameof(coefficient), "Attribute magnitude coefficient must be finite.");
            GASGuard.Finite(postAdd, nameof(postAdd), "Attribute magnitude postAdd must be finite.");

            this.coefficient = coefficient;
            this.postAdd = postAdd;
        }

        public IReadOnlyList<PFAttributeId> Dependencies => magnitude.Dependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            var result = magnitude.Evaluate(context) * coefficient + postAdd;
            return AttributeMagnitude.ValidateFinite(result, nameof(TransformedAttributeMagnitude));
        }
    }
}
