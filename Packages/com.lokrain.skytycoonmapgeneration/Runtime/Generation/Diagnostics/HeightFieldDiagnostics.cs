// Runtime/Generation/Diagnostics/HeightFieldDiagnostics.cs
#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Diagnostics
{
    /// <summary>
    /// Entry point for deterministic height-field diagnostics.
    ///
    /// The scheduled job collects primitive diagnostics only. The public domain summary is
    /// constructed after completion to keep the Burst boundary small and stable.
    /// </summary>
    public static class HeightFieldDiagnostics
    {
        public static HeightFieldSummary BuildSummary(
            NativeArray<float> values,
            HeightFieldValueRange outputRange,
            Allocator allocator = Allocator.TempJob)
        {
            using NativeReference<HeightFieldDiagnosticsAccumulator> accumulator =
                new(allocator);

            using NativeArray<int> histogram =
                new(HeightFieldSummary.HistogramBinCount, allocator, NativeArrayOptions.ClearMemory);

            JobHandle handle = ScheduleSummary(values, outputRange, accumulator, histogram);
            handle.Complete();

            return CreateSummary(accumulator.Value, histogram, outputRange);
        }

        internal static JobHandle ScheduleSummary(
            NativeArray<float> values,
            HeightFieldValueRange outputRange,
            NativeReference<HeightFieldDiagnosticsAccumulator> accumulator,
            NativeArray<int> histogram,
            JobHandle dependency = default)
        {
            var job = new HeightFieldDiagnosticsJob
            {
                Values = values,
                OutputRange = outputRange,
                Accumulator = accumulator,
                Histogram = histogram
            };

            return job.Schedule(dependency);
        }

        private static HeightFieldSummary CreateSummary(
            in HeightFieldDiagnosticsAccumulator accumulator,
            NativeArray<int> histogram,
            HeightFieldValueRange outputRange)
        {
            HeightFieldSummary summary = HeightFieldSummary.CreateInvalid();

            summary.IsValid = true;
            summary.SampleCount = accumulator.SampleCount;
            summary.NonFiniteCount = accumulator.NonFiniteCount;
            summary.ClampedLowCount = accumulator.ClampedLowCount;
            summary.ClampedHighCount = accumulator.ClampedHighCount;
            summary.QuantizedHash64 = accumulator.QuantizedHash64;
            summary.Histogram = HeightFieldSummary.CreateEmptyHistogram();

            for (int i = 0; i < HeightFieldSummary.HistogramBinCount; i++)
                summary.Histogram[i] = histogram[i];

            if (accumulator.FiniteCount == 0)
            {
                summary.Min = 0f;
                summary.Max = 0f;
                summary.Mean = 0f;
                summary.StandardDeviation = 0f;
                summary.ApproximateP05 = 0f;
                summary.ApproximateP50 = 0f;
                summary.ApproximateP95 = 0f;
                return summary;
            }

            summary.Min = accumulator.Min;
            summary.Max = accumulator.Max;
            summary.Mean = (float)accumulator.Mean;
            summary.StandardDeviation = accumulator.FiniteCount > 1
                ? (float)math.sqrt(accumulator.M2 / (accumulator.FiniteCount - 1))
                : 0f;

            summary.ApproximateP05 = HistogramPercentile(summary.Histogram, accumulator.FiniteCount, 0.05f, outputRange);
            summary.ApproximateP50 = HistogramPercentile(summary.Histogram, accumulator.FiniteCount, 0.50f, outputRange);
            summary.ApproximateP95 = HistogramPercentile(summary.Histogram, accumulator.FiniteCount, 0.95f, outputRange);

            return summary;
        }

        private static float HistogramPercentile(
            FixedList512Bytes<int> histogram,
            int finiteCount,
            float percentile,
            HeightFieldValueRange range)
        {
            if (finiteCount <= 0)
                return 0f;

            int target = math.clamp((int)math.ceil(finiteCount * percentile), 1, finiteCount);
            int cumulative = 0;

            for (int i = 0; i < HeightFieldSummary.HistogramBinCount; i++)
            {
                cumulative += histogram[i];

                if (cumulative >= target)
                {
                    float unit = (i + 0.5f) / HeightFieldSummary.HistogramBinCount;
                    return UnitToRange(unit, range);
                }
            }

            return UnitToRange(1f, range);
        }

        private static float UnitToRange(float unit, HeightFieldValueRange range)
        {
            return range == HeightFieldValueRange.SignedMinusOneToOne
                ? unit * 2f - 1f
                : unit;
        }
    }
}