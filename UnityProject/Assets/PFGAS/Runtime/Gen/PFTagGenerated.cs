///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    public enum PFTagId
    {
        State = 0,
        State_Buff = 1,
        State_DeBuff = 2,
        State_DeBuff_Du = 5,
        State_DeBuff_Fire = 6,
        State_DeBuff_Ice = 7,
        Life = 3,
        Life_HP = 4,
    }

    /// <summary>
    /// Registers generated PFTag hierarchy and display names.
    /// </summary>
    public static class PFTagGenerated
    {
        static PFTagGenerated()
        {
            TagHelper.Register(new Dictionary<PFTagId, PFTag>
            {
                { PFTagId.State, new PFTag(PFTagId.State, Array.Empty<PFTagId>(), new[] { PFTagId.State_Buff, PFTagId.State_DeBuff }) },
                { PFTagId.State_Buff, new PFTag(PFTagId.State_Buff, new[] { PFTagId.State }, Array.Empty<PFTagId>()) },
                { PFTagId.State_DeBuff, new PFTag(PFTagId.State_DeBuff, new[] { PFTagId.State }, new[] { PFTagId.State_DeBuff_Du, PFTagId.State_DeBuff_Fire, PFTagId.State_DeBuff_Ice }) },
                { PFTagId.State_DeBuff_Du, new PFTag(PFTagId.State_DeBuff_Du, new[] { PFTagId.State_DeBuff, PFTagId.State }, Array.Empty<PFTagId>()) },
                { PFTagId.State_DeBuff_Fire, new PFTag(PFTagId.State_DeBuff_Fire, new[] { PFTagId.State_DeBuff, PFTagId.State }, Array.Empty<PFTagId>()) },
                { PFTagId.State_DeBuff_Ice, new PFTag(PFTagId.State_DeBuff_Ice, new[] { PFTagId.State_DeBuff, PFTagId.State }, Array.Empty<PFTagId>()) },
                { PFTagId.Life, new PFTag(PFTagId.Life, Array.Empty<PFTagId>(), new[] { PFTagId.Life_HP }) },
                { PFTagId.Life_HP, new PFTag(PFTagId.Life_HP, new[] { PFTagId.Life }, Array.Empty<PFTagId>()) },
            });

            TagHelper.RegisterNames(new Dictionary<PFTagId, string>
            {
                { PFTagId.State, "State" },
                { PFTagId.State_Buff, "State.Buff" },
                { PFTagId.State_DeBuff, "State.DeBuff" },
                { PFTagId.State_DeBuff_Du, "State.DeBuff.Du" },
                { PFTagId.State_DeBuff_Fire, "State.DeBuff.Fire" },
                { PFTagId.State_DeBuff_Ice, "State.DeBuff.Ice" },
                { PFTagId.Life, "Life" },
                { PFTagId.Life_HP, "Life.HP" },
            });
        }
    }
}
