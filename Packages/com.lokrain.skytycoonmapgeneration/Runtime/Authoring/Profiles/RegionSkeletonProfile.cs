#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles
{
    /// <summary>
    /// Designer-facing authoring profile for Stage 0: Competitive Economy Skeleton.
    /// Compile this asset into RegionSkeletonSettings and RegionRoleCatalog before runtime generation.
    /// Jobs and algorithms must never read this ScriptableObject directly.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RegionSkeletonProfile",
        menuName = "Sky Tycoon/Map Generation/Region Skeleton Profile")]
    public sealed class RegionSkeletonProfile : ScriptableObject
    {
        private const string StageSeedScope = "Stage.CompetitiveEconomySkeleton.RegionSkeleton";

        [Header("Neutral Zones")]
        [SerializeField] private RegionSkeletonNeutralPolicy _neutralPolicy = RegionSkeletonNeutralPolicy.CentralCore;
        [SerializeField, Range(0, RegionSkeletonSettings.MaxNeutralZoneCount)] private int _neutralZoneCount = 1;
        [SerializeField, Range(0f, 0.35f)] private float _neutralCoreRadiusPercentOfMinDimension = 0.16f;

        [Header("Region Assignment")]
        [SerializeField] private RegionSkeletonRoleAssignmentPolicy _roleAssignmentPolicy = RegionSkeletonRoleAssignmentPolicy.DeterministicShuffle;
        [SerializeField, Range(0f, 0.08f)] private float _boundaryWarpAmplitudeTurns;

        [Header("Validation Thresholds")]
        [SerializeField, Range(0, RegionSkeletonSettings.MaxPlayerRegionCount - 1)] private int _minUsefulRegionNeighbors = 2;
        [SerializeField, Range(0.01f, 0.20f)] private float _minRegionAreaPercentOfMap = 0.06f;
        [SerializeField, Range(0f, 0.25f)] private float _minNeutralAreaPercentOfMap = 0.025f;
        [SerializeField, Range(0, RegionSkeletonSettings.MaxPlayerRegionCount)] private int _minRegionsTouchingNeutralZone = 4;

        [Header("Role Catalog")]
        [SerializeField] private bool _useTycoonEightRegionDefaultRoles = true;
        [SerializeField] private RegionRoleDefinitionAuthoring[] _customRoleDefinitions = Array.Empty<RegionRoleDefinitionAuthoring>();

        public RegionSkeletonNeutralPolicy NeutralPolicy => _neutralPolicy;
        public int NeutralZoneCount => _neutralZoneCount;
        public float NeutralCoreRadiusPercentOfMinDimension => _neutralCoreRadiusPercentOfMinDimension;
        public RegionSkeletonRoleAssignmentPolicy RoleAssignmentPolicy => _roleAssignmentPolicy;
        public float BoundaryWarpAmplitudeTurns => _boundaryWarpAmplitudeTurns;
        public int MinUsefulRegionNeighbors => _minUsefulRegionNeighbors;
        public float MinRegionAreaPercentOfMap => _minRegionAreaPercentOfMap;
        public float MinNeutralAreaPercentOfMap => _minNeutralAreaPercentOfMap;
        public int MinRegionsTouchingNeutralZone => _minRegionsTouchingNeutralZone;
        public bool UseTycoonEightRegionDefaultRoles => _useTycoonEightRegionDefaultRoles;

        public RegionSkeletonSettings CompileSettings(MapGenerationRequest request)
        {
            request.Validate();
            ClampSerializedValues();

            RegionSkeletonSettings settings = new(
                request.Dimensions,
                request.Seed.Derive(StageSeedScope),
                request.PlayerCount,
                _neutralZoneCount,
                _neutralPolicy,
                _roleAssignmentPolicy,
                _neutralCoreRadiusPercentOfMinDimension,
                _boundaryWarpAmplitudeTurns,
                _minUsefulRegionNeighbors,
                _minRegionAreaPercentOfMap,
                _minNeutralAreaPercentOfMap,
                _minRegionsTouchingNeutralZone);

            settings.Validate();
            return settings;
        }

        public RegionRoleCatalog CreateRoleCatalog()
        {
            if (_useTycoonEightRegionDefaultRoles)
                return RegionRoleCatalog.CreateTycoonEightRegionDefault();

            if (_customRoleDefinitions == null || _customRoleDefinitions.Length == 0)
                throw new InvalidOperationException("Region skeleton profile has custom roles enabled but contains no role definitions.");

            RegionRoleDefinition[] definitions = new RegionRoleDefinition[_customRoleDefinitions.Length];

            for (int i = 0; i < _customRoleDefinitions.Length; i++)
            {
                RegionRoleDefinitionAuthoring? authoring = _customRoleDefinitions[i];

                if (authoring == null)
                    throw new InvalidOperationException("Region skeleton profile contains a null role definition at index " + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");

                definitions[i] = authoring.ToRuntimeDefinition();
            }

            return new RegionRoleCatalog(definitions);
        }

        public void ResetToTycoonEightRegionDefaults()
        {
            _neutralPolicy = RegionSkeletonNeutralPolicy.CentralCore;
            _neutralZoneCount = 1;
            _neutralCoreRadiusPercentOfMinDimension = 0.16f;
            _roleAssignmentPolicy = RegionSkeletonRoleAssignmentPolicy.DeterministicShuffle;
            _boundaryWarpAmplitudeTurns = 0f;
            _minUsefulRegionNeighbors = 2;
            _minRegionAreaPercentOfMap = 0.06f;
            _minNeutralAreaPercentOfMap = 0.025f;
            _minRegionsTouchingNeutralZone = 4;
            _useTycoonEightRegionDefaultRoles = true;
            _customRoleDefinitions = CreateTycoonDefaultAuthoringRoles();
        }

        public static RegionSkeletonProfile CreateTransientTycoonDefault()
        {
            RegionSkeletonProfile profile = CreateInstance<RegionSkeletonProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.ResetToTycoonEightRegionDefaults();
            return profile;
        }

        public void ApplyPreviewOverrides(
            RegionSkeletonRoleAssignmentPolicy roleAssignmentPolicy,
            float neutralCoreRadiusPercentOfMinDimension,
            float boundaryWarpAmplitudeTurns,
            int minUsefulRegionNeighbors,
            float minRegionAreaPercentOfMap,
            float minNeutralAreaPercentOfMap,
            int minRegionsTouchingNeutralZone)
        {
            _roleAssignmentPolicy = roleAssignmentPolicy;
            _neutralPolicy = RegionSkeletonNeutralPolicy.CentralCore;
            _neutralZoneCount = 1;
            _neutralCoreRadiusPercentOfMinDimension = neutralCoreRadiusPercentOfMinDimension;
            _boundaryWarpAmplitudeTurns = boundaryWarpAmplitudeTurns;
            _minUsefulRegionNeighbors = minUsefulRegionNeighbors;
            _minRegionAreaPercentOfMap = minRegionAreaPercentOfMap;
            _minNeutralAreaPercentOfMap = minNeutralAreaPercentOfMap;
            _minRegionsTouchingNeutralZone = minRegionsTouchingNeutralZone;
            ClampSerializedValues();
        }

        private void Reset()
        {
            ResetToTycoonEightRegionDefaults();
        }

        private void OnValidate()
        {
            ClampSerializedValues();
        }

        private void ClampSerializedValues()
        {
            _neutralZoneCount = Mathf.Clamp(_neutralZoneCount, 0, RegionSkeletonSettings.MaxNeutralZoneCount);
            _neutralCoreRadiusPercentOfMinDimension = Mathf.Clamp(_neutralCoreRadiusPercentOfMinDimension, 0f, 0.35f);
            _boundaryWarpAmplitudeTurns = Mathf.Clamp(_boundaryWarpAmplitudeTurns, 0f, 0.08f);
            _minUsefulRegionNeighbors = Mathf.Clamp(_minUsefulRegionNeighbors, 0, RegionSkeletonSettings.MaxPlayerRegionCount - 1);
            _minRegionAreaPercentOfMap = Mathf.Clamp(_minRegionAreaPercentOfMap, 0.01f, 0.20f);
            _minNeutralAreaPercentOfMap = Mathf.Clamp(_minNeutralAreaPercentOfMap, 0f, 0.25f);
            _minRegionsTouchingNeutralZone = Mathf.Clamp(_minRegionsTouchingNeutralZone, 0, RegionSkeletonSettings.MaxPlayerRegionCount);

            if (_neutralPolicy == RegionSkeletonNeutralPolicy.None)
            {
                _neutralZoneCount = 0;
                _minNeutralAreaPercentOfMap = 0f;
                _minRegionsTouchingNeutralZone = 0;
            }
            else if (_neutralPolicy == RegionSkeletonNeutralPolicy.CentralCore)
            {
                _neutralZoneCount = 1;
            }
        }

        private static RegionRoleDefinitionAuthoring[] CreateTycoonDefaultAuthoringRoles()
        {
            return new[]
            {
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.FertileBreadbasket")),
                    "Tycoon.RegionRole.FertileBreadbasket"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.MiningUplands")),
                    "Tycoon.RegionRole.MiningUplands"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.Timberland")),
                    "Tycoon.RegionRole.Timberland"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.PortTradeCoast")),
                    "Tycoon.RegionRole.PortTradeCoast"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.RiverlandGrowth")),
                    "Tycoon.RegionRole.RiverlandGrowth"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.IndustrialPlateau")),
                    "Tycoon.RegionRole.IndustrialPlateau"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.DryExtractorBasin")),
                    "Tycoon.RegionRole.DryExtractorBasin"),
                RegionRoleDefinitionAuthoring.FromRuntimeDefinition(
                    RegionRoleCatalog.CreateTycoonEightRegionDefault().GetRequired(RegionRoleId.FromStableName("Tycoon.RegionRole.BalancedFrontier")),
                    "Tycoon.RegionRole.BalancedFrontier")
            };
        }
    }
}
