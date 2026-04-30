#nullable enable

using System;
using Unity.Collections;
using Unity.Mathematics;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Deterministic generator for the first strategic map field: player regions and neutral economic core.
    /// This stage intentionally does not generate terrain. It produces strategic ownership intent that later
    /// stages must respect and explain.
    /// </summary>
    public sealed class RegionSkeletonGenerator
    {
        private const float TwoPi = math.PI * 2f;

        public RegionSkeletonResult Generate(
            RegionSkeletonSettings settings,
            RegionRoleCatalog roleCatalog,
            Allocator allocator = Allocator.Persistent)
        {
            settings.Validate();

            if (roleCatalog == null)
                throw new ArgumentNullException(nameof(roleCatalog));

            roleCatalog.ValidateForRegionCount(settings.PlayerRegionCount);

            if (allocator == Allocator.Invalid || allocator == Allocator.None)
                throw new ArgumentOutOfRangeException(nameof(allocator), allocator, "Region skeleton output allocator must create native storage.");

            NativeField2D<int> regionIdField = new(MapFieldIds.RegionId, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<int> neutralZoneIdField = new(MapFieldIds.NeutralZoneId, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);

            try
            {
                GenerateFields(settings, regionIdField.Samples, neutralZoneIdField.Samples);

                RegionRoleAssignment[] roleAssignments = BuildRoleAssignments(settings, roleCatalog);

                RegionAdjacencyGraph adjacencyGraph = RegionAdjacencyGraph.BuildFromFields(
                    regionIdField.Samples,
                    neutralZoneIdField.Samples,
                    settings.Dimensions,
                    settings.PlayerRegionCount,
                    settings.NeutralZoneCount);

                StableHash128 artifactHash = ComputeArtifactHash(settings, regionIdField.Samples, neutralZoneIdField.Samples, roleAssignments);

                return new RegionSkeletonResult(
                    settings,
                    regionIdField,
                    neutralZoneIdField,
                    roleAssignments,
                    adjacencyGraph,
                    artifactHash);
            }
            catch
            {
                regionIdField.Dispose();
                neutralZoneIdField.Dispose();
                throw;
            }
        }

        public RegionSkeletonResult GenerateDefault(
            MapGenerationRequest request,
            Allocator allocator = Allocator.Persistent)
        {
            RegionSkeletonSettings settings = RegionSkeletonSettings.CreateDefault(request);
            RegionRoleCatalog catalog = RegionRoleCatalog.CreateTycoonEightRegionDefault();
            return Generate(settings, catalog, allocator);
        }

        private static void GenerateFields(
            RegionSkeletonSettings settings,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds)
        {
            HeightFieldDimensions dimensions = settings.Dimensions;
            float centerX = (dimensions.Width - 1) * 0.5f;
            float centerY = (dimensions.Height - 1) * 0.5f;
            float minDimension = math.min(dimensions.Width, dimensions.Height);
            float neutralRadius = minDimension * settings.NeutralCoreRadiusPercentOfMinDimension;
            float angularOffsetTurns = ComputeAngularOffsetTurns(settings.Seed.Value);
            float phaseA = ComputePhaseRadians(settings.Seed.Value, 0x9E3779B9u);
            float phaseB = ComputePhaseRadians(settings.Seed.Value, 0x85EBCA6Bu);

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int index = y * dimensions.Width + x;
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = math.sqrt(dx * dx + dy * dy);

                    if (settings.NeutralPolicy == RegionSkeletonNeutralPolicy.CentralCore && distance <= neutralRadius)
                    {
                        regionIds[index] = RegionId.NoneValue;
                        neutralZoneIds[index] = NeutralZoneId.MinValue;
                        continue;
                    }

                    float turn = math.atan2(dy, dx) / TwoPi;

                    if (turn < 0f)
                        turn += 1f;

                    float radial01 = minDimension <= 0f
                        ? 0f
                        : math.saturate((distance - neutralRadius) / math.max(1f, minDimension * 0.5f - neutralRadius));

                    if (settings.BoundaryWarpAmplitudeTurns > 0f)
                    {
                        float warp = math.sin(turn * TwoPi * 3f + phaseA) + 0.5f * math.sin(turn * TwoPi * 5f + phaseB);
                        turn += settings.BoundaryWarpAmplitudeTurns * radial01 * (warp / 1.5f);
                    }

                    turn = math.frac(turn + angularOffsetTurns);
                    int regionValue = (int)math.floor(turn * settings.PlayerRegionCount) + RegionId.MinValue;

                    if (regionValue > settings.PlayerRegionCount)
                        regionValue = settings.PlayerRegionCount;

                    regionIds[index] = regionValue;
                    neutralZoneIds[index] = NeutralZoneId.NoneValue;
                }
            }
        }

        private static RegionRoleAssignment[] BuildRoleAssignments(RegionSkeletonSettings settings, RegionRoleCatalog roleCatalog)
        {
            RegionRoleDefinition[] definitions = roleCatalog.CopyDefinitions();

            if (settings.RoleAssignmentPolicy == RegionSkeletonRoleAssignmentPolicy.DeterministicShuffle)
                DeterministicShuffle(settings.Seed.Value, definitions);
            else if (settings.RoleAssignmentPolicy != RegionSkeletonRoleAssignmentPolicy.FixedCatalogOrder)
                throw new ArgumentOutOfRangeException(nameof(settings.RoleAssignmentPolicy), settings.RoleAssignmentPolicy, "Unsupported region role assignment policy.");

            RegionRoleAssignment[] assignments = new RegionRoleAssignment[settings.PlayerRegionCount];

            for (int i = 0; i < assignments.Length; i++)
                assignments[i] = new RegionRoleAssignment(new RegionId(i + 1), definitions[i].Id);

            return assignments;
        }

        private static void DeterministicShuffle(uint seed, RegionRoleDefinition[] definitions)
        {
            Unity.Mathematics.Random random = new(seed == 0u ? 1u : seed);

            for (int i = definitions.Length - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (definitions[j], definitions[i]) = (definitions[i], definitions[j]);
            }
        }

        private static StableHash128 ComputeArtifactHash(
            RegionSkeletonSettings settings,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds,
            RegionRoleAssignment[] roleAssignments)
        {
            StableHash128 hash = StableHash128.FromStableName("RegionSkeletonResult.v0.0.1")
                .Append(settings.Dimensions.Width)
                .Append(settings.Dimensions.Height)
                .Append(settings.Seed.Value)
                .Append(settings.PlayerRegionCount)
                .Append(settings.NeutralZoneCount)
                .Append((int)settings.NeutralPolicy)
                .Append((int)settings.RoleAssignmentPolicy)
                .Append(FloatToDeterministicInt(settings.NeutralCoreRadiusPercentOfMinDimension))
                .Append(FloatToDeterministicInt(settings.BoundaryWarpAmplitudeTurns));

            for (int i = 0; i < roleAssignments.Length; i++)
            {
                hash = hash
                    .Append(roleAssignments[i].RegionId.Value)
                    .Append(roleAssignments[i].RoleId.Value);
            }

            for (int i = 0; i < regionIds.Length; i++)
                hash = hash.Append(regionIds[i]).Append(neutralZoneIds[i]);

            return hash;
        }

        private static int FloatToDeterministicInt(float value)
        {
            return (int)math.round(value * 1_000_000f);
        }

        private static float ComputeAngularOffsetTurns(uint seed)
        {
            uint mixed = Mix(seed ^ 0xC2B2AE35u);
            return (mixed & 0x00FFFFFFu) / 16777216f;
        }

        private static float ComputePhaseRadians(uint seed, uint salt)
        {
            uint mixed = Mix(seed ^ salt);
            return ((mixed & 0x00FFFFFFu) / 16777216f) * TwoPi;
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value == 0u ? 1u : value;
        }
    }
}
