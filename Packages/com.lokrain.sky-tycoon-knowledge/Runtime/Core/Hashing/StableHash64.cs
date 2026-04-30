using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Core.Hashing
{
    /// <summary>
    /// Stable 64-bit hashing utility for deterministic procedural generation.
    /// </summary>
    /// <remarks>
    /// This type exists for seed derivation, deterministic stream separation, and compact
    /// algorithm provenance identifiers.
    ///
    /// It is intentionally not a cryptographic hash. Do not use it for security,
    /// authentication, save tamper protection, or untrusted-content validation.
    ///
    /// The implementation is platform-stable because it explicitly mixes primitive values
    /// byte-by-byte in little-endian order and does not rely on framework hash codes.
    ///
    /// The public methods are allocation-free when called with Unity fixed strings and are
    /// suitable for Burst-compatible generation code. Managed string overloads are deliberately
    /// not provided because runtime generation algorithms should not use managed labels.
    /// </remarks>
    public static class StableHash64
    {
        private const ulong FnvaOffsetBasis = 14695981039346656037UL;
        private const ulong FnvaPrime = 1099511628211UL;

        /// <summary>
        /// Combines a current hash state with a UTF-8 fixed string label.
        /// </summary>
        /// <remarks>
        /// Label hashing includes the UTF-8 byte length after the label bytes. FNV-1a already
        /// distinguishes most practical label sequences, but length folding makes the path
        /// representation explicit and protects future refactors that may combine byte spans.
        /// </remarks>
        public static ulong Combine(ulong state, FixedString128Bytes label)
        {
            unchecked
            {
                ulong hash = Begin(state);

                for (int i = 0; i < label.Length; i++)
                    hash = AddByte(hash, label[i]);

                hash = AddUInt64Bytes(hash, (ulong)label.Length);
                return Avalanche(hash);
            }
        }

        /// <summary>
        /// Combines a current hash state with a signed integer value.
        /// </summary>
        public static ulong Combine(ulong state, int value)
        {
            unchecked
            {
                ulong hash = Begin(state);
                hash = AddUInt32Bytes(hash, (uint)value);
                return Avalanche(hash);
            }
        }

        /// <summary>
        /// Combines a current hash state with an unsigned integer value.
        /// </summary>
        public static ulong Combine(ulong state, uint value)
        {
            unchecked
            {
                ulong hash = Begin(state);
                hash = AddUInt32Bytes(hash, value);
                return Avalanche(hash);
            }
        }

        /// <summary>
        /// Combines a current hash state with an unsigned 64-bit value.
        /// </summary>
        public static ulong Combine(ulong state, ulong value)
        {
            unchecked
            {
                ulong hash = Begin(state);
                hash = AddUInt64Bytes(hash, value);
                return Avalanche(hash);
            }
        }

        /// <summary>
        /// Converts a 64-bit hash into a non-zero 32-bit state.
        /// </summary>
        /// <remarks>
        /// Some random generators disallow zero state. This helper provides a stable way to
        /// derive a non-zero unsigned state without exposing call sites to that detail.
        /// </remarks>
        public static uint ToNonZeroUInt(ulong value)
        {
            unchecked
            {
                uint result = (uint)(Avalanche(value) & uint.MaxValue);
                return result == 0u ? 1u : result;
            }
        }

        private static ulong Begin(ulong state)
        {
            unchecked
            {
                return Avalanche(state ^ FnvaOffsetBasis);
            }
        }

        private static ulong AddByte(ulong hash, byte value)
        {
            unchecked
            {
                hash ^= value;
                hash *= FnvaPrime;
                return hash;
            }
        }

        private static ulong AddUInt32Bytes(ulong hash, uint value)
        {
            unchecked
            {
                hash = AddByte(hash, (byte)value);
                hash = AddByte(hash, (byte)(value >> 8));
                hash = AddByte(hash, (byte)(value >> 16));
                hash = AddByte(hash, (byte)(value >> 24));
                return hash;
            }
        }

        private static ulong AddUInt64Bytes(ulong hash, ulong value)
        {
            unchecked
            {
                hash = AddByte(hash, (byte)value);
                hash = AddByte(hash, (byte)(value >> 8));
                hash = AddByte(hash, (byte)(value >> 16));
                hash = AddByte(hash, (byte)(value >> 24));
                hash = AddByte(hash, (byte)(value >> 32));
                hash = AddByte(hash, (byte)(value >> 40));
                hash = AddByte(hash, (byte)(value >> 48));
                hash = AddByte(hash, (byte)(value >> 56));
                return hash;
            }
        }

        /// <summary>
        /// SplitMix64 finalizer used as an avalanche step.
        /// </summary>
        private static ulong Avalanche(ulong value)
        {
            unchecked
            {
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;
                return value;
            }
        }
    }
}