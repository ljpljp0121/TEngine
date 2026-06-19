namespace PFGAS.Runtime
{
    /// <summary>GameplayEffect 对单个属性贡献的一条 Modifier 定义。</summary>
    public readonly struct GameplayEffectModifierSpec
    {
        public GameplayEffectModifierSpec(
            GameplayEffectModifierPhase phase,
            PFAttributeId attributeId,
            GEOperation operation,
            GameplayEffectMagnitudeSpec magnitude,
            GameplayEffectCapturePolicy capturePolicy,
            bool scaleByStackCount = false)
        {
            Phase = phase;
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude;
            CapturePolicy = capturePolicy;
            ScaleByStackCount = scaleByStackCount;
        }

        public GameplayEffectModifierPhase Phase { get; }

        public PFAttributeId AttributeId { get; }

        public GEOperation Operation { get; }

        public GameplayEffectMagnitudeSpec Magnitude { get; }

        public GameplayEffectCapturePolicy CapturePolicy { get; }

        public bool ScaleByStackCount { get; }

        internal AttributeModifier ToAttributeModifier(IAttributeMagnitude magnitude)
        {
            return new AttributeModifier(AttributeId, Operation, magnitude);
        }
    }
}
