namespace PFGAS.Runtime
{
    /// <summary>单个属性的基础值、当前值、范围和聚合配置。</summary>
    public struct AttributeValue
    {
        public AttributeValue(float baseValue,
            AggregationMode aggregationMode = AggregationMode.Stacking,
            float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            ValidateFinite(baseValue, nameof(baseValue));
            ValidateFinite(minValue, nameof(minValue));
            ValidateFinite(maxValue, nameof(maxValue));

            if (minValue > maxValue)
            {
                GASGuard.ThrowArgument("MinValue cannot be greater than MaxValue.");
            }

            CalculateMode = aggregationMode;
            MinValue = minValue;
            MaxValue = maxValue;
            BaseValue = ClampValue(baseValue, minValue, maxValue);
            CurrentValue = BaseValue;
        }

        public AggregationMode CalculateMode { get; }
        public float BaseValue { get; private set; }
        public float CurrentValue { get; private set; }
        public float MinValue { get; private set; }
        public float MaxValue { get; private set; }

        public void SetBaseValue(float value)
        {
            BaseValue = Clamp(value);
        }

        public void SetCurrentValue(float value)
        {
            CurrentValue = Clamp(value);
        }

        public float Clamp(float value)
        {
            ValidateFinite(value, nameof(value));
            return ClampValue(value, MinValue, MaxValue);
        }

        private static float ClampValue(float value, float minValue, float maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }

        private static void ValidateFinite(float value, string name)
        {
            GASGuard.Finite(value, name, "Attribute value must be finite.");
        }
    }
}
