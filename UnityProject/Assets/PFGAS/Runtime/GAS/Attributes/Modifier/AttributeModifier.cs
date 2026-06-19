namespace PFGAS.Runtime
{
    /// <summary>描述某个 ModifierSource 对单个属性的一条修改。</summary>
    public readonly struct AttributeModifier
    {
        public AttributeModifier(PFAttributeId attributeId, GEOperation operation, IAttributeMagnitude magnitude)
        {
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude;
        }

        public PFAttributeId AttributeId { get; }

        public GEOperation Operation { get; }

        public IAttributeMagnitude Magnitude { get; }

        public float EvaluateMagnitude(AttributeGraphContext context)
        {
            return AttributeMagnitude.ValidateFinite(Magnitude.Evaluate(context), nameof(Magnitude));
        }
    }
}
