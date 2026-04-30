#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation
{
    /// <summary>
    /// Compiled runtime contract for Stage 0: Competitive Economy Skeleton.
    /// Contains no ScriptableObject or UnityEngine.Object references.
    /// </summary>
    public sealed class CompiledRegionSkeletonStage
    {
        public CompiledRegionSkeletonStage(
            RegionSkeletonSettings settings,
            RegionRoleCatalog roleCatalog)
        {
            if (roleCatalog == null)
                throw new ArgumentNullException(nameof(roleCatalog));

            settings.Validate();
            roleCatalog.ValidateForRegionCount(settings.PlayerRegionCount);

            StageId = MapGenerationStageIds.CompetitiveEconomySkeleton;
            Settings = settings;
            RoleCatalog = roleCatalog;
            PlanHash = ComputePlanHash(settings, roleCatalog);
        }

        public MapGenerationStageId StageId { get; }
        public RegionSkeletonSettings Settings { get; }
        public RegionRoleCatalog RoleCatalog { get; }
        public StableHash128 PlanHash { get; }

        public void Validate()
        {
            if (StageId.IsNone)
                throw new InvalidOperationException("Compiled region skeleton stage id must not be None.");

            Settings.Validate();
            RoleCatalog.ValidateForRegionCount(Settings.PlayerRegionCount);

            StableHash128 recomputed = ComputePlanHash(Settings, RoleCatalog);

            if (recomputed != PlanHash)
                throw new InvalidOperationException("Compiled region skeleton stage hash is stale or corrupted.");
        }

        private StableHash128 ComputePlanHash(
            RegionSkeletonSettings settings,
            RegionRoleCatalog roleCatalog)
        {
            StableHash128 hash = StableHash128.FromStableName("CompiledRegionSkeletonStage.v0.0.1")
                .Append(StageId.Value)
                .Append(settings.Dimensions.Width)
                .Append(settings.Dimensions.Height)
                .Append(settings.Seed.Value)
                .Append(settings.PlayerRegionCount)
                .Append(settings.NeutralZoneCount)
                .Append((int)settings.NeutralPolicy)
                .Append((int)settings.RoleAssignmentPolicy)
                .Append(settings.NeutralCoreRadiusPercentOfMinDimension)
                .Append(settings.BoundaryWarpAmplitudeTurns)
                .Append(settings.MinUsefulRegionNeighbors)
                .Append(settings.MinRegionAreaPercentOfMap)
                .Append(settings.MinNeutralAreaPercentOfMap)
                .Append(settings.MinRegionsTouchingNeutralZone)
                .Append(roleCatalog.Count);

            RegionRoleDefinition[] definitions = roleCatalog.CopyDefinitions();

            for (int i = 0; i < definitions.Length; i++)
            {
                RegionRoleDefinition definition = definitions[i];

                hash = hash
                    .Append(i)
                    .Append(definition.Id.Value)
                    .Append((int)definition.PrimaryIdentity)
                    .AppendStableString(definition.DisplayName)
                    .AppendStableString(definition.SecondaryFlavor)
                    .AppendStableString(definition.RequiredLocalStrength)
                    .AppendStableString(definition.RequiredWeakness)
                    .AppendStableString(definition.ExportTarget)
                    .AppendStableString(definition.ImportTemptation)
                    .AppendStableString(definition.ScenicIdentity)
                    .AppendStableString(definition.PreferredTerrain)
                    .AppendStableString(definition.PreferredClimate)
                    .AppendStableString(definition.PreferredGeology)
                    .AppendStableString(definition.PreferredConnectivity);
            }

            return hash;
        }
    }
}
