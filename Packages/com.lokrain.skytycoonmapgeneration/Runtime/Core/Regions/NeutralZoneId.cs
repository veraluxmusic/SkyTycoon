#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Regions
{
    /// <summary>
    /// Stable one-based neutral economic zone identifier.
    /// Value 0 means the cell is not part of a neutral zone.
    /// </summary>
    [Serializable]
    public readonly struct NeutralZoneId : IEquatable<NeutralZoneId>, IComparable<NeutralZoneId>
    {
        public const int NoneValue = 0;
        public const int MinValue = 1;
        public const int MaxValue = 16;

        public static readonly NeutralZoneId None = new(NoneValue);

        public readonly int Value;

        public NeutralZoneId(int value)
        {
            Value = value;
        }

        public bool IsNone => Value == NoneValue;
        public bool IsValid => Value >= MinValue && Value <= MaxValue;

        public static NeutralZoneId FromOneBasedIndex(int value)
        {
            NeutralZoneId neutralZoneId = new(value);
            neutralZoneId.Validate();
            return neutralZoneId;
        }

        public int ToZeroBasedIndex()
        {
            Validate();
            return Value - 1;
        }

        public void Validate()
        {
            if (!IsValid)
                throw new ArgumentOutOfRangeException(nameof(Value), Value, "Neutral zone id must be a one-based value in the supported range.");
        }

        public bool Equals(NeutralZoneId other)
        {
            return Value == other.Value;
        }

        public int CompareTo(NeutralZoneId other)
        {
            return Value.CompareTo(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is NeutralZoneId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsNone ? "NeutralZone:None" : "NeutralZone:" + Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(NeutralZoneId left, NeutralZoneId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NeutralZoneId left, NeutralZoneId right)
        {
            return !left.Equals(right);
        }
    }
}
