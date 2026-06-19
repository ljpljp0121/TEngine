using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    /// <summary>将一个 Magnitude 的结果限制在 min/max Magnitude 之间。</summary>
    internal sealed class ClampAttributeMagnitude : IAttributeMagnitude
    {
        private readonly IAttributeMagnitude value;
        private readonly IAttributeMagnitude min;
        private readonly IAttributeMagnitude max;
        private readonly PFAttributeId[] dependencies;

        public ClampAttributeMagnitude(
            IAttributeMagnitude value,
            IAttributeMagnitude min,
            IAttributeMagnitude max)
        {
            this.value = value;
            this.min = min;
            this.max = max;

            var mergedDependencies = new List<PFAttributeId>();
            PFGASHelper.AddRangeUnique(mergedDependencies, value.Dependencies);
            PFGASHelper.AddRangeUnique(mergedDependencies, min.Dependencies);
            PFGASHelper.AddRangeUnique(mergedDependencies, max.Dependencies);
            dependencies = mergedDependencies.ToArray();
        }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Evaluate(AttributeGraphContext context)
        {
            var valueResult = value.Evaluate(context);
            var minResult = min.Evaluate(context);
            var maxResult = max.Evaluate(context);
            if (minResult > maxResult)
            {
                GASGuard.ThrowInvalidOperation("Attribute magnitude clamp min cannot be greater than max.");
            }

            var result = Math.Min(Math.Max(valueResult, minResult), maxResult);
            return AttributeMagnitude.ValidateFinite(result, nameof(AttributeMagnitude.Clamp));
        }
    }
}
