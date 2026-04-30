// Runtime/Generation/Algorithms/OpenSimplex2SFbm/GenerateOpenSimplex2SFbmHeightFieldJob.cs
#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    /// <summary>
    /// Generates an OpenSimplex2S fBM height field.
    ///
    /// The job is sample-indexed rather than tile-indexed because Unity's parallel-for
    /// safety system requires writable NativeArray access to be scoped to the current
    /// Execute index unless NativeDisableParallelForRestriction is used.
    ///
    /// Keeping one output element per Execute index preserves safety checks, avoids
    /// unnecessary unsafe attributes, and lets Unity batch execution internally through
    /// ScheduleParallel's inner-loop batch count.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard)]
    internal struct GenerateOpenSimplex2SFbmHeightFieldJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<float2> Gradients2D;

        [WriteOnly]
        public NativeArray<float> Destination;

        public long Seed;

        public int Width;
        public int Height;

        public int OctaveCount;

        public float BaseFrequency;
        public float Persistence;
        public float Lacunarity;

        public float2 NoiseSpaceOrigin;
        public float2 SampleSpacing;
        public float2 GlobalNoiseOffset;

        public OpenSimplex2SFbmOrientation Orientation;
        public HeightFieldValueRange OutputRange;

        public bool NormalizeByAmplitude;

        public void Execute(int sampleIndex)
        {
            int y = sampleIndex / Width;
            int x = sampleIndex - y * Width;

            float2 point =
                NoiseSpaceOrigin +
                GlobalNoiseOffset +
                new float2(x * SampleSpacing.x, y * SampleSpacing.y);

            var evaluator = new OpenSimplex2S2DEvaluator(Gradients2D);

            float signedValue = EvaluateFbm(evaluator, point);
            Destination[sampleIndex] = ConvertSignedToOutputRange(signedValue);
        }

        private readonly float EvaluateFbm(OpenSimplex2S2DEvaluator evaluator, float2 samplePoint)
        {
            float value = 0f;
            float frequency = BaseFrequency;
            float amplitude = 1f;
            float amplitudeSum = 0f;

            for (int octave = 0; octave < OctaveCount; octave++)
            {
                long octaveSeed = unchecked(Seed + octave * OpenSimplex2SFbmConstants.OctaveSeedStep);

                value += evaluator.Evaluate(
                    octaveSeed,
                    samplePoint * frequency,
                    Orientation) * amplitude;

                amplitudeSum += amplitude;
                frequency *= Lacunarity;
                amplitude *= Persistence;
            }

            if (NormalizeByAmplitude && amplitudeSum > 0f)
                value /= amplitudeSum;

            return value;
        }

        private readonly float ConvertSignedToOutputRange(float signedValue)
        {
            float clamped = math.clamp(signedValue, -1f, 1f);

            return OutputRange == HeightFieldValueRange.SignedMinusOneToOne
                ? clamped
                : clamped * 0.5f + 0.5f;
        }
    }
}