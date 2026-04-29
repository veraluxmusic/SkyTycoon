using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, OptimizeFor = OptimizeFor.Performance)]
    internal struct GenerateHeightFieldJob : IJobFor
    {
        private const long OctaveSeedStep = unchecked((long)0x9E3779B97F4A7C15UL);

        [ReadOnly] public NativeArray<float2> Gradients2D;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float> Destination;

        public long Seed;
        public int Width;
        public int Height;
        public int TileSize;
        public int TilesPerRow;
        public int OctaveCount;
        public float BaseFrequency;
        public float Persistence;
        public float Lacunarity;
        public float2 NoiseSpaceOrigin;
        public float2 SampleSpacing;
        public float2 GlobalNoiseOffset;
        public Orientation Orientation;
        public HeightFieldValueRange OutputRange;
        public bool NormalizeByAmplitude;

        public void Execute(int tileIndex)
        {
            int tileX = tileIndex % TilesPerRow;
            int tileY = tileIndex / TilesPerRow;
            int minX = tileX * TileSize;
            int minY = tileY * TileSize;
            int maxX = math.min(minX + TileSize, Width);
            int maxY = math.min(minY + TileSize, Height);

            var evaluator = new Evaluator2D(Gradients2D);

            for (int y = minY; y < maxY; y++)
            {
                int rowStart = y * Width;
                float sampleY = NoiseSpaceOrigin.y + GlobalNoiseOffset.y + y * SampleSpacing.y;

                for (int x = minX; x < maxX; x++)
                {
                    float sampleX = NoiseSpaceOrigin.x + GlobalNoiseOffset.x + x * SampleSpacing.x;
                    float value = EvaluateFbm(evaluator, new float2(sampleX, sampleY));

                    if (OutputRange == HeightFieldValueRange.UnsignedZeroToOne)
                        value = math.saturate(value * 0.5f + 0.5f);
                    else
                        value = math.clamp(value, -1f, 1f);

                    Destination[rowStart + x] = value;
                }
            }
        }

        private float EvaluateFbm(Evaluator2D evaluator, float2 samplePoint)
        {
            float value = 0f;
            float frequency = BaseFrequency;
            float amplitude = 1f;
            float amplitudeSum = 0f;

            for (int octave = 0; octave < OctaveCount; octave++)
            {
                value += evaluator.Evaluate(Seed + octave * OctaveSeedStep, samplePoint * frequency, Orientation) * amplitude;
                amplitudeSum += amplitude;
                frequency *= Lacunarity;
                amplitude *= Persistence;
            }

            if (NormalizeByAmplitude && amplitudeSum > 0f)
                value /= amplitudeSum;

            return value;
        }
    }
}
