#nullable enable

using System;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Generic request used by height-field source stages.
    /// Coordinates are expressed in generation-space units. For map-generation work, use tile units unless a stage explicitly says otherwise.
    /// </summary>
    public readonly struct HeightFieldRequest : IEquatable<HeightFieldRequest>
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

            Validate();
        }

        public int Width => Dimensions.Width;
        public int Height => Dimensions.Height;
        public int SampleCount => Dimensions.SampleCount;

        public void Validate()
        {
            if (!math.all(math.isfinite(NoiseSpaceOrigin)))
                throw new ArgumentOutOfRangeException(nameof(NoiseSpaceOrigin), "Noise-space origin must be finite.");
            if (!math.all(math.isfinite(NoiseSpaceSize)) || NoiseSpaceSize.x <= 0f || NoiseSpaceSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(NoiseSpaceSize), "Noise-space size must be finite and both axes must be greater than zero.");
            if (JobTileSize < 0)
                throw new ArgumentOutOfRangeException(nameof(JobTileSize), "Job tile size must be zero or greater. Zero means use the generator default.");
        }

        public float2 CalculateSampleSpacing()
        {
            return new float2(
                NoiseSpaceSize.x / math.max(1, Width - 1),
                NoiseSpaceSize.y / math.max(1, Height - 1));
        }

        public int GetEffectiveJobTileSize(int fallbackJobTileSize)
        {
            if (fallbackJobTileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(fallbackJobTileSize), "Fallback job tile size must be greater than zero.");

            return JobTileSize > 0 ? JobTileSize : fallbackJobTileSize;
        }

        public bool Equals(HeightFieldRequest other)
        {
            return Dimensions.Equals(other.Dimensions)
                && NoiseSpaceOrigin.Equals(other.NoiseSpaceOrigin)
                && NoiseSpaceSize.Equals(other.NoiseSpaceSize)
                && OutputRange == other.OutputRange
                && JobTileSize == other.JobTileSize;
        }

        public override bool Equals(object? obj)
        {
            return obj is HeightFieldRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Dimensions, NoiseSpaceOrigin, NoiseSpaceSize, OutputRange, JobTileSize);

        }

        public override string ToString()
        {
            return Dimensions + " origin=" + NoiseSpaceOrigin + " size=" + NoiseSpaceSize + " range=" + OutputRange + " tile=" + JobTileSize;
        }
    }
}
