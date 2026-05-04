using System.Runtime.CompilerServices;

namespace Lokrain.Burstable.Math
{
    /// <summary>
    /// Provides deterministic integer hashing helpers for procedural generation.
    /// </summary>
    /// <remarks>
    /// Deterministic hashing is low-level scalar infrastructure. It is suitable for deriving
    /// repeatable pseudo-random values from explicit seeds, tile coordinates, and integer
    /// generation parameters.
    ///
    /// This type does not own domain policy. It does not know about elevation, climate,
    /// terrain, biomes, map shaping, workspaces, jobs, or previews.
    ///
    /// The methods in this type are allocation-free, integer-only, and suitable for use from
    /// Burst-compatible code. They are not cryptographic hash functions and must not be used
    /// for security-sensitive purposes.
    /// </remarks>
    public static class DeterministicHash
    {
        /// <summary>
        /// Golden-ratio-derived 32-bit mixing constant.
        /// </summary>
        public const uint GoldenRatio32 = 0x9E3779B9u;

        /// <summary>
        /// First avalanche multiplication constant.
        /// </summary>
        public const uint AvalancheMultiplierA = 0x7FEB352Du;

        /// <summary>
        /// Second avalanche multiplication constant.
        /// </summary>
        public const uint AvalancheMultiplierB = 0x846CA68Bu;

        /// <summary>
        /// Applies a deterministic 32-bit avalanche mix.
        /// </summary>
        /// <param name="value">Value to mix.</param>
        /// <returns>A mixed 32-bit hash value.</returns>
        /// <remarks>
        /// This method is intended to decorrelate nearby integer inputs. The exact mixing
        /// behavior is part of the deterministic generation contract once used by generation
        /// algorithms.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= AvalancheMultiplierA;
                value ^= value >> 15;
                value *= AvalancheMultiplierB;
                value ^= value >> 16;

                return value;
            }
        }

        /// <summary>
        /// Combines a hash seed with an unsigned integer value.
        /// </summary>
        /// <param name="seed">Existing hash seed.</param>
        /// <param name="value">Value to combine into the seed.</param>
        /// <returns>A deterministic combined hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Combine(
            uint seed,
            uint value)
        {
            unchecked
            {
                uint hash = seed;
                hash ^= value + GoldenRatio32 + (hash << 6) + (hash >> 2);

                return Mix(hash);
            }
        }

        /// <summary>
        /// Combines a hash seed with a signed integer value.
        /// </summary>
        /// <param name="seed">Existing hash seed.</param>
        /// <param name="value">Value to combine into the seed.</param>
        /// <returns>A deterministic combined hash value.</returns>
        /// <remarks>
        /// Signed values are reinterpreted as their two's-complement unsigned representation.
        /// This keeps negative coordinates and parameters deterministic without branching.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Combine(
            uint seed,
            int value)
        {
            unchecked
            {
                return Combine(seed, (uint)value);
            }
        }

        /// <summary>
        /// Hashes a deterministic seed and two-dimensional tile coordinate.
        /// </summary>
        /// <param name="seed">Explicit deterministic generation seed.</param>
        /// <param name="x">Tile x-coordinate.</param>
        /// <param name="y">Tile y-coordinate.</param>
        /// <returns>A deterministic hash value for the supplied coordinate.</returns>
        /// <remarks>
        /// This method is suitable for coordinate-stable procedural sampling. The same seed and
        /// coordinate always produce the same hash value.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint HashCoordinate(
            uint seed,
            int x,
            int y)
        {
            unchecked
            {
                uint hash = Mix(seed);
                hash = Combine(hash, x);
                hash = Combine(hash, y);

                return hash;
            }
        }

        /// <summary>
        /// Converts a hash value to a deterministic non-negative signed integer.
        /// </summary>
        /// <param name="hash">Hash value to convert.</param>
        /// <returns>
        /// A non-negative signed integer in the inclusive range <c>0</c> to
        /// <see cref="int.MaxValue"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToNonNegativeInt32(uint hash)
        {
            return (int)(hash & 0x7FFFFFFFu);
        }

        /// <summary>
        /// Scales a hash value into a signed integer range.
        /// </summary>
        /// <param name="hash">Hash value to scale.</param>
        /// <param name="minimumInclusive">Minimum returned value.</param>
        /// <param name="range">Positive number of possible returned values.</param>
        /// <returns>
        /// A deterministic value in the range
        /// <paramref name="minimumInclusive"/> to
        /// <c>minimumInclusive + range - 1</c>.
        /// </returns>
        /// <remarks>
        /// This method performs multiply-high scaling instead of modulo scaling. Callers must
        /// pass a positive <paramref name="range"/> and must ensure the resulting range fits in
        /// a signed 32-bit integer.
        ///
        /// Validation is intentionally owned by settings and algorithms so this method can stay
        /// small and Burst-friendly in hot paths.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ScaleToRange(
            uint hash,
            int minimumInclusive,
            int range)
        {
            unchecked
            {
                int offset = (int)(((ulong)hash * (uint)range) >> 32);
                return minimumInclusive + offset;
            }
        }
    }
}