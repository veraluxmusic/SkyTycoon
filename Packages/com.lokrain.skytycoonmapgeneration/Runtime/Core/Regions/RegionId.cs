#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Regions
{
    /// <summary>
    /// Stable one-based player/home region identifier.
    /// Value 0 is reserved for None/unassigned/neutral cells.
    /// </summary>
    [Serializable]
    public readonly struct RegionId : IEquatable<RegionId>, IComparable<RegionId>
    {
        public const int NoneValue = 0;
        public const int MinValue = 1;
        public const int MaxValue = 255;

        public static readonly RegionId None = new(NoneValue);

        public readonly int Value;

        public RegionId(int value)
        {
            Value = value;
        }

        public bool IsNone => Value == NoneValue;
        public bool IsValid => Value >= MinValue && Value <= MaxValue;

        public static RegionId FromOneBasedIndex(int value)
        {
            RegionId regionId = new(value);
            regionId.Validate();
            return regionId;
        }

        public int ToZeroBasedIndex()
        {
            Validate();
            return Value - 1;
        }

        public void Validate()
        {
            if (!IsValid)
                throw new ArgumentOutOfRangeException(nameof(Value), Value, "Region id must be a one-based value in the supported range.");
        }

        public bool Equals(RegionId other)
        {
            return Value == other.Value;
        }

        public int CompareTo(RegionId other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is RegionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsNone ? "Region:None" : "Region:" + Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(RegionId left, RegionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegionId left, RegionId right)
        {
            return !left.Equals(right);
        }
    }
}
