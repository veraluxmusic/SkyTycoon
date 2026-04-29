#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm;
using Lokrain.SkyTycoon.MapGeneration.Preview.OpenSimplex2SFbm;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.Preview.OpenSimplex2SFbm
{
    internal static class HeightFieldPreviewTextureBuilder
    {
        private const string TextureName =
            "Lokrain SkyTycoon MapGeneration OpenSimplex2S fBM Height Field Preview";

        public static bool TryBuild(
            HeightFieldProfile profile,
            int width,
            int height,
            Vector2 noiseSpaceOrigin,
            Vector2 noiseSpaceSize,
            HeightFieldValueRange outputRange,
            HeightFieldPreviewTextureMode textureMode,
            [NotNullWhen(true)] out Texture2D? texture,
            out string? errorMessage)
        {
            texture = null;
            errorMessage = null;

            if (profile == null)
            {
                errorMessage = "Height field profile is missing.";
                return false;
            }

            width = math.max(1, width);
            height = math.max(1, height);

            noiseSpaceSize.x = math.max(0.0001f, noiseSpaceSize.x);
            noiseSpaceSize.y = math.max(0.0001f, noiseSpaceSize.y);

            try
            {
                FbmSettings settings = profile.CreateRuntimeSettings();

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
                    int expectedLength = width * height;

                    if (values.Length != expectedLength)
                    {
                        errorMessage =
                            $"Height field sample count mismatch. Expected {expectedLength}, got {values.Length}.";

                        return false;
                    }

                    texture = CreateTexture(width, height, textureMode);
                    WriteTexture(texture, values, outputRange, textureMode);
                    return true;
                }
                finally
                {
                    if (values.IsCreated)
                        values.Dispose();
                }
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private static Texture2D CreateTexture(
            int width,
            int height,
            HeightFieldPreviewTextureMode textureMode)
        {
            TextureFormat format = textureMode == HeightFieldPreviewTextureMode.RFloat
                ? TextureFormat.RFloat
                : TextureFormat.RGBA32;

            return new Texture2D(width, height, format, false, true)
            {
                name = TextureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void WriteTexture(
            Texture2D texture,
            NativeArray<float> values,
            HeightFieldValueRange outputRange,
            HeightFieldPreviewTextureMode textureMode)
        {
            if (textureMode == HeightFieldPreviewTextureMode.RFloat)
            {
                texture.SetPixelData(values, 0);
                texture.Apply(false, false);
                return;
            }

            NativeArray<Color32> pixels =
                new(values.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    float normalized = NormalizeForPreview(values[i], outputRange);
                    byte value = (byte)math.round(normalized * 255f);

                    pixels[i] = new Color32(value, value, value, 255);
                }

                texture.SetPixelData(pixels, 0);
                texture.Apply(false, false);
            }
            finally
            {
                if (pixels.IsCreated)
                    pixels.Dispose();
            }
        }

        private static float NormalizeForPreview(float value, HeightFieldValueRange outputRange)
        {
            return outputRange == HeightFieldValueRange.UnsignedZeroToOne
                ? math.saturate(value)
                : math.saturate(value * 0.5f + 0.5f);
        }
    }
}