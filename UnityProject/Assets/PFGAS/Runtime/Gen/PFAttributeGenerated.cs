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

    public static class PFAttributeRules
    {
        public static readonly AttributeRule HP =
            new AttributeRule(
                PFAttributeId.HP,
                100f,
                AggregationMode.Stacking,
                0f,
                float.MaxValue,
                new ClampBaseValueProcessor(0f, PFAttributeId.MaxHP),
                new ClampMaxCurrentValueProcessor(PFAttributeId.MaxHP));

        public static readonly AttributeRule MaxHP =
            new AttributeRule(
                PFAttributeId.MaxHP,
                100f,
                AggregationMode.Stacking,
                1f,
                float.MaxValue,
                DefaultAttributeBaseValueProcessor.Instance,
                DefaultAttributeCurrentValueProcessor.Instance);

        private static readonly AttributeRule[] AllRules =
        {
            HP,
            MaxHP,
        };

        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<AttributeRule> All =
            System.Array.AsReadOnly(AllRules);

        public static AttributeRule Get(PFAttributeId attributeId)
        {
            switch (attributeId)
            {
                case PFAttributeId.HP:
                    return HP;
                case PFAttributeId.MaxHP:
                    return MaxHP;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(attributeId), attributeId, null);
            }
        }
    }
}
