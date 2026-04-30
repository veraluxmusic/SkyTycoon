#nullable enable

using System;
using System.Globalization;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Regions
{
    /// <summary>
    /// Stable region-role identifier. Role definitions are data; this value is what generated artifacts store.
    /// </summary>
    [Serializable]
    public readonly struct RegionRoleId : IEquatable<RegionRoleId>, IComparable<RegionRoleId>
    {
        public static readonly RegionRoleId None = new(0u);

        public readonly uint Value;

        public RegionRoleId(uint value)
        {
            Value = value;
        }

        public bool IsNone => Value == 0u;

        public static RegionRoleId FromStableName(string stableName)
        {
            return new RegionRoleId(StableHash128.StableNameToUInt32(stableName));
        }

        public void Validate()
        {
            if (IsNone)
                throw new ArgumentOutOfRangeException(nameof(Value), Value, "Region role id must not be None.");
        }

        public bool Equals(RegionRoleId other)
        {
            return Value == other.Value;
        }

        public int CompareTo(RegionRoleId other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is RegionRoleId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return IsNone ? "RegionRole:None" : "RegionRole:" + Value.ToString("X8", CultureInfo.InvariantCulture);
        }

        public static bool operator ==(RegionRoleId left, RegionRoleId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegionRoleId left, RegionRoleId right)
        {
            return !left.Equals(right);
        }
    }
}
