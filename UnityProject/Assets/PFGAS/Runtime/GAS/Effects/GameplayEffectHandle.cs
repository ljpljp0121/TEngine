using System;

namespace PFGAS.Runtime
{
    public readonly struct GameplayEffectHandle : IEquatable<GameplayEffectHandle>
    {
        private readonly GameplayEffectContainer owner;

        internal GameplayEffectHandle(GameplayEffectContainer owner, int value)
        {
            this.owner = owner;
            Value = value;
        }

        public static GameplayEffectHandle Invalid => default;

        public int Value { get; }

        public bool IsValid => owner != null && Value > 0;

        internal GameplayEffectContainer Owner => owner;

        public bool Equals(GameplayEffectHandle other)
        {
            return ReferenceEquals(owner, other.owner) && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayEffectHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((owner != null ? owner.GetHashCode() : 0) * 397) ^ Value;
            }
        }

        public static bool operator ==(GameplayEffectHandle left, GameplayEffectHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayEffectHandle left, GameplayEffectHandle right)
        {
            return !left.Equals(right);
        }
    }
}
