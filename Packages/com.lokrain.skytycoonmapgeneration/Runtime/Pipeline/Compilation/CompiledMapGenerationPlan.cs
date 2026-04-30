#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation
{
    /// <summary>
    /// Pure runtime generation plan produced from authoring assets.
    /// Stage contracts are explicit typed properties; do not hide generation stages behind object bags.
    /// </summary>
    public sealed class CompiledMapGenerationPlan
    {
        public CompiledMapGenerationPlan(
            MapGenerationRequest request,
            CompiledRegionSkeletonStage regionSkeleton)
            : this(
                request,
                regionSkeleton,
                new CompiledMacroLandformStage(MacroLandformSettings.CreateDefault(request)))
        {
        }

        public CompiledMapGenerationPlan(
            MapGenerationRequest request,
            CompiledRegionSkeletonStage regionSkeleton,
            CompiledMacroLandformStage macroLandform)
        {
            if (regionSkeleton == null)
                throw new ArgumentNullException(nameof(regionSkeleton));

            if (macroLandform == null)
                throw new ArgumentNullException(nameof(macroLandform));

            request.Validate();
            regionSkeleton.Validate();
            macroLandform.Validate();

            if (regionSkeleton.Settings.Dimensions != request.Dimensions)
                throw new ArgumentException("Region skeleton stage dimensions must match the request.", nameof(regionSkeleton));

            if (macroLandform.Settings.Dimensions != request.Dimensions)
                throw new ArgumentException("Macro landform stage dimensions must match the request.", nameof(macroLandform));

            Request = request;
            RegionSkeleton = regionSkeleton;
            MacroLandform = macroLandform;
            PlanHash = ComputePlanHash(request, regionSkeleton, macroLandform);
        }

        public MapGenerationRequest Request { get; }
        public CompiledRegionSkeletonStage RegionSkeleton { get; }
        public CompiledMacroLandformStage MacroLandform { get; }
        public StableHash128 PlanHash { get; }

        public void Validate()
        {
            Request.Validate();
            RegionSkeleton.Validate();
            MacroLandform.Validate();

            StableHash128 recomputed = ComputePlanHash(Request, RegionSkeleton, MacroLandform);

            if (recomputed != PlanHash)
                throw new InvalidOperationException("Compiled map generation plan hash is stale or corrupted.");
        }

        private static StableHash128 ComputePlanHash(
            MapGenerationRequest request,
            CompiledRegionSkeletonStage regionSkeleton,
            CompiledMacroLandformStage macroLandform)
        {
            return StableHash128.FromStableName("CompiledMapGenerationPlan.v0.0.1")
                .Append(request.Dimensions.Width)
                .Append(request.Dimensions.Height)
                .Append(request.Seed.Value)
                .Append((int)request.Archetype)
                .Append((int)request.Mode)
                .Append(request.PlayerCount)
                .Append(regionSkeleton.StageId.Value)
                .Append(regionSkeleton.PlanHash)
                .Append(macroLandform.StageId.Value)
                .Append(macroLandform.PlanHash);
        }
    }
}
