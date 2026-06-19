namespace PFGAS.Runtime
{
    /// <summary>一次属性 BaseValue 或 CurrentValue 变化的快照。</summary>
    public readonly struct AttributeChange
    {
        public AttributeChange(
            PFAttributeId attributeId,
            float oldBaseValue,
            float newBaseValue,
            float oldCurrentValue,
            float newCurrentValue)
        {
            AttributeId = attributeId;
            OldBaseValue = oldBaseValue;
            NewBaseValue = newBaseValue;
            OldCurrentValue = oldCurrentValue;
            NewCurrentValue = newCurrentValue;
        }

        public PFAttributeId AttributeId { get; }

        public float OldBaseValue { get; }

        public float NewBaseValue { get; }

        public float OldCurrentValue { get; }

        public float NewCurrentValue { get; }

        public bool BaseValueChanged => PFGASHelper.HasMeaningfulChange(OldBaseValue, NewBaseValue);

        public bool CurrentValueChanged => PFGASHelper.HasMeaningfulChange(OldCurrentValue, NewCurrentValue);
    }
}
