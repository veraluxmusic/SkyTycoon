// Runtime/Generation/Diagnostics/HeightFieldDiagnosticsJob.cs
#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Diagnostics
{
    /// <summary>
    /// Burst-compatible deterministic diagnostics pass for height-field samples.
    ///
    /// This job intentionally writes only primitive/native data:
    /// - scalar accumulator through NativeReference
    /// - histogram bins through NativeArray
    ///
    /// It must not write HeightFieldSummary directly. HeightFieldSummary is a domain/report type
    /// and should be constructed on the managed side after the job completes.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard)]
    internal struct HeightFieldDiagnosticsJob : IJob
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;
        private const int QuantizationSteps = 65535;

        [ReadOnly]
        public NativeArray<float> Values;

        [WriteOnly]
        public NativeReference<HeightFieldDiagnosticsAccumulator> Accumulator;

        public NativeArray<int> Histogram;

        public HeightFieldValueRange OutputRange;

        public void Execute()
        {
            HeightFieldDiagnosticsAccumulator accumulator = default;
            accumulator.SampleCount = Values.Length;
            accumulator.Min = float.PositiveInfinity;
            accumulator.Max = float.NegativeInfinity;
            accumulator.QuantizedHash64 = FnvOffsetBasis;

            for (int i = 0; i < Histogram.Length; i++)
                Histogram[i] = 0;

            for (int i = 0; i < Values.Length; i++)
            {
                float value = Values[i];

                if (!math.isfinite(value))
                {
                    accumulator.NonFiniteCount++;
                    accumulator.QuantizedHash64 = AddToHash(accumulator.QuantizedHash64, 0xFFFFFFFFu);
                    continue;
                }

                if (value < accumulator.Min)
                    accumulator.Min = value;

                if (value > accumulator.Max)
                    accumulator.Max = value;

                accumulator.FiniteCount++;

                double delta = value - accumulator.Mean;
                accumulator.Mean += delta / accumulator.FiniteCount;
                double delta2 = value - accumulator.Mean;
                accumulator.M2 += delta * delta2;

                float unit = NormalizeToUnitInterval(value, OutputRange);

                if (value <= ExpectedMin(OutputRange))
                    accumulator.ClampedLowCount++;

                if (value >= ExpectedMax(OutputRange))
                    accumulator.ClampedHighCount++;

                int bin = math.clamp(
                    (int)math.floor(unit * HeightFieldSummary.HistogramBinCount),
                    0,
                    HeightFieldSummary.HistogramBinCount - 1);

                Histogram[bin] = Histogram[bin] + 1;

                uint quantized = (uint)math.round(unit * QuantizationSteps);
                accumulator.QuantizedHash64 = AddToHash(accumulator.QuantizedHash64, quantized);
            }

            if (accumulator.FiniteCount == 0)
            {
                accumulator.Min = 0f;
                accumulator.Max = 0f;
                accumulator.Mean = 0.0;
                accumulator.M2 = 0.0;
            }

            Accumulator.Value = accumulator;
        }

        private static float NormalizeToUnitInterval(float value, HeightFieldValueRange range)
        {
            return range == HeightFieldValueRange.SignedMinusOneToOne
                ? math.saturate(value * 0.5f + 0.5f)
                : math.saturate(value);
        }

        private static float ExpectedMin(HeightFieldValueRange range)
        {
            return range == HeightFieldValueRange.SignedMinusOneToOne ? -1f : 0f;
        }

        private static float ExpectedMax(HeightFieldValueRange range)
        {
            return 1f;
        }

        private static ulong AddToHash(ulong hash, uint value)
        {
            hash ^= value & 0xFFu;
            hash *= FnvPrime;

            hash ^= (value >> 8) & 0xFFu;
            hash *= FnvPrime;

            hash ^= (value >> 16) & 0xFFu;
            hash *= FnvPrime;

            hash ^= (value >> 24) & 0xFFu;
            hash *= FnvPrime;

            return hash;
        }
    }
}