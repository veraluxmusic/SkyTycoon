#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    [Serializable]
    public readonly struct FieldRange : IEquatable<FieldRange>
    {
        public static readonly FieldRange Unbounded = new(0f, 0f, false);
        public static readonly FieldRange Normalized01 = new(0f, 1f, true);
        public static readonly FieldRange SignedNormalized = new(-1f, 1f, true);

        public readonly float Min;
        public readonly float Max;
        public readonly bool IsBounded;

        public FieldRange(float min, float max, bool isBounded)
        {
            if (isBounded && min > max)
                throw new ArgumentOutOfRangeException(nameof(min), "Bounded field range minimum must not exceed maximum.");

            Min = min;
            Max = max;
            IsBounded = isBounded;
        }

        public bool Equals(FieldRange other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max) && IsBounded == other.IsBounded;
        }

        public override bool Equals(object? obj)
        {
            return obj is FieldRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Min.GetHashCode();
                hash = hash * 31 + Max.GetHashCode();
                hash = hash * 31 + IsBounded.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(FieldRange left, FieldRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FieldRange left, FieldRange right)
        {
            return !left.Equals(right);
        }
    }
}
