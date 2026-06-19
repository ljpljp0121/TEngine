using System;

namespace PFGAS.Runtime
{
    /// <summary>AttributeGraph 中已添加 ModifierSource 的运行时句柄。</summary>
    public readonly struct ModifierSourceHandle : IEquatable<ModifierSourceHandle>
    {
        internal ModifierSourceHandle(int value)
        {
            Value = value;
        }

        public static ModifierSourceHandle Invalid { get; } = new ModifierSourceHandle(0);

        public int Value { get; }

        public bool IsValid => Value > 0;

        public bool Equals(ModifierSourceHandle other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ModifierSourceHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}
