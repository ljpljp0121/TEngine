///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

namespace PFGAS.Runtime
{
    public enum PFAttributeId
    {
        HP = 0,
        MaxHP = 1,
    }

    public enum PFAttributeSetId
    {
        Vital = 0,
    }

    public static class PFAttributeSets
    {
        public static readonly AttributeSet Vital =
            new AttributeSet(
                (int)PFAttributeSetId.Vital,
                nameof(PFAttributeSetId.Vital),
                new[]
                {
                    new AttributeSetEntry(
                        PFAttributeId.HP,
                        100f,
                        AggregationMode.Stacking,
                        0f,
                        float.MaxValue,
                        new ClampBaseValueProcessor(0f, PFAttributeId.MaxHP),
                        new ClampMaxCurrentValueProcessor(PFAttributeId.MaxHP)),
                    new AttributeSetEntry(
                        PFAttributeId.MaxHP,
                        100f,
                        AggregationMode.Stacking,
                        1f,
                        float.MaxValue,
                        DefaultAttributeBaseValueProcessor.Instance,
                        DefaultAttributeCurrentValueProcessor.Instance)
                });

        private static readonly AttributeSet[] AllSets =
        {
            Vital,
        };

        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<AttributeSet> All =
            System.Array.AsReadOnly(AllSets);

        public static AttributeSet Get(PFAttributeSetId attributeSetId)
        {
            switch (attributeSetId)
            {
                case PFAttributeSetId.Vital:
                    return Vital;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(attributeSetId), attributeSetId, null);
            }
        }
    }
}
