using System;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm 
{
    public readonly struct FbmSettings
    {
        public const int MinOctaveCount = 1;
        public const int MaxOctaveCount = 16;
        public const int DefaultOctaveCount = 4;
        public const int DefaultSuggestedJobTileSize = 32;

        public readonly long Seed;
        public readonly int OctaveCount;
        public readonly float BaseFrequency;
        public readonly float Persistence;
        public readonly float Lacunarity;
        public readonly float2 GlobalNoiseOffset;
        public readonly Orientation Orientation;
        public readonly bool NormalizeByAmplitude;
        public readonly int SuggestedJobTileSize;

        public FbmSettings(
            long seed,
            int octaveCount,
            float baseFrequency,
            float persistence,
            float lacunarity,
            float2 globalNoiseOffset,
            Orientation orientation,
            bool normalizeByAmplitude,
            int suggestedJobTileSize)
        {
            if (octaveCount < MinOctaveCount || octaveCount > MaxOctaveCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(octaveCount),
                    $"Octave count must be in the range [{MinOctaveCount}, {MaxOctaveCount}].");
            }

            if (!math.isfinite(baseFrequency) || baseFrequency <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseFrequency), "Base frequency must be finite and greater than zero.");
            if (!math.isfinite(persistence) || persistence <= 0f || persistence > 1f)
                throw new ArgumentOutOfRangeException(nameof(persistence), "Persistence must be finite and in the range (0, 1].");
            if (!math.isfinite(lacunarity) || lacunarity < 1f)
                throw new ArgumentOutOfRangeException(nameof(lacunarity), "Lacunarity must be finite and greater than or equal to 1.");
            if (!math.all(math.isfinite(globalNoiseOffset)))
                throw new ArgumentOutOfRangeException(nameof(globalNoiseOffset), "Global noise offset must be finite.");
            if (suggestedJobTileSize < 1)
                throw new ArgumentOutOfRangeException(nameof(suggestedJobTileSize), "Suggested job tile size must be greater than zero.");

            Seed = seed;
            OctaveCount = octaveCount;
            BaseFrequency = baseFrequency;
            Persistence = persistence;
            Lacunarity = lacunarity;
            GlobalNoiseOffset = globalNoiseOffset;
            Orientation = orientation;
            NormalizeByAmplitude = normalizeByAmplitude;
            SuggestedJobTileSize = suggestedJobTileSize;
        }

        public static FbmSettings CreateDefault(long seed = 1)
        {
            return new FbmSettings(
                seed: seed,
                octaveCount: DefaultOctaveCount,
                baseFrequency: 1f,
                persistence: 0.5f,
                lacunarity: 2f,
                globalNoiseOffset: new float2(10000f, 10000f),
                orientation: Orientation.Standard,
                normalizeByAmplitude: true,
                suggestedJobTileSize: DefaultSuggestedJobTileSize);
        }
    }
}
