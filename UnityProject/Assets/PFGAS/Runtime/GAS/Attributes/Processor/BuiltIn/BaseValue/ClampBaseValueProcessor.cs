using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public sealed class ClampBaseValueProcessor : IAttributeBaseValueProcessor
    {
        private readonly PFAttributeId[] dependencies;
        private readonly float? staticMinValue;
        private readonly float? staticMaxValue;
        private readonly PFAttributeId? minAttributeId;
        private readonly PFAttributeId? maxAttributeId;

        public ClampBaseValueProcessor(float minValue, float maxValue)
            : this(minValue, maxValue, null, null)
        {
        }

        public ClampBaseValueProcessor(float minValue, PFAttributeId maxAttributeId)
            : this(minValue, null, null, maxAttributeId)
        {
        }

        public ClampBaseValueProcessor(PFAttributeId minAttributeId, float maxValue)
            : this(null, maxValue, minAttributeId, null)
        {
        }

        public ClampBaseValueProcessor(PFAttributeId minAttributeId, PFAttributeId maxAttributeId)
            : this(null, null, minAttributeId, maxAttributeId)
        {
        }

        private ClampBaseValueProcessor(
            float? staticMinValue,
            float? staticMaxValue,
            PFAttributeId? minAttributeId,
            PFAttributeId? maxAttributeId)
        {
            this.staticMinValue = staticMinValue;
            this.staticMaxValue = staticMaxValue;
            this.minAttributeId = minAttributeId;
            this.maxAttributeId = maxAttributeId;

            if (staticMinValue.HasValue)
            {
                GASGuard.Finite(staticMinValue.Value, nameof(staticMinValue), "BaseValue clamp min must be finite.");
            }

            if (staticMaxValue.HasValue)
            {
                GASGuard.Finite(staticMaxValue.Value, nameof(staticMaxValue), "BaseValue clamp max must be finite.");
            }

            if (staticMinValue.HasValue &&
                staticMaxValue.HasValue &&
                staticMinValue.Value > staticMaxValue.Value)
            {
                GASGuard.ThrowArgument("BaseValue clamp min cannot be greater than max.");
            }

            if (minAttributeId.HasValue && maxAttributeId.HasValue)
            {
                dependencies = new[] { minAttributeId.Value, maxAttributeId.Value };
            }
            else if (minAttributeId.HasValue)
            {
                dependencies = new[] { minAttributeId.Value };
            }
            else if (maxAttributeId.HasValue)
            {
                dependencies = new[] { maxAttributeId.Value };
            }
            else
            {
                dependencies = Array.Empty<PFAttributeId>();
            }
        }

        public IReadOnlyList<PFAttributeId> Dependencies => dependencies;

        public float Process(AttributeGraphContext context, PFAttributeId attributeId, float proposedBaseValue)
        {
            var minValue = staticMinValue ?? context.GetCurrentValue(minAttributeId.Value);
            var maxValue = staticMaxValue ?? context.GetCurrentValue(maxAttributeId.Value);
            if (minValue > maxValue)
            {
                GASGuard.ThrowInvalidOperation(
                    $"Attribute '{attributeId}' base value clamp min is greater than max.");
            }

            if (proposedBaseValue < minValue)
            {
                return minValue;
            }

            return proposedBaseValue > maxValue ? maxValue : proposedBaseValue;
        }
    }
}
