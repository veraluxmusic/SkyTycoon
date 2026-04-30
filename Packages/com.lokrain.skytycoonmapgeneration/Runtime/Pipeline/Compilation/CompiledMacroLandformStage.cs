#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation
{
    /// <summary>
    /// Compiled runtime contract for Stage 1: Macro Landform.
    /// Contains no ScriptableObject or UnityEngine.Object references.
    /// </summary>
    public sealed class CompiledMacroLandformStage
    {
        public CompiledMacroLandformStage(MacroLandformSettings settings)
        {
            settings.Validate();

            StageId = MapGenerationStageIds.MacroLandform;
            Settings = settings;
            PlanHash = ComputePlanHash(settings);
        }

        public MapGenerationStageId StageId { get; }
        public MacroLandformSettings Settings { get; }
        public StableHash128 PlanHash { get; }

        public void Validate()
        {
            if (StageId.IsNone)
                throw new InvalidOperationException("Compiled macro landform stage id must not be None.");

            Settings.Validate();

            StableHash128 recomputed = ComputePlanHash(Settings);

            if (recomputed != PlanHash)
                throw new InvalidOperationException("Compiled macro landform stage hash is stale or corrupted.");
        }

        private StableHash128 ComputePlanHash(MacroLandformSettings settings)
        {
            return StableHash128.FromStableName("CompiledMacroLandformStage.v0.0.1")
                .Append(StageId.Value)
                .Append(settings.Dimensions.Width)
                .Append(settings.Dimensions.Height)
                .Append(settings.Seed.Value)
                .Append(settings.TargetLandPercent)
                .Append(settings.HardWaterBorderThickness)
                .Append(settings.ContinentFalloffExponent)
                .Append(settings.FbmOctaves)
                .Append(settings.FbmBaseWavelengthTiles)
                .Append(settings.FbmPersistence)
                .Append(settings.FbmLacunarity)
                .Append(settings.FbmAmplitude)
                .Append(settings.DomainWarpAmplitudeTiles)
                .Append(settings.DomainWarpWavelengthTiles)
                .Append(settings.MountainHeightContribution)
                .Append(settings.BasinHeightContribution)
                .Append(settings.PlainHeightContribution)
                .Append(settings.MountainBuildabilityPenalty)
                .Append(settings.FullyUnbuildableSlope);
        }
    }
}
