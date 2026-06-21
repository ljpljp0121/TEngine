namespace PFGAS.Runtime
{
    /// <summary>创建正式内置 Attribute Magnitude 计算类型的工厂。</summary>
    public static class AttributeMagnitude
    {
        public static IAttributeMagnitude Fixed(float value)
        {
            return new FixedAttributeMagnitude(value);
        }

        public static IAttributeMagnitude ScalableFloat(
            float baseValue,
            float coefficient = 1f,
            float postAdd = 0f)
        {
            return new ScalableFloatAttributeMagnitude(baseValue, coefficient, postAdd);
        }

        public static IAttributeMagnitude AttributeBased(
            PFAttributeId attributeId,
            float coefficient = 1f,
            float postAdd = 0f)
        {
            return new AttributeBasedMagnitude(attributeId, coefficient, postAdd);
        }

        internal static IAttributeMagnitude Transform(
            IAttributeMagnitude magnitude,
            float coefficient,
            float postAdd)
        {
            if (magnitude == null)
            {
                GASGuard.ThrowArgument("Attribute magnitude cannot be null.", nameof(magnitude));
            }

            if (PFGASHelper.IsNearlyEqual(coefficient, 1f) &&
                PFGASHelper.IsNearlyZero(postAdd))
            {
                return magnitude;
            }

            return new TransformedAttributeMagnitude(magnitude, coefficient, postAdd);
        }

        internal static float ValidateFinite(float value, string label)
        {
            if (!PFGASHelper.IsFinite(value))
            {
                GASGuard.ThrowInvalidOperation($"Attribute magnitude '{label}' evaluated to a non-finite value.");
            }

            return value;
        }
    }
}
