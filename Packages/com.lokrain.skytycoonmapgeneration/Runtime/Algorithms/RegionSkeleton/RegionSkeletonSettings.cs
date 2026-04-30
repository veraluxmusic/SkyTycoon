#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Pure runtime settings for the first strategic skeleton stage.
    /// This deliberately contains no ScriptableObject, UnityEngine.Object, texture, or editor dependency.
    /// </summary>
    [Serializable]
    public readonly struct RegionSkeletonSettings : IEquatable<RegionSkeletonSettings>
    {
        public const int MinPlayerRegionCount = 2;
        public const int MaxPlayerRegionCount = 8;
        public const int MaxNeutralZoneCount = 3;

        public readonly HeightFieldDimensions Dimensions;
        public readonly DeterministicSeed Seed;
        public readonly int PlayerRegionCount;
        public readonly int NeutralZoneCount;
        public readonly RegionSkeletonNeutralPolicy NeutralPolicy;
        public readonly RegionSkeletonRoleAssignmentPolicy RoleAssignmentPolicy;
        public readonly float NeutralCoreRadiusPercentOfMinDimension;
        public readonly float BoundaryWarpAmplitudeTurns;
        public readonly int MinUsefulRegionNeighbors;
        public readonly float MinRegionAreaPercentOfMap;
        public readonly float MinNeutralAreaPercentOfMap;
        public readonly int MinRegionsTouchingNeutralZone;

        public RegionSkeletonSettings(
            HeightFieldDimensions dimensions,
            DeterministicSeed seed,
            int playerRegionCount,
            int neutralZoneCount,
            RegionSkeletonNeutralPolicy neutralPolicy,
            RegionSkeletonRoleAssignmentPolicy roleAssignmentPolicy,
            float neutralCoreRadiusPercentOfMinDimension,
            float boundaryWarpAmplitudeTurns,
            int minUsefulRegionNeighbors,
            float minRegionAreaPercentOfMap,
            float minNeutralAreaPercentOfMap,
            int minRegionsTouchingNeutralZone)
        {
            Dimensions = dimensions;
            Seed = seed;
            PlayerRegionCount = playerRegionCount;
            NeutralZoneCount = neutralZoneCount;
            NeutralPolicy = neutralPolicy;
            RoleAssignmentPolicy = roleAssignmentPolicy;
            NeutralCoreRadiusPercentOfMinDimension = neutralCoreRadiusPercentOfMinDimension;
            BoundaryWarpAmplitudeTurns = boundaryWarpAmplitudeTurns;
            MinUsefulRegionNeighbors = minUsefulRegionNeighbors;
            MinRegionAreaPercentOfMap = minRegionAreaPercentOfMap;
            MinNeutralAreaPercentOfMap = minNeutralAreaPercentOfMap;
            MinRegionsTouchingNeutralZone = minRegionsTouchingNeutralZone;
        }

        public static RegionSkeletonSettings CreateDefault(MapGenerationRequest request)
        {
            request.Validate();

            return new RegionSkeletonSettings(
                request.Dimensions,
                request.Seed.Derive("Stage.CompetitiveEconomySkeleton.RegionSkeleton"),
                request.PlayerCount,
                neutralZoneCount: 1,
                neutralPolicy: RegionSkeletonNeutralPolicy.CentralCore,
                roleAssignmentPolicy: RegionSkeletonRoleAssignmentPolicy.DeterministicShuffle,
                neutralCoreRadiusPercentOfMinDimension: 0.16f,
                boundaryWarpAmplitudeTurns: 0f,
                minUsefulRegionNeighbors: 2,
                minRegionAreaPercentOfMap: 0.06f,
                minNeutralAreaPercentOfMap: 0.025f,
                minRegionsTouchingNeutralZone: 4);
        }

        public void Validate()
        {
            Dimensions.Validate();

            if (Seed.Value == 0u)
                throw new ArgumentOutOfRangeException(nameof(Seed), Seed.Value, "Region skeleton seed must not be zero.");

            if (PlayerRegionCount < MinPlayerRegionCount || PlayerRegionCount > MaxPlayerRegionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PlayerRegionCount),
                    PlayerRegionCount,
                    "Region skeleton supports two to eight player/home regions in v0.0.1.");
            }

            if (NeutralZoneCount < 0 || NeutralZoneCount > MaxNeutralZoneCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(NeutralZoneCount),
                    NeutralZoneCount,
                    "Neutral zone count must be between zero and three.");
            }

            if (NeutralPolicy == RegionSkeletonNeutralPolicy.None && NeutralZoneCount != 0)
                throw new ArgumentException("Neutral zone count must be zero when neutral policy is None.");

            if (NeutralPolicy == RegionSkeletonNeutralPolicy.CentralCore && NeutralZoneCount != 1)
                throw new ArgumentException("CentralCore neutral policy currently requires exactly one neutral zone.");

            if (NeutralPolicy != RegionSkeletonNeutralPolicy.None && NeutralPolicy != RegionSkeletonNeutralPolicy.CentralCore)
                throw new ArgumentOutOfRangeException(nameof(NeutralPolicy), NeutralPolicy, "Unsupported neutral policy.");

            if (NeutralCoreRadiusPercentOfMinDimension < 0f || NeutralCoreRadiusPercentOfMinDimension > 0.35f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(NeutralCoreRadiusPercentOfMinDimension),
                    NeutralCoreRadiusPercentOfMinDimension,
                    "Neutral core radius must be between 0 and 0.35 of the minimum map dimension.");
            }

            if (BoundaryWarpAmplitudeTurns < 0f || BoundaryWarpAmplitudeTurns > 0.08f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(BoundaryWarpAmplitudeTurns),
                    BoundaryWarpAmplitudeTurns,
                    "Boundary warp amplitude must be between 0 and 0.08 turns.");
            }

            if (MinUsefulRegionNeighbors < 0 || MinUsefulRegionNeighbors > PlayerRegionCount - 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MinUsefulRegionNeighbors),
                    MinUsefulRegionNeighbors,
                    "Minimum useful region neighbors is outside the supported range.");
            }

            if (MinRegionAreaPercentOfMap <= 0f || MinRegionAreaPercentOfMap >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MinRegionAreaPercentOfMap),
                    MinRegionAreaPercentOfMap,
                    "Minimum region area percentage must be in the open range (0, 1).");
            }

            if (MinNeutralAreaPercentOfMap < 0f || MinNeutralAreaPercentOfMap >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MinNeutralAreaPercentOfMap),
                    MinNeutralAreaPercentOfMap,
                    "Minimum neutral area percentage must be in the range [0, 1).");
            }

            if (MinRegionsTouchingNeutralZone < 0 || MinRegionsTouchingNeutralZone > PlayerRegionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MinRegionsTouchingNeutralZone),
                    MinRegionsTouchingNeutralZone,
                    "Minimum regions touching a neutral zone is outside the supported range.");
            }
        }

        public bool Equals(RegionSkeletonSettings other)
        {
            return Dimensions == other.Dimensions
                && Seed == other.Seed
                && PlayerRegionCount == other.PlayerRegionCount
                && NeutralZoneCount == other.NeutralZoneCount
                && NeutralPolicy == other.NeutralPolicy
                && RoleAssignmentPolicy == other.RoleAssignmentPolicy
                && NeutralCoreRadiusPercentOfMinDimension.Equals(other.NeutralCoreRadiusPercentOfMinDimension)
                && BoundaryWarpAmplitudeTurns.Equals(other.BoundaryWarpAmplitudeTurns)
                && MinUsefulRegionNeighbors == other.MinUsefulRegionNeighbors
                && MinRegionAreaPercentOfMap.Equals(other.MinRegionAreaPercentOfMap)
                && MinNeutralAreaPercentOfMap.Equals(other.MinNeutralAreaPercentOfMap)
                && MinRegionsTouchingNeutralZone == other.MinRegionsTouchingNeutralZone;
        }

        public override bool Equals(object? obj)
        {
            return obj is RegionSkeletonSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Dimensions.GetHashCode();
                hash = hash * 31 + Seed.GetHashCode();
                hash = hash * 31 + PlayerRegionCount;
                hash = hash * 31 + NeutralZoneCount;
                hash = hash * 31 + NeutralPolicy.GetHashCode();
                hash = hash * 31 + RoleAssignmentPolicy.GetHashCode();
                hash = hash * 31 + NeutralCoreRadiusPercentOfMinDimension.GetHashCode();
                hash = hash * 31 + BoundaryWarpAmplitudeTurns.GetHashCode();
                hash = hash * 31 + MinUsefulRegionNeighbors;
                hash = hash * 31 + MinRegionAreaPercentOfMap.GetHashCode();
                hash = hash * 31 + MinNeutralAreaPercentOfMap.GetHashCode();
                hash = hash * 31 + MinRegionsTouchingNeutralZone;
                return hash;
            }
        }
    }
}
