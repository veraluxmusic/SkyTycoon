#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Hashing
{
    /// <summary>
    /// Stable 32-bit identifier hash. Use this for compact IDs, not for artifact integrity.
    /// </summary>
    [Serializable]
    public readonly struct StableHash32 : IEquatable<StableHash32>
    {
        public readonly uint Value;

        public StableHash32(uint value)
        {
            Value = value;
        }

        public bool IsZero => Value == 0u;

        public static StableHash32 FromStableName(string stableName)
        {
            return new StableHash32(StableHash128.StableNameToUInt32(stableName));
        }

        public bool Equals(StableHash32 other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is StableHash32 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return "0x" + Value.ToString("X8", CultureInfo.InvariantCulture);
        }

        public static bool operator ==(StableHash32 left, StableHash32 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StableHash32 left, StableHash32 right)
        {
            return !left.Equals(right);
        }
    }
}
