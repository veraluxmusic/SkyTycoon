#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm
{
    /// <summary>
    /// Authoring asset for the F001 OpenSimplex2S fBM height-field source stage.
    /// This asset produces only a source height field. It does not perform domain warp, landmass falloff, land/water classification,
    /// percentile cutting, connectivity filtering or area compensation.
    /// </summary>
    [CreateAssetMenu(
        fileName = "OpenSimplex2SFbmHeightFieldProfile",
        menuName = "Sky Tycoon/Map Generation/Height Fields/OpenSimplex2S fBM Profile")]
    public sealed class OpenSimplex2SFbmHeightFieldProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private long seed = 1L;

        [Header("Noise Shape")]
        [SerializeField, Range(OpenSimplex2SFbmSettings.MinOctaveCount, OpenSimplex2SFbmSettings.MaxOctaveCount)]
        private int octaveCount = OpenSimplex2SFbmSettings.DefaultOctaveCount;

        [SerializeField, Min(0.0001f)]
        private float baseWavelength = OpenSimplex2SFbmSettings.DefaultBaseWavelength;

        [SerializeField, Range(0.0001f, 1f)]
        private float persistence = OpenSimplex2SFbmSettings.DefaultPersistence;

        [SerializeField, Min(1f)]
        private float lacunarity = OpenSimplex2SFbmSettings.DefaultLacunarity;

        [SerializeField]
        private Vector2 globalNoiseOffset = new(10000f, 10000f);

        [SerializeField]
        private OpenSimplex2SFbmOrientation orientation = OpenSimplex2SFbmOrientation.Standard;

        [SerializeField]
        private bool normalizeByAmplitude = true;

        [Header("Execution")]
        [SerializeField, Range(OpenSimplex2SFbmSettings.MinSuggestedJobTileSize, OpenSimplex2SFbmSettings.MaxSuggestedJobTileSize)]
        private int suggestedJobTileSize = OpenSimplex2SFbmSettings.DefaultSuggestedJobTileSize;

        [Header("Saved Preview")]
        [SerializeField]
        private HeightFieldPreviewSettings preview = new();

        public long Seed => seed;
        public int OctaveCount => octaveCount;
        public float BaseWavelength => baseWavelength;
        public float Persistence => persistence;
        public float Lacunarity => lacunarity;
        public Vector2 GlobalNoiseOffset => globalNoiseOffset;
        public OpenSimplex2SFbmOrientation Orientation => orientation;
        public bool NormalizeByAmplitude => normalizeByAmplitude;
        public int SuggestedJobTileSize => suggestedJobTileSize;
        public HeightFieldPreviewSettings Preview => preview;

        public OpenSimplex2SFbmSettings CreateRuntimeSettings()
        {
            return new OpenSimplex2SFbmSettings(
                seed: seed,
                octaveCount: octaveCount,
                baseWavelength: baseWavelength,
                persistence: persistence,
                lacunarity: lacunarity,
                globalNoiseOffset: new float2(globalNoiseOffset.x, globalNoiseOffset.y),
                orientation: orientation,
                normalizeByAmplitude: normalizeByAmplitude,
                suggestedJobTileSize: suggestedJobTileSize);
        }

        private void OnValidate()
        {
            octaveCount = math.clamp(octaveCount, OpenSimplex2SFbmSettings.MinOctaveCount, OpenSimplex2SFbmSettings.MaxOctaveCount);
            baseWavelength = math.max(0.0001f, baseWavelength);
            persistence = math.clamp(persistence, 0.0001f, 1f);
            lacunarity = math.max(1f, lacunarity);
            suggestedJobTileSize = math.clamp(
                suggestedJobTileSize,
                OpenSimplex2SFbmSettings.MinSuggestedJobTileSize,
                OpenSimplex2SFbmSettings.MaxSuggestedJobTileSize);

            if (preview == null)
                preview = new HeightFieldPreviewSettings();

            preview.Validate();
        }
    }
}
