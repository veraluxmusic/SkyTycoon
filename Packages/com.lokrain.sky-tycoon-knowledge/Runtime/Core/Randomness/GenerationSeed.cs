using System;
using Lokrain.SkyTycoon.Knowledge.Core.Hashing;
using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Core.Randomness
{
    /// <summary>
    /// Stable root or child seed used by deterministic procedural generation systems.
    /// </summary>
    /// <remarks>
    /// The type is intentionally tiny and immutable. It is safe to pass by value into Burst
    /// jobs and deterministic algorithm settings.
    ///
    /// The key design rule is:
    ///
    /// <code>
    /// Do not advance one shared random stream across unrelated generation stages.
    /// Derive named child seeds instead.
    /// </code>
    ///
    /// This prevents a future algorithm change in one subsystem from changing every later
    /// subsystem by accidentally consuming one extra random value.
    ///
    /// Example:
    ///
    /// <code>
    /// root
    ///     .Derive("MacroLandform")
    ///     .Derive("DomainWarp");
    /// </code>
    ///
    /// In production code, use <see cref="FixedString128Bytes"/> labels or stable enum-backed
    /// labels. Do not use managed strings inside runtime generation hot paths.
    /// </remarks>
    [Serializable]
    public readonly struct GenerationSeed : IEquatable<GenerationSeed>
    {
        /// <summary>
        /// Non-zero seed value.
        /// </summary>
        public readonly ulong Value;

        /// <summary>
        /// Creates a deterministic seed. Zero is canonicalized to one because several random
        /// generators reserve zero as an invalid state.
        /// </summary>
        public GenerationSeed(ulong value)
        {
            Value = value == 0UL ? 1UL : value;
        }

        /// <summary>
        /// Creates a seed from a 32-bit authoring or user-facing seed.
        /// </summary>
        public static GenerationSeed FromUInt(uint value)
        {
            return new GenerationSeed(value == 0u ? 1UL : value);
        }

        /// <summary>
        /// Derives a named child stream from this seed.
        /// </summary>
        /// <remarks>
        /// The result depends only on this seed and the supplied label. It does not depend on
        /// any previous or future random calls.
        /// </remarks>
        public GenerationSeed Derive(FixedString128Bytes label)
        {
            return new GenerationSeed(StableHash64.Combine(Value, label));
        }

        /// <summary>
        /// Derives an indexed child stream from this seed.
        /// </summary>
        /// <remarks>
        /// Use this for deterministic candidate attempts, chunk ids, octave ids, pass ids,
        /// or repeated algorithm instances.
        /// </remarks>
        public GenerationSeed Derive(FixedString128Bytes label, int index)
        {
            ulong hash = StableHash64.Combine(Value, label);
            hash = StableHash64.Combine(hash, index);
            return new GenerationSeed(hash);
        }

        /// <summary>
        /// Creates a deterministic pseudo-random generator initialized from this seed.
        /// </summary>
        public DeterministicRandom CreateRandom()
        {
            return new DeterministicRandom(Value);
        }

        public bool Equals(GenerationSeed other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is GenerationSeed other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Value * 397) ^ (int)(Value >> 32);
            }
        }

        public static bool operator ==(GenerationSeed left, GenerationSeed right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationSeed left, GenerationSeed right)
        {
            return !left.Equals(right);
        }
    }
}