
using Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm
{
    [CreateAssetMenu(
        fileName = "HeightFieldProfile",
        menuName = "MapGeneration/OpenSimplex2S fBM Height Field Profile")]
    public sealed class HeightFieldProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private long seed = 1;

        [Header("Noise Shape")]
        [SerializeField, Range(FbmSettings.MinOctaveCount, FbmSettings.MaxOctaveCount)] private int octaveCount = FbmSettings.DefaultOctaveCount;
        [SerializeField, Min(0.0001f)] private float baseFrequency = 1f;
        [SerializeField, Range(0.01f, 1f)] private float persistence = 0.5f;
        [SerializeField, Min(1f)] private float lacunarity = 2f;
        [SerializeField] private Vector2 globalNoiseOffset = new Vector2(10000f, 10000f);
        [SerializeField] private Orientation orientation = Orientation.Standard;
        [SerializeField] private bool normalizeByAmplitude = true;

        [Header("Job Scheduling")]
        [SerializeField, Range(8, 128)] private int suggestedJobTileSize = FbmSettings.DefaultSuggestedJobTileSize;

        public FbmSettings CreateRuntimeSettings()
        {
            return new FbmSettings(
                seed: seed,
                octaveCount: octaveCount,
                baseFrequency: baseFrequency,
                persistence: persistence,
                lacunarity: lacunarity,
                globalNoiseOffset: new float2(globalNoiseOffset.x, globalNoiseOffset.y),
                orientation: orientation,
                normalizeByAmplitude: normalizeByAmplitude,
                suggestedJobTileSize: suggestedJobTileSize);
        }

        private void OnValidate()
        {
            octaveCount = math.clamp(octaveCount, FbmSettings.MinOctaveCount, FbmSettings.MaxOctaveCount);
            baseFrequency = math.max(0.0001f, baseFrequency);
            persistence = math.clamp(persistence, 0.01f, 1f);
            lacunarity = math.max(1f, lacunarity);
            suggestedJobTileSize = math.clamp(suggestedJobTileSize, 8, 128);
        }
    }
}
