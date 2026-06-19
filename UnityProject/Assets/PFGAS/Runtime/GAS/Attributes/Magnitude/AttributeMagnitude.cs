using System;

namespace PFGAS.Runtime
{
    /// <summary>创建常用 Attribute Magnitude 表达式节点的工厂。</summary>
    public static class AttributeMagnitude
    {
        public static IAttributeMagnitude Fixed(float value)
        {
            return new FixedAttributeMagnitude(value);
        }

        public static IAttributeMagnitude Attribute(PFAttributeId attributeId)
        {
            return new AttributeReferenceMagnitude(attributeId);
        }

        public static IAttributeMagnitude Add(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Add, left, right);
        }

        public static IAttributeMagnitude Subtract(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Subtract, left, right);
        }

        public static IAttributeMagnitude Multiply(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Multiply, left, right);
        }

        public static IAttributeMagnitude Divide(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Divide, left, right);
        }

        public static IAttributeMagnitude Min(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Min, left, right);
        }

        public static IAttributeMagnitude Max(IAttributeMagnitude left, IAttributeMagnitude right)
        {
            return new BinaryAttributeMagnitude(BinaryMagnitudeOperation.Max, left, right);
        }

        public static IAttributeMagnitude Clamp(
            IAttributeMagnitude value,
            IAttributeMagnitude min,
            IAttributeMagnitude max)
        {
            return new ClampAttributeMagnitude(value, min, max);
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
