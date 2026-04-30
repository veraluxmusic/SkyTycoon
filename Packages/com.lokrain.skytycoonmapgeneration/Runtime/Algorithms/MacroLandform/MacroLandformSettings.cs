#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform
{
    /// <summary>
    /// Pure runtime settings for Stage 1: Macro Landform.
    ///
    /// This stage produces the first terrain-bearing fields from the strategic region skeleton:
    /// continent mask, base height, influence fields, slope, buildability, and distance-to-coast.
    /// It deliberately does not know about ScriptableObjects, textures, editor UI, or game entities.
    /// </summary>
    [Serializable]
    public readonly struct MacroLandformSettings : IEquatable<MacroLandformSettings>
    {
        public const float MinTargetLandPercent = 0.30f;
        public const float MaxTargetLandPercent = 0.90f;
        public const int MinHardWaterBorderThickness = 1;
        public const int MaxHardWaterBorderThickness = 32;
        public const int MinFbmOctaves = 1;
        public const int MaxFbmOctaves = 8;

        public readonly HeightFieldDimensions Dimensions;
        public readonly DeterministicSeed Seed;
        public readonly float TargetLandPercent;
        public readonly int HardWaterBorderThickness;
        public readonly float ContinentFalloffExponent;
        public readonly int FbmOctaves;
        public readonly float FbmBaseWavelengthTiles;
        public readonly float FbmPersistence;
        public readonly float FbmLacunarity;
        public readonly float FbmAmplitude;
        public readonly float DomainWarpAmplitudeTiles;
        public readonly float DomainWarpWavelengthTiles;
        public readonly float MountainHeightContribution;
        public readonly float BasinHeightContribution;
        public readonly float PlainHeightContribution;
        public readonly float MountainBuildabilityPenalty;
        public readonly float FullyUnbuildableSlope;

        public MacroLandformSettings(
            HeightFieldDimensions dimensions,
            DeterministicSeed seed,
            float targetLandPercent,
            int hardWaterBorderThickness,
            float continentFalloffExponent,
            int fbmOctaves,
            float fbmBaseWavelengthTiles,
            float fbmPersistence,
            float fbmLacunarity,
            float fbmAmplitude,
            float domainWarpAmplitudeTiles,
            float domainWarpWavelengthTiles,
            float mountainHeightContribution,
            float basinHeightContribution,
            float plainHeightContribution,
            float mountainBuildabilityPenalty,
            float fullyUnbuildableSlope)
        {
            Dimensions = dimensions;
            Seed = seed;
            TargetLandPercent = targetLandPercent;
            HardWaterBorderThickness = hardWaterBorderThickness;
            ContinentFalloffExponent = continentFalloffExponent;
            FbmOctaves = fbmOctaves;
            FbmBaseWavelengthTiles = fbmBaseWavelengthTiles;
            FbmPersistence = fbmPersistence;
            FbmLacunarity = fbmLacunarity;
            FbmAmplitude = fbmAmplitude;
            DomainWarpAmplitudeTiles = domainWarpAmplitudeTiles;
            DomainWarpWavelengthTiles = domainWarpWavelengthTiles;
            MountainHeightContribution = mountainHeightContribution;
            BasinHeightContribution = basinHeightContribution;
            PlainHeightContribution = plainHeightContribution;
            MountainBuildabilityPenalty = mountainBuildabilityPenalty;
            FullyUnbuildableSlope = fullyUnbuildableSlope;
        }

        public static MacroLandformSettings CreateDefault(MapGenerationRequest request)
        {
            request.Validate();

            return new MacroLandformSettings(
                request.Dimensions,
                request.Seed.Derive("Stage.MacroLandform.SingleContinent"),
                targetLandPercent: 0.70f,
                hardWaterBorderThickness: 1,
                continentFalloffExponent: 2.35f,
                fbmOctaves: 4,
                fbmBaseWavelengthTiles: math.max(16f, math.min(request.Dimensions.Width, request.Dimensions.Height) * 0.75f),
                fbmPersistence: 0.52f,
                fbmLacunarity: 2.0f,
                fbmAmplitude: 0.22f,
                domainWarpAmplitudeTiles: math.max(2f, math.min(request.Dimensions.Width, request.Dimensions.Height) * 0.045f),
                domainWarpWavelengthTiles: math.max(16f, math.min(request.Dimensions.Width, request.Dimensions.Height) * 0.50f),
                mountainHeightContribution: 0.36f,
                basinHeightContribution: 0.22f,
                plainHeightContribution: 0.10f,
                mountainBuildabilityPenalty: 0.45f,
                fullyUnbuildableSlope: 0.18f);
        }

        public int TargetLandSampleCount
        {
            get
            {
                Validate();
                return (int)math.round(Dimensions.SampleCount * TargetLandPercent);
            }
        }

        public void Validate()
        {
            Dimensions.Validate();

            if (Seed.Value == 0u)
                throw new ArgumentOutOfRangeException(nameof(Seed), Seed.Value, "Macro landform seed must not be zero.");

            if (TargetLandPercent < MinTargetLandPercent || TargetLandPercent > MaxTargetLandPercent)
                throw new ArgumentOutOfRangeException(nameof(TargetLandPercent), TargetLandPercent, "Target land percentage is outside the supported v0.0.1 range.");

            if (HardWaterBorderThickness < MinHardWaterBorderThickness || HardWaterBorderThickness > MaxHardWaterBorderThickness)
                throw new ArgumentOutOfRangeException(nameof(HardWaterBorderThickness), HardWaterBorderThickness, "Hard water border thickness is outside the supported range.");

            if (HardWaterBorderThickness * 2 >= Dimensions.Width || HardWaterBorderThickness * 2 >= Dimensions.Height)
                throw new ArgumentOutOfRangeException(nameof(HardWaterBorderThickness), HardWaterBorderThickness, "Hard water border consumes the whole map.");

            int interiorWidth = Dimensions.Width - HardWaterBorderThickness * 2;
            int interiorHeight = Dimensions.Height - HardWaterBorderThickness * 2;
            int interiorSampleCount = interiorWidth * interiorHeight;
            int targetLandCount = (int)math.round(Dimensions.SampleCount * TargetLandPercent);

            if (targetLandCount <= 0 || targetLandCount > interiorSampleCount)
                throw new ArgumentOutOfRangeException(nameof(TargetLandPercent), TargetLandPercent, "Target land sample count must fit inside the non-border interior area.");

            if (ContinentFalloffExponent < 0.50f || ContinentFalloffExponent > 8.0f)
                throw new ArgumentOutOfRangeException(nameof(ContinentFalloffExponent), ContinentFalloffExponent, "Continent falloff exponent is outside the supported range.");

            if (FbmOctaves < MinFbmOctaves || FbmOctaves > MaxFbmOctaves)
                throw new ArgumentOutOfRangeException(nameof(FbmOctaves), FbmOctaves, "fBm octave count is outside the supported range.");

            if (FbmBaseWavelengthTiles < 2.0f)
                throw new ArgumentOutOfRangeException(nameof(FbmBaseWavelengthTiles), FbmBaseWavelengthTiles, "fBm base wavelength must be at least two tiles.");

            if (FbmPersistence <= 0f || FbmPersistence >= 1f)
                throw new ArgumentOutOfRangeException(nameof(FbmPersistence), FbmPersistence, "fBm persistence must be in the open range (0, 1).");

            if (FbmLacunarity <= 1f || FbmLacunarity > 4f)
                throw new ArgumentOutOfRangeException(nameof(FbmLacunarity), FbmLacunarity, "fBm lacunarity must be in the range (1, 4].");

            if (FbmAmplitude < 0f || FbmAmplitude > 1f)
                throw new ArgumentOutOfRangeException(nameof(FbmAmplitude), FbmAmplitude, "fBm amplitude must be in the range [0, 1].");

            if (DomainWarpAmplitudeTiles < 0f)
                throw new ArgumentOutOfRangeException(nameof(DomainWarpAmplitudeTiles), DomainWarpAmplitudeTiles, "Domain warp amplitude must not be negative.");

            if (DomainWarpWavelengthTiles < 2f)
                throw new ArgumentOutOfRangeException(nameof(DomainWarpWavelengthTiles), DomainWarpWavelengthTiles, "Domain warp wavelength must be at least two tiles.");

            if (MountainHeightContribution < 0f || BasinHeightContribution < 0f || PlainHeightContribution < 0f)
                throw new ArgumentOutOfRangeException(nameof(MountainHeightContribution), "Height contributions must not be negative.");

            if (MountainBuildabilityPenalty < 0f || MountainBuildabilityPenalty > 1f)
                throw new ArgumentOutOfRangeException(nameof(MountainBuildabilityPenalty), MountainBuildabilityPenalty, "Mountain buildability penalty must be in the range [0, 1].");

            if (FullyUnbuildableSlope <= 0.001f || FullyUnbuildableSlope > 2f)
                throw new ArgumentOutOfRangeException(nameof(FullyUnbuildableSlope), FullyUnbuildableSlope, "Fully unbuildable slope must be positive and reasonable.");
        }

        public bool Equals(MacroLandformSettings other)
        {
            return Dimensions == other.Dimensions
                && Seed == other.Seed
                && TargetLandPercent.Equals(other.TargetLandPercent)
                && HardWaterBorderThickness == other.HardWaterBorderThickness
                && ContinentFalloffExponent.Equals(other.ContinentFalloffExponent)
                && FbmOctaves == other.FbmOctaves
                && FbmBaseWavelengthTiles.Equals(other.FbmBaseWavelengthTiles)
                && FbmPersistence.Equals(other.FbmPersistence)
                && FbmLacunarity.Equals(other.FbmLacunarity)
                && FbmAmplitude.Equals(other.FbmAmplitude)
                && DomainWarpAmplitudeTiles.Equals(other.DomainWarpAmplitudeTiles)
                && DomainWarpWavelengthTiles.Equals(other.DomainWarpWavelengthTiles)
                && MountainHeightContribution.Equals(other.MountainHeightContribution)
                && BasinHeightContribution.Equals(other.BasinHeightContribution)
                && PlainHeightContribution.Equals(other.PlainHeightContribution)
                && MountainBuildabilityPenalty.Equals(other.MountainBuildabilityPenalty)
                && FullyUnbuildableSlope.Equals(other.FullyUnbuildableSlope);
        }

        public override bool Equals(object? obj)
        {
            return obj is MacroLandformSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Dimensions.GetHashCode();
                hash = hash * 31 + Seed.GetHashCode();
                hash = hash * 31 + TargetLandPercent.GetHashCode();
                hash = hash * 31 + HardWaterBorderThickness;
                hash = hash * 31 + ContinentFalloffExponent.GetHashCode();
                hash = hash * 31 + FbmOctaves;
                hash = hash * 31 + FbmBaseWavelengthTiles.GetHashCode();
                hash = hash * 31 + FbmPersistence.GetHashCode();
                hash = hash * 31 + FbmLacunarity.GetHashCode();
                hash = hash * 31 + FbmAmplitude.GetHashCode();
                hash = hash * 31 + DomainWarpAmplitudeTiles.GetHashCode();
                hash = hash * 31 + DomainWarpWavelengthTiles.GetHashCode();
                hash = hash * 31 + MountainHeightContribution.GetHashCode();
                hash = hash * 31 + BasinHeightContribution.GetHashCode();
                hash = hash * 31 + PlainHeightContribution.GetHashCode();
                hash = hash * 31 + MountainBuildabilityPenalty.GetHashCode();
                hash = hash * 31 + FullyUnbuildableSlope.GetHashCode();
                return hash;
            }
        }
    }
}
