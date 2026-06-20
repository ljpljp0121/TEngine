using System;
using System.Globalization;

namespace PFGAS.Runtime
{
    /// <summary>
    /// Runtime tag identifier value. Project-specific tag constants live in generated project assemblies.
    /// </summary>
    public readonly struct PFTagId : IEquatable<PFTagId>
    {
        public readonly int Value;

        public PFTagId(int value)
        {
            Value = value;
        }

        public bool Equals(PFTagId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PFTagId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static explicit operator int(PFTagId tagId)
        {
            return tagId.Value;
        }

        public static bool operator ==(PFTagId left, PFTagId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PFTagId left, PFTagId right)
        {
            return !left.Equals(right);
        }
    }
}
