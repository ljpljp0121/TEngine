using System;
using System.Collections.Generic;
using System.Linq;

namespace PFGAS.Runtime
{
    /// <summary>一组来自同一来源的属性 Modifier。</summary>
    public sealed class ModifierSource
    {
        public ModifierSource(IEnumerable<AttributeModifier> modifiers)
            : this(string.Empty, modifiers)
        {
        }

        public ModifierSource(string name, IEnumerable<AttributeModifier> modifiers)
        {
            Name = name;
            Modifiers = modifiers.ToArray();
        }

        public string Name { get; }

        public IReadOnlyList<AttributeModifier> Modifiers { get; }
    }
}
