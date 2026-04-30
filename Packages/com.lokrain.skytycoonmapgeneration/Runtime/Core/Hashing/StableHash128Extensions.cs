#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Hashing
{
    /// <summary>
    /// Stable hash composition helpers used by compile-time plans and diagnostics.
    /// Keep this allocation-free for numeric values because it may also be used by runtime hashing paths.
    /// </summary>
    public static class StableHash128Extensions
    {
        public static StableHash128 Append(this StableHash128 hash, bool value)
        {
            return hash.Append(value ? 1 : 0);
        }

        public static StableHash128 Append(this StableHash128 hash, float value)
        {
            return hash.Append(BitConverter.SingleToInt32Bits(value));
        }

        public static StableHash128 AppendStableString(this StableHash128 hash, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return hash.Append(0);

            return hash.Append(StableHash128.FromStableName(value));
        }
    }
}
