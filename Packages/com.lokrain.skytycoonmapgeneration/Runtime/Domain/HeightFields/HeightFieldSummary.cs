#nullable enable

using System;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Deterministic summary of a generated height field.
    /// The histogram is normalized to the output range that was requested by the generator.
    /// </summary>
    public struct HeightFieldSummary
    {
        public const int HistogramBinCount = 64;

        public bool IsValid;
        public int SampleCount;
        public float Min;
        public float Max;
        public float Mean;
        public float StandardDeviation;
        public float ApproximateP05;
        public float ApproximateP50;
        public float ApproximateP95;
        public int NonFiniteCount;
        public int ClampedLowCount;
        public int ClampedHighCount;
        public ulong QuantizedHash64;
        public FixedList512Bytes<int> Histogram;

        public static HeightFieldSummary CreateInvalid()
        {
            return new HeightFieldSummary
            {
                IsValid = false,
                SampleCount = 0,
                Min = 0f,
                Max = 0f,
                Mean = 0f,
                StandardDeviation = 0f,
                ApproximateP05 = 0f,
                ApproximateP50 = 0f,
                ApproximateP95 = 0f,
                NonFiniteCount = 0,
                ClampedLowCount = 0,
                ClampedHighCount = 0,
                QuantizedHash64 = 0UL,
                Histogram = CreateEmptyHistogram()
            };
        }

        public static FixedList512Bytes<int> CreateEmptyHistogram()
        {
            FixedList512Bytes<int> histogram = default;
            for (int i = 0; i < HistogramBinCount; i++)
                histogram.Add(0);
            return histogram;
        }

        public override readonly string ToString()
        {
            if (!IsValid)
                return "Invalid height-field summary";

            return "samples=" + SampleCount
                + " min=" + Min.ToString("0.####")
                + " max=" + Max.ToString("0.####")
                + " mean=" + Mean.ToString("0.####")
                + " std=" + StandardDeviation.ToString("0.####")
                + " p05=" + ApproximateP05.ToString("0.####")
                + " p50=" + ApproximateP50.ToString("0.####")
                + " p95=" + ApproximateP95.ToString("0.####")
                + " nonFinite=" + NonFiniteCount
                + " hash=0x" + QuantizedHash64.ToString("X16");
        }
    }
}
