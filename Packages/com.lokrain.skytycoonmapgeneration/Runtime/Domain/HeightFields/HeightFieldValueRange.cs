#nullable enable

using System;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Output range contract for normalized height-field sources.
    /// </summary>
    public enum HeightFieldValueRange : byte
    {
        SignedMinusOneToOne = 0,
        UnsignedZeroToOne = 1
    }

    public static class HeightFieldValueRangeExtensions
    {
        public static float NormalizeToUnitInterval(this HeightFieldValueRange range, float value)
        {
            switch (range)
            {
                case HeightFieldValueRange.SignedMinusOneToOne:
                    return math.saturate(value * 0.5f + 0.5f);
                case HeightFieldValueRange.UnsignedZeroToOne:
                    return math.saturate(value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(range), range, "Unsupported height-field value range.");
            }
        }

        public static float ConvertFromSignedUnit(this HeightFieldValueRange range, float signedValue)
        {
            float clamped = math.clamp(signedValue, -1f, 1f);

            switch (range)
            {
                case HeightFieldValueRange.SignedMinusOneToOne:
                    return clamped;
                case HeightFieldValueRange.UnsignedZeroToOne:
                    return clamped * 0.5f + 0.5f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(range), range, "Unsupported height-field value range.");
            }
        }
    }
}
