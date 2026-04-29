#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Preview.OpenSimplex2SFbm
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class HeightFieldPreview : MonoBehaviour
    {
        private static readonly int PreviewTexturePropertyId = Shader.PropertyToID("_PreviewTexture");
        private static readonly int MainTexturePropertyId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapTexturePropertyId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorMapTexturePropertyId = Shader.PropertyToID("_BaseColorMap");

        private const string PreviewTextureName = "Lokrain SkyTycoon MapGeneration OpenSimplex2S fBM Height Field Preview";

        [SerializeField] private HeightFieldProfile? profile;
        [SerializeField, Min(1)] private int width = 128;
        [SerializeField, Min(1)] private int height = 128;
        [SerializeField] private Vector2 noiseSpaceOrigin = Vector2.zero;
        [SerializeField] private Vector2 noiseSpaceSize = new(4f, 4f);
        [SerializeField] private HeightFieldValueRange outputRange = HeightFieldValueRange.UnsignedZeroToOne;
        [SerializeField] private HeightFieldPreviewTextureMode textureMode = HeightFieldPreviewTextureMode.Rgba32Grayscale;
        [SerializeField] private Renderer? targetRenderer;
        [SerializeField] private bool regenerateOnEnable = true;

        private Texture2D? texture;
        private MaterialPropertyBlock? propertyBlock;

        public Texture2D? Texture => texture;

        public bool HasTexture => texture != null;

        public bool TryGetTexture([NotNullWhen(true)] out Texture2D? result)
        {
            result = texture;
            return result != null;
        }

        public void Initialize(HeightFieldProfile? profileAsset, Renderer? renderer)
        {
            profile = profileAsset;
            targetRenderer = renderer;

            Regenerate();
        }

        private void OnEnable()
        {
            if (regenerateOnEnable)
                Regenerate();
        }

        private void OnDisable()
        {
            ClearTextureFromRenderer();
            DestroyTexture();
        }

        private void OnValidate()
        {
            width = math.max(1, width);
            height = math.max(1, height);

            noiseSpaceSize.x = math.max(0.0001f, noiseSpaceSize.x);
            noiseSpaceSize.y = math.max(0.0001f, noiseSpaceSize.y);
        }

        [ContextMenu("Regenerate Height Field")]
        public void Regenerate()
        {
            if (width <= 0 || height <= 0)
                return;

            FbmSettings settings = profile != null
                ? profile.CreateRuntimeSettings()
                : FbmSettings.CreateDefault();

            HeightFieldRequest request = new(
                dimensions: new HeightFieldDimensions(width, height),
                noiseSpaceOrigin: new float2(noiseSpaceOrigin.x, noiseSpaceOrigin.y),
                noiseSpaceSize: new float2(noiseSpaceSize.x, noiseSpaceSize.y),
                outputRange: outputRange,
                jobTileSize: 0);

            using Generator generator = new(settings, Allocator.TempJob);
            NativeArray<float> values = generator.Generate(request, Allocator.TempJob);

            try
            {
                ApplyTexture(values);
            }
            finally
            {
                if (values.IsCreated)
                    values.Dispose();
            }
        }

        private void ApplyTexture(NativeArray<float> values)
        {
            int expectedLength = width * height;

            if (values.Length != expectedLength)
            {
                throw new InvalidOperationException(
                    $"Height field sample count mismatch. Expected {expectedLength}, got {values.Length}.");
            }

            TextureFormat format = textureMode == HeightFieldPreviewTextureMode.RFloat
                ? TextureFormat.RFloat
                : TextureFormat.RGBA32;

            if (texture == null || texture.width != width || texture.height != height || texture.format != format)
            {
                DestroyTexture();

                texture = new Texture2D(width, height, format, false, true)
                {
                    name = PreviewTextureName,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            if (textureMode == HeightFieldPreviewTextureMode.RFloat)
                ApplyRFloatTexture(values);
            else
                ApplyRgba32GrayscaleTexture(values);

            texture.Apply(false, false);
            ApplyTextureToRenderer();
        }

        private void ApplyRFloatTexture(NativeArray<float> values)
        {
            texture!.SetPixelData(values, 0);
        }

        private void ApplyRgba32GrayscaleTexture(NativeArray<float> values)
        {
            NativeArray<Color32> pixels = new(values.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    byte value = (byte)math.round(NormalizeForPreview(values[i]) * 255f);
                    pixels[i] = new Color32(value, value, value, 255);
                }

                texture!.SetPixelData(pixels, 0);
            }
            finally
            {
                if (pixels.IsCreated)
                    pixels.Dispose();
            }
        }

        private float NormalizeForPreview(float value)
        {
            return outputRange == HeightFieldValueRange.UnsignedZeroToOne
                ? math.saturate(value)
                : math.saturate(value * 0.5f + 0.5f);
        }

        private void ApplyTextureToRenderer()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null || texture == null)
                return;

            propertyBlock ??= new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetTexture(PreviewTexturePropertyId, texture);
            propertyBlock.SetTexture(MainTexturePropertyId, texture);
            propertyBlock.SetTexture(BaseMapTexturePropertyId, texture);
            propertyBlock.SetTexture(BaseColorMapTexturePropertyId, texture);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ClearTextureFromRenderer()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
                return;

            propertyBlock ??= new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetTexture(PreviewTexturePropertyId, null);
            propertyBlock.SetTexture(MainTexturePropertyId, null);
            propertyBlock.SetTexture(BaseMapTexturePropertyId, null);
            propertyBlock.SetTexture(BaseColorMapTexturePropertyId, null);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void DestroyTexture()
        {
            if (texture == null)
                return;

            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);

            texture = null;
        }
    }
}