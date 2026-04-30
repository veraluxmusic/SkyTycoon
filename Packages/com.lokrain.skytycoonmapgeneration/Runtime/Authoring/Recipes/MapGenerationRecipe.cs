#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.Recipes
{
    /// <summary>
    /// Central authoring asset for a map-generation recipe.
    /// Profiles are authoring inputs only; Compile produces a pure runtime plan.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MapGenerationRecipe",
        menuName = "Sky Tycoon/Map Generation/Map Generation Recipe")]
    public sealed class MapGenerationRecipe : ScriptableObject
    {
        [SerializeField] private RegionSkeletonProfile? _regionSkeletonProfile;
        [SerializeField] private MacroLandformProfile? _macroLandformProfile;

        public RegionSkeletonProfile? RegionSkeletonProfile => _regionSkeletonProfile;
        public MacroLandformProfile? MacroLandformProfile => _macroLandformProfile;

        public bool HasRegionSkeletonProfile => _regionSkeletonProfile != null;
        public bool HasMacroLandformProfile => _macroLandformProfile != null;

        public CompiledMapGenerationPlan Compile(MapGenerationRequest request)
        {
            return Compile(new GenerationCompileContext(request));
        }

        public CompiledMapGenerationPlan Compile(GenerationCompileContext context)
        {
            context.Validate();

            RegionSkeletonSettings regionSkeletonSettings = CompileRegionSkeletonSettings(context.Request);
            RegionRoleCatalog regionRoleCatalog = CreateRegionRoleCatalog();
            MacroLandformSettings macroLandformSettings = CompileMacroLandformSettings(context.Request);

            CompiledRegionSkeletonStage regionSkeletonStage = new(
                regionSkeletonSettings,
                regionRoleCatalog);

            CompiledMacroLandformStage macroLandformStage = new(
                macroLandformSettings);

            return new CompiledMapGenerationPlan(
                context.Request,
                regionSkeletonStage,
                macroLandformStage);
        }

        public RegionSkeletonSettings CompileRegionSkeletonSettings(MapGenerationRequest request)
        {
            RegionSkeletonProfile profile = GetRequiredRegionSkeletonProfile();
            return profile.CompileSettings(request);
        }

        public RegionRoleCatalog CreateRegionRoleCatalog()
        {
            RegionSkeletonProfile profile = GetRequiredRegionSkeletonProfile();
            return profile.CreateRoleCatalog();
        }

        public MacroLandformSettings CompileMacroLandformSettings(MapGenerationRequest request)
        {
            MacroLandformProfile profile = GetRequiredMacroLandformProfile();
            return profile.CompileSettings(request);
        }

        private RegionSkeletonProfile GetRequiredRegionSkeletonProfile()
        {
            if (_regionSkeletonProfile == null)
            {
                throw new InvalidOperationException(
                    "Map generation recipe requires a RegionSkeletonProfile before the Strategic Map Skeleton stage can be compiled.");
            }

            return _regionSkeletonProfile;
        }

        private MacroLandformProfile GetRequiredMacroLandformProfile()
        {
            if (_macroLandformProfile == null)
            {
                throw new InvalidOperationException(
                    "Map generation recipe requires a MacroLandformProfile before the Macro Landform stage can be compiled.");
            }

            return _macroLandformProfile;
        }
    }
}
