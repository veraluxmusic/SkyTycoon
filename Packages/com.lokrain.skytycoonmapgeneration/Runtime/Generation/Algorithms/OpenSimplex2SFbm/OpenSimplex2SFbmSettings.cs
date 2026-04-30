#nullable enable

using System;
using System.Globalization;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    /// <summary>
    /// Immutable runtime settings for the F001 OpenSimplex2S fBM height-field source stage.
    /// Wavelength values are expressed in the same generation-space units as HeightFieldRequest.
    /// </summary>
    public readonly struct OpenSimplex2SFbmSettings : IEquatable<OpenSimplex2SFbmSettings>
    {
        public const int MinOctaveCount = 1;
        public const int MaxOctaveCount = 16;
        public const int DefaultOctaveCount = 4;
        public const int MinSuggestedJobTileSize = 1;
        public const int MaxSuggestedJobTileSize = 256;
        public const int DefaultSuggestedJobTileSize = 32;
        public const float DefaultBaseWavelength = 96f;
        public const float DefaultPersistence = 0.5f;
        public const float DefaultLacunarity = 2f;

        public readonly long Seed;
        public readonly int OctaveCount;
        public readonly float BaseWavelength;
        public readonly float Persistence;
        public readonly float Lacunarity;
        public readonly float2 GlobalNoiseOffset;
        public readonly OpenSimplex2SFbmOrientation Orientation;
        public readonly bool NormalizeByAmplitude;
        public readonly int SuggestedJobTileSize;

        public OpenSimplex2SFbmSettings(
            long seed,
            int octaveCount,
            float baseWavelength,
            float persistence,
            float lacunarity,
            float2 globalNoiseOffset,
            OpenSimplex2SFbmOrientation orientation,
            bool normalizeByAmplitude,
            int suggestedJobTileSize)
        {
            if (octaveCount < MinOctaveCount || octaveCount > MaxOctaveCount)
                throw new ArgumentOutOfRangeException(nameof(octaveCount), "Octave count must be inside the supported range.");
            if (!math.isfinite(baseWavelength) || baseWavelength <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseWavelength), "Base wavelength must be finite and greater than zero.");
            if (!math.isfinite(persistence) || persistence <= 0f || persistence > 1f)
                throw new ArgumentOutOfRangeException(nameof(persistence), "Persistence must be finite and in the range (0, 1].");
            if (!math.isfinite(lacunarity) || lacunarity < 1f)
                throw new ArgumentOutOfRangeException(nameof(lacunarity), "Lacunarity must be finite and greater than or equal to 1.");
            if (!math.all(math.isfinite(globalNoiseOffset)))
                throw new ArgumentOutOfRangeException(nameof(globalNoiseOffset), "Global noise offset must be finite.");
            if (suggestedJobTileSize < MinSuggestedJobTileSize || suggestedJobTileSize > MaxSuggestedJobTileSize)
                throw new ArgumentOutOfRangeException(nameof(suggestedJobTileSize), "Suggested job tile size must be inside the supported range.");

            Seed = seed;
            OctaveCount = octaveCount;
            BaseWavelength = baseWavelength;
            Persistence = persistence;
            Lacunarity = lacunarity;
            GlobalNoiseOffset = globalNoiseOffset;
            Orientation = orientation;
            NormalizeByAmplitude = normalizeByAmplitude;
            SuggestedJobTileSize = suggestedJobTileSize;
        }

        public float BaseFrequency => 1f / BaseWavelength;

        public static OpenSimplex2SFbmSettings CreateCentralIrregularContinentBaseline(long seed = 1)
        {
            return new OpenSimplex2SFbmSettings(
                seed: seed,
                octaveCount: DefaultOctaveCount,
                baseWavelength: DefaultBaseWavelength,
                persistence: DefaultPersistence,
                lacunarity: DefaultLacunarity,
                globalNoiseOffset: new float2(10000f, 10000f),
                orientation: OpenSimplex2SFbmOrientation.Standard,
                normalizeByAmplitude: true,
                suggestedJobTileSize: DefaultSuggestedJobTileSize);
        }

        public string CreateFingerprint()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return "seed=" + Seed.ToString(culture)
                + ";octaves=" + OctaveCount.ToString(culture)
                + ";baseWavelength=" + BaseWavelength.ToString("R", culture)
                + ";persistence=" + Persistence.ToString("R", culture)
                + ";lacunarity=" + Lacunarity.ToString("R", culture)
                + ";offset=" + GlobalNoiseOffset.x.ToString("R", culture) + "," + GlobalNoiseOffset.y.ToString("R", culture)
                + ";orientation=" + Orientation
                + ";normalize=" + NormalizeByAmplitude
                + ";tile=" + SuggestedJobTileSize.ToString(culture);
        }

        public bool Equals(OpenSimplex2SFbmSettings other)
        {
            return Seed == other.Seed
                && OctaveCount == other.OctaveCount
                && BaseWavelength.Equals(other.BaseWavelength)
                && Persistence.Equals(other.Persistence)
                && Lacunarity.Equals(other.Lacunarity)
                && GlobalNoiseOffset.Equals(other.GlobalNoiseOffset)
                && Orientation == other.Orientation
                && NormalizeByAmplitude == other.NormalizeByAmplitude
                && SuggestedJobTileSize == other.SuggestedJobTileSize;
        }

        public override bool Equals(object? obj)
        {
            return obj is OpenSimplex2SFbmSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Seed.GetHashCode();
                hash = hash * 397 ^ OctaveCount;
                hash = hash * 397 ^ BaseWavelength.GetHashCode();
                hash = hash * 397 ^ Persistence.GetHashCode();
                hash = hash * 397 ^ Lacunarity.GetHashCode();
                hash = hash * 397 ^ GlobalNoiseOffset.GetHashCode();
                hash = hash * 397 ^ (int)Orientation;
                hash = hash * 397 ^ NormalizeByAmplitude.GetHashCode();
                hash = hash * 397 ^ SuggestedJobTileSize;
                return hash;
            }
        }
    }
}
