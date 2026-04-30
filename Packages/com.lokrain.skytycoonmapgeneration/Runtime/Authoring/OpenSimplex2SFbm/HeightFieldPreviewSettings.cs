#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm
{
    /// <summary>
    /// Designer-facing, asset-saved preview configuration for one height-field profile.
    ///
    /// These settings affect only preview and diagnostics. They must not be treated as
    /// production map-generation parameters unless a future stage explicitly promotes them.
    /// </summary>
    [Serializable]
    public sealed class HeightFieldPreviewSettings
    {
        public const int MinPreviewResolution = 1;
        public const int MaxPreviewResolution = 4096;

        public const float MinNoiseSpaceSize = 0.0001f;

        public const float DefaultPercentileCandidateLandPercent = 70f;
        public const float MinPercentileCandidateLandPercent = 0f;
        public const float MaxPercentileCandidateLandPercent = 100f;

        [SerializeField, Min(MinPreviewResolution)]
        private int width = 128;

        [SerializeField, Min(MinPreviewResolution)]
        private int height = 128;

        [SerializeField]
        private Vector2 noiseSpaceOrigin = Vector2.zero;

        [SerializeField]
        private Vector2 noiseSpaceSize = new(128f, 128f);

        [SerializeField]
        private HeightFieldValueRange outputRange = HeightFieldValueRange.UnsignedZeroToOne;

        [SerializeField]
        private HeightFieldPreviewTextureMode textureMode = HeightFieldPreviewTextureMode.Rgba32Grayscale;

        [SerializeField]
        private HeightFieldPreviewDisplayMode displayMode = HeightFieldPreviewDisplayMode.RawHeightField;

        [SerializeField, Range(MinPercentileCandidateLandPercent, MaxPercentileCandidateLandPercent)]
        private float percentileCandidateLandPercent = DefaultPercentileCandidateLandPercent;

        [SerializeField]
        private GenerationDiagnosticsMode diagnosticsMode = GenerationDiagnosticsMode.Summary;

        [SerializeField]
        private bool regenerateAutomatically = true;

        [SerializeField, Range(0, 256)]
        private int jobTileSizeOverride;

        public int Width => width;
        public int Height => height;
        public Vector2 NoiseSpaceOrigin => noiseSpaceOrigin;
        public Vector2 NoiseSpaceSize => noiseSpaceSize;
        public HeightFieldValueRange OutputRange => outputRange;
        public HeightFieldPreviewTextureMode TextureMode => textureMode;
        public HeightFieldPreviewDisplayMode DisplayMode => displayMode;
        public float PercentileCandidateLandPercent => percentileCandidateLandPercent;
        public GenerationDiagnosticsMode DiagnosticsMode => diagnosticsMode;
        public bool RegenerateAutomatically => regenerateAutomatically;
        public int JobTileSizeOverride => jobTileSizeOverride;

        public HeightFieldRequest CreateRequest()
        {
            return new HeightFieldRequest(
                dimensions: new HeightFieldDimensions(width, height),
                noiseSpaceOrigin: new float2(noiseSpaceOrigin.x, noiseSpaceOrigin.y),
                noiseSpaceSize: new float2(noiseSpaceSize.x, noiseSpaceSize.y),
                outputRange: outputRange,
                jobTileSize: jobTileSizeOverride);
        }

        public void Validate()
        {
            width = math.clamp(width, MinPreviewResolution, MaxPreviewResolution);
            height = math.clamp(height, MinPreviewResolution, MaxPreviewResolution);

            noiseSpaceSize.x = math.max(MinNoiseSpaceSize, noiseSpaceSize.x);
            noiseSpaceSize.y = math.max(MinNoiseSpaceSize, noiseSpaceSize.y);

            percentileCandidateLandPercent = math.clamp(
                percentileCandidateLandPercent,
                MinPercentileCandidateLandPercent,
                MaxPercentileCandidateLandPercent);

            jobTileSizeOverride = math.clamp(jobTileSizeOverride, 0, 256);
        }
    }
}