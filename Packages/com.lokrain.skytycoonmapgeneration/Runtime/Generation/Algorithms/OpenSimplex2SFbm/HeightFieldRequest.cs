using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    public readonly struct HeightFieldRequest
    {
        public readonly HeightFieldDimensions Dimensions;
        public readonly float2 NoiseSpaceOrigin;
        public readonly float2 NoiseSpaceSize;
        public readonly HeightFieldValueRange OutputRange;
        public readonly int JobTileSize;

        public HeightFieldRequest(
            HeightFieldDimensions dimensions,
            float2 noiseSpaceOrigin,
            float2 noiseSpaceSize,
            HeightFieldValueRange outputRange,
            int jobTileSize = 0)
        {
            Dimensions = dimensions;
            NoiseSpaceOrigin = noiseSpaceOrigin;
            NoiseSpaceSize = noiseSpaceSize;
            OutputRange = outputRange;
            JobTileSize = jobTileSize;
        }

        public int Width => Dimensions.Width;
        public int Height => Dimensions.Height;
        public int SampleCount => Dimensions.SampleCount;

        public void Validate()
        {
            if (NoiseSpaceSize.x <= 0f || NoiseSpaceSize.y <= 0f || !math.all(math.isfinite(NoiseSpaceSize)))
                throw new ArgumentOutOfRangeException(nameof(NoiseSpaceSize), "Noise-space size must be finite and both axes must be greater than zero.");
            if (!math.all(math.isfinite(NoiseSpaceOrigin)))
                throw new ArgumentOutOfRangeException(nameof(NoiseSpaceOrigin), "Noise-space origin must be finite.");
            if (JobTileSize < 0)
                throw new ArgumentOutOfRangeException(nameof(JobTileSize), "Job tile size must be zero or greater.");
        }

        public float2 CalculateSampleSpacing()
        {
            return new float2(
                NoiseSpaceSize.x / math.max(1, Width - 1),
                NoiseSpaceSize.y / math.max(1, Height - 1));
        }

        public int GetEffectiveJobTileSize(int fallbackJobTileSize)
        {
            return JobTileSize > 0 ? JobTileSize : math.max(1, fallbackJobTileSize);
        }

        public static HeightFieldRequest CreatePreview128(HeightFieldValueRange outputRange = HeightFieldValueRange.UnsignedZeroToOne)
        {
            return new HeightFieldRequest(
                dimensions: new HeightFieldDimensions(128, 128),
                noiseSpaceOrigin: float2.zero,
                noiseSpaceSize: new float2(4f, 4f),
                outputRange: outputRange,
                jobTileSize: 32);
        }
    }
}
