#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Hashing
{
    /// <summary>
    /// Stable, engine-independent 128-bit hash value used for generated artifacts,
    /// field metadata, stage metadata, and deterministic replay diagnostics.
    ///
    /// This type intentionally does not wrap UnityEngine.Hash128 so it can stay in
    /// the pure runtime layer and remain usable from package code that avoids
    /// UnityEngine dependencies.
    /// </summary>
    [Serializable]
    public readonly struct StableHash128 : IEquatable<StableHash128>
    {
        public static readonly StableHash128 Zero = new(0UL, 0UL);

        public readonly ulong Low;
        public readonly ulong High;

        public StableHash128(ulong low, ulong high)
        {
            Low = low;
            High = high;
        }

        public bool IsZero => Low == 0UL && High == 0UL;

        public StableHash128 Append(uint value)
        {
            ulong low = Mix64(Low ^ value);
            ulong high = Mix64(High ^ ((ulong)value << 32) ^ value);
            return new StableHash128(low, high);
        }

        public StableHash128 Append(int value)
        {
            return Append(unchecked((uint)value));
        }

        public StableHash128 Append(ulong value)
        {
            ulong low = Mix64(Low ^ value);
            ulong high = Mix64(High ^ RotateLeft(value, 32));
            return new StableHash128(low, high);
        }

        public StableHash128 Append(StableHash128 value)
        {
            ulong low = Mix64(Low ^ value.Low);
            ulong high = Mix64(High ^ value.High);
            return new StableHash128(low, high);
        }

        public static StableHash128 FromUInt32(uint value)
        {
            ulong low = Mix64(value);
            ulong high = Mix64(((ulong)value << 32) | value);
            return new StableHash128(low, high);
        }

        public static StableHash128 FromUInt64(ulong value)
        {
            return new StableHash128(Mix64(value), Mix64(RotateLeft(value, 32)));
        }

        public static StableHash128 FromStableName(string stableName)
        {
            if (string.IsNullOrWhiteSpace(stableName))
                throw new ArgumentException("Stable hash name must not be null, empty, or whitespace.", nameof(stableName));

            ulong low = 14695981039346656037UL;
            ulong high = 1099511628211UL;

            for (int i = 0; i < stableName.Length; i++)
            {
                char c = stableName[i];
                low ^= c;
                low *= 1099511628211UL;

                high ^= (uint)c + 0x9E3779B9u;
                high *= 14029467366897019727UL;
            }

            return new StableHash128(Mix64(low), Mix64(high));
        }

        public static uint StableNameToUInt32(string stableName)
        {
            StableHash128 hash = FromStableName(stableName);
            return unchecked((uint)(hash.Low ^ (hash.Low >> 32) ^ hash.High ^ (hash.High >> 32)));
        }

        public bool Equals(StableHash128 other)
        {
            return Low == other.Low && High == other.High;
        }

        public override bool Equals(object? obj)
        {
            return obj is StableHash128 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Low.GetHashCode();
                hash = hash * 31 + High.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return High.ToString("X16", CultureInfo.InvariantCulture)
                + Low.ToString("X16", CultureInfo.InvariantCulture);
        }

        public static bool operator ==(StableHash128 left, StableHash128 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StableHash128 left, StableHash128 right)
        {
            return !left.Equals(right);
        }

        private static ulong Mix64(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return value;
        }

        private static ulong RotateLeft(ulong value, int offset)
        {
            return (value << offset) | (value >> (64 - offset));
        }
    }
}
