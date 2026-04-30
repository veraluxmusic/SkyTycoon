#nullable enable
 
using Unity.Mathematics;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using System;
using Random = Unity.Mathematics.Random;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Requests
{
    /// <summary>
    /// Root seed wrapper with deterministic derivation for stages, fields, and algorithms.
    ///
    /// Rule: derive independent streams by stable names. Do not share one mutable Random
    /// across stages because that makes replay dependent on implementation order.
    /// </summary>
    [Serializable]
    public readonly struct DeterministicSeed : IEquatable<DeterministicSeed>
    {
        public readonly uint Value;

        public DeterministicSeed(uint value)
        {
            Value = value == 0u ? 1u : value;
        }

        public static DeterministicSeed Default => new(1u);

        public DeterministicSeed Derive(uint salt)
        {
            uint mixed = Mix(Value ^ salt ^ 0x9E3779B9u);
            return new DeterministicSeed(mixed);
        }

        public DeterministicSeed Derive(string stablePath)
        {
            if (string.IsNullOrWhiteSpace(stablePath))
                throw new ArgumentException("Seed derivation path must not be null, empty, or whitespace.", nameof(stablePath));

            return Derive(StableHash128.StableNameToUInt32(stablePath));
        }

        public Random CreateUnityRandom()
        {
            return new Random(Value == 0u ? 1u : Value);
        }

        public bool Equals(DeterministicSeed other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is DeterministicSeed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(DeterministicSeed left, DeterministicSeed right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DeterministicSeed left, DeterministicSeed right)
        {
            return !left.Equals(right);
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value == 0u ? 1u : value;
        }
    }
}
