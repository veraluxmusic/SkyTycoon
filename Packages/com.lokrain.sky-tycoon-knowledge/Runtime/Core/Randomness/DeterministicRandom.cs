using System;

namespace Lokrain.SkyTycoon.Knowledge.Core.Randomness
{
    /// <summary>
    /// Small deterministic pseudo-random generator based on SplitMix64.
    /// </summary>
    /// <remarks>
    /// This generator is intended for deterministic procedural generation decisions:
    /// sampling candidates, choosing attempts, shuffling small fixed work sets, and producing
    /// stable numeric variation from a derived <see cref="GenerationSeed"/>.
    ///
    /// It is not a cryptographic random generator.
    ///
    /// SplitMix64 is appropriate here because it is:
    ///
    /// - deterministic across platforms;
    /// - simple enough to audit;
    /// - allocation-free;
    /// - suitable for Burst-compatible value-type generation code;
    /// - good at expanding independent hashed seed streams.
    ///
    /// Do not share one instance across unrelated algorithm stages. Derive separate
    /// <see cref="GenerationSeed"/> values and create one random generator per subsystem.
    /// </remarks>
    public struct DeterministicRandom
    {
        private ulong _state;

        /// <summary>
        /// Creates a deterministic random stream. Zero is canonicalized to one.
        /// </summary>
        public DeterministicRandom(ulong seed)
        {
            _state = seed == 0UL ? 1UL : seed;
        }

        /// <summary>
        /// Returns the next 64 deterministic random bits.
        /// </summary>
        public ulong NextULong()
        {
            unchecked
            {
                ulong z = _state += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Returns the next 32 deterministic random bits.
        /// </summary>
        public uint NextUInt()
        {
            return (uint)(NextULong() >> 32);
        }

        /// <summary>
        /// Returns an integer in the range [minInclusive, maxExclusive).
        /// </summary>
        /// <remarks>
        /// Uses rejection sampling to avoid modulo bias.
        /// </remarks>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Max must be greater than min.");

            ulong range = (ulong)(maxExclusive - minInclusive);
            ulong threshold = unchecked((0UL - range) % range);

            ulong value;
            do
            {
                value = NextULong();
            }
            while (value < threshold);

            return minInclusive + (int)(value % range);
        }

        /// <summary>
        /// Returns a float in the range [0, 1).
        /// </summary>
        public float NextFloat01()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        /// <summary>
        /// Returns true with the supplied probability.
        /// </summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f)
                return false;

            if (probability >= 1f)
                return true;

            return NextFloat01() < probability;
        }

        /// <summary>
        /// Deterministically shuffles an array in place using Fisher-Yates.
        /// </summary>
        public void Shuffle<T>(T[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);

                (values[j], values[i]) = (values[i], values[j]);
            }
        }
    }
}