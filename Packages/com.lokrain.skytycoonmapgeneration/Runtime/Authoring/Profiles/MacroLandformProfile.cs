#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles
{
    /// <summary>
    /// Designer-facing authoring profile for Stage 1: Macro Landform.
    /// Compile this asset into MacroLandformSettings before runtime generation.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MacroLandformProfile",
        menuName = "Sky Tycoon/Map Generation/Macro Landform Profile")]
    public sealed class MacroLandformProfile : ScriptableObject
    {
        [Header("Continent Contract")]
        [SerializeField, Range(MacroLandformSettings.MinTargetLandPercent, MacroLandformSettings.MaxTargetLandPercent)]
        private float _targetLandPercent = 0.70f;

        [SerializeField, Range(MacroLandformSettings.MinHardWaterBorderThickness, MacroLandformSettings.MaxHardWaterBorderThickness)]
        private int _hardWaterBorderThickness = 1;

        [SerializeField, Range(0.50f, 8.0f)]
        private float _continentFalloffExponent = 2.35f;

        [Header("Base Noise")]
        [SerializeField, Range(MacroLandformSettings.MinFbmOctaves, MacroLandformSettings.MaxFbmOctaves)]
        private int _fbmOctaves = 4;

        [SerializeField, Min(2.0f)]
        private float _fbmBaseWavelengthTiles = 96f;

        [SerializeField, Range(0.01f, 0.99f)]
        private float _fbmPersistence = 0.52f;

        [SerializeField, Range(1.01f, 4.0f)]
        private float _fbmLacunarity = 2.0f;

        [SerializeField, Range(0f, 1f)]
        private float _fbmAmplitude = 0.22f;

        [Header("Domain Warp")]
        [SerializeField, Min(0f)]
        private float _domainWarpAmplitudeTiles = 6f;

        [SerializeField, Min(2f)]
        private float _domainWarpWavelengthTiles = 64f;

        [Header("Role Height Response")]
        [SerializeField, Min(0f)]
        private float _mountainHeightContribution = 0.36f;

        [SerializeField, Min(0f)]
        private float _basinHeightContribution = 0.22f;

        [SerializeField, Min(0f)]
        private float _plainHeightContribution = 0.10f;

        [Header("Buildability")]
        [SerializeField, Range(0f, 1f)]
        private float _mountainBuildabilityPenalty = 0.45f;

        [SerializeField, Range(0.001f, 2f)]
        private float _fullyUnbuildableSlope = 0.18f;

        public float TargetLandPercent => _targetLandPercent;
        public int HardWaterBorderThickness => _hardWaterBorderThickness;
        public float ContinentFalloffExponent => _continentFalloffExponent;
        public int FbmOctaves => _fbmOctaves;
        public float FbmBaseWavelengthTiles => _fbmBaseWavelengthTiles;
        public float FbmPersistence => _fbmPersistence;
        public float FbmLacunarity => _fbmLacunarity;
        public float FbmAmplitude => _fbmAmplitude;
        public float DomainWarpAmplitudeTiles => _domainWarpAmplitudeTiles;
        public float DomainWarpWavelengthTiles => _domainWarpWavelengthTiles;
        public float MountainHeightContribution => _mountainHeightContribution;
        public float BasinHeightContribution => _basinHeightContribution;
        public float PlainHeightContribution => _plainHeightContribution;
        public float MountainBuildabilityPenalty => _mountainBuildabilityPenalty;
        public float FullyUnbuildableSlope => _fullyUnbuildableSlope;

        public MacroLandformSettings CompileSettings(MapGenerationRequest request)
        {
            request.Validate();
            ClampSerializedValues(request);

            MacroLandformSettings defaults = MacroLandformSettings.CreateDefault(request);

            float fbmBaseWavelength = _fbmBaseWavelengthTiles > 0f
                ? _fbmBaseWavelengthTiles
                : defaults.FbmBaseWavelengthTiles;

            float domainWarpWavelength = _domainWarpWavelengthTiles > 0f
                ? _domainWarpWavelengthTiles
                : defaults.DomainWarpWavelengthTiles;

            MacroLandformSettings settings = new(
                request.Dimensions,
                request.Seed.Derive("Stage.MacroLandform.SingleContinent"),
                _targetLandPercent,
                _hardWaterBorderThickness,
                _continentFalloffExponent,
                _fbmOctaves,
                fbmBaseWavelength,
                _fbmPersistence,
                _fbmLacunarity,
                _fbmAmplitude,
                _domainWarpAmplitudeTiles,
                domainWarpWavelength,
                _mountainHeightContribution,
                _basinHeightContribution,
                _plainHeightContribution,
                _mountainBuildabilityPenalty,
                _fullyUnbuildableSlope);

            settings.Validate();
            return settings;
        }

        public void ResetToDefaults()
        {
            _targetLandPercent = 0.70f;
            _hardWaterBorderThickness = 1;
            _continentFalloffExponent = 2.35f;
            _fbmOctaves = 4;
            _fbmBaseWavelengthTiles = 96f;
            _fbmPersistence = 0.52f;
            _fbmLacunarity = 2.0f;
            _fbmAmplitude = 0.22f;
            _domainWarpAmplitudeTiles = 6f;
            _domainWarpWavelengthTiles = 64f;
            _mountainHeightContribution = 0.36f;
            _basinHeightContribution = 0.22f;
            _plainHeightContribution = 0.10f;
            _mountainBuildabilityPenalty = 0.45f;
            _fullyUnbuildableSlope = 0.18f;
        }

        public static MacroLandformProfile CreateTransientDefault()
        {
            MacroLandformProfile profile = CreateInstance<MacroLandformProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            profile.ResetToDefaults();
            return profile;
        }

        private void Reset()
        {
            ResetToDefaults();
        }

        private void OnValidate()
        {
            ClampSerializedValues(null);
        }

        private void ClampSerializedValues(MapGenerationRequest? request)
        {
            _targetLandPercent = Mathf.Clamp(_targetLandPercent, MacroLandformSettings.MinTargetLandPercent, MacroLandformSettings.MaxTargetLandPercent);
            _hardWaterBorderThickness = Mathf.Clamp(_hardWaterBorderThickness, MacroLandformSettings.MinHardWaterBorderThickness, MacroLandformSettings.MaxHardWaterBorderThickness);
            _continentFalloffExponent = Mathf.Clamp(_continentFalloffExponent, 0.50f, 8.0f);
            _fbmOctaves = Mathf.Clamp(_fbmOctaves, MacroLandformSettings.MinFbmOctaves, MacroLandformSettings.MaxFbmOctaves);
            _fbmBaseWavelengthTiles = Mathf.Max(2.0f, _fbmBaseWavelengthTiles);
            _fbmPersistence = Mathf.Clamp(_fbmPersistence, 0.01f, 0.99f);
            _fbmLacunarity = Mathf.Clamp(_fbmLacunarity, 1.01f, 4.0f);
            _fbmAmplitude = Mathf.Clamp01(_fbmAmplitude);
            _domainWarpAmplitudeTiles = Mathf.Max(0f, _domainWarpAmplitudeTiles);
            _domainWarpWavelengthTiles = Mathf.Max(2f, _domainWarpWavelengthTiles);
            _mountainHeightContribution = Mathf.Max(0f, _mountainHeightContribution);
            _basinHeightContribution = Mathf.Max(0f, _basinHeightContribution);
            _plainHeightContribution = Mathf.Max(0f, _plainHeightContribution);
            _mountainBuildabilityPenalty = Mathf.Clamp01(_mountainBuildabilityPenalty);
            _fullyUnbuildableSlope = Mathf.Clamp(_fullyUnbuildableSlope, 0.001f, 2f);

            if (request.HasValue)
            {
                int maxBorder = math.max(1, (math.min(request.Value.Dimensions.Width, request.Value.Dimensions.Height) - 1) / 2 - 1);
                _hardWaterBorderThickness = Mathf.Min(_hardWaterBorderThickness, maxBorder);
            }
        }
    }
}
