#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.MacroLandform
{
    /// <summary>
    /// Converts Stage 1 macro-landform fields into editor preview textures.
    /// This is editor-only presentation code; runtime generation remains texture-free.
    /// </summary>
    public static class MacroLandformPreviewTextureBuilder
    {
        public static Texture2D CreateTexture(
            RegionSkeletonResult regionSkeleton,
            MacroLandformResult macroLandform,
            MacroLandformPreviewLayer layer,
            bool drawRegionBoundaries,
            bool drawCoastline)
        {
            if (regionSkeleton == null)
                throw new ArgumentNullException(nameof(regionSkeleton));

            if (macroLandform == null)
                throw new ArgumentNullException(nameof(macroLandform));

            Texture2D texture = new(
                macroLandform.Dimensions.Width,
                macroLandform.Dimensions.Height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true)
            {
                name = "Macro Landform Preview",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RebuildTexture(texture, regionSkeleton, macroLandform, layer, drawRegionBoundaries, drawCoastline);
            return texture;
        }

        public static void RebuildTexture(
            Texture2D texture,
            RegionSkeletonResult regionSkeleton,
            MacroLandformResult macroLandform,
            MacroLandformPreviewLayer layer,
            bool drawRegionBoundaries,
            bool drawCoastline)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            if (regionSkeleton == null)
                throw new ArgumentNullException(nameof(regionSkeleton));

            if (macroLandform == null)
                throw new ArgumentNullException(nameof(macroLandform));

            HeightFieldDimensions dimensions = macroLandform.Dimensions;

            if (regionSkeleton.Dimensions != dimensions)
                throw new ArgumentException("Region skeleton and macro landform dimensions must match.", nameof(regionSkeleton));

            if (texture.width != dimensions.Width || texture.height != dimensions.Height)
                throw new ArgumentException("Preview texture dimensions must match the macro landform dimensions.", nameof(texture));

            Color32[] pixels = BuildPixelBuffer(regionSkeleton, macroLandform, layer, drawRegionBoundaries, drawCoastline);
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }

        public static Color32[] BuildPixelBuffer(
            RegionSkeletonResult regionSkeleton,
            MacroLandformResult macroLandform,
            MacroLandformPreviewLayer layer,
            bool drawRegionBoundaries,
            bool drawCoastline)
        {
            if (regionSkeleton == null)
                throw new ArgumentNullException(nameof(regionSkeleton));

            if (macroLandform == null)
                throw new ArgumentNullException(nameof(macroLandform));

            HeightFieldDimensions dimensions = macroLandform.Dimensions;

            if (regionSkeleton.Dimensions != dimensions)
                throw new ArgumentException("Region skeleton and macro landform dimensions must match.", nameof(regionSkeleton));

            FieldRange coastDistanceRange = FieldRange.From(macroLandform.CoastDistanceField.Samples);
            FieldRange continentDistanceRange = FieldRange.From(macroLandform.ContinentDistanceField.Samples);

            Color32[] pixels = new Color32[dimensions.SampleCount];

            NativeArray<int> regionIds = regionSkeleton.RegionIdField.Samples;
            NativeArray<int> neutralZoneIds = regionSkeleton.NeutralZoneIdField.Samples;
            NativeArray<float> height = macroLandform.BaseHeightField.Samples;
            NativeArray<byte> landMask = macroLandform.LandMaskField.Samples;
            NativeArray<byte> oceanMask = macroLandform.OceanMaskField.Samples;
            NativeArray<float> coastDistance = macroLandform.CoastDistanceField.Samples;
            NativeArray<float> continentDistance = macroLandform.ContinentDistanceField.Samples;
            NativeArray<float> slope = macroLandform.SlopeField.Samples;
            NativeArray<float> buildability = macroLandform.BuildabilityField.Samples;
            NativeArray<float> mountain = macroLandform.MountainInfluenceField.Samples;
            NativeArray<float> basin = macroLandform.BasinInfluenceField.Samples;
            NativeArray<float> plain = macroLandform.PlainInfluenceField.Samples;

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int sourceIndex = y * dimensions.Width + x;
                    int textureIndex = (dimensions.Height - 1 - y) * dimensions.Width + x;

                    Color32 color = EvaluateSampleColor(
                        sourceIndex,
                        layer,
                        regionIds,
                        neutralZoneIds,
                        height,
                        landMask,
                        oceanMask,
                        coastDistance,
                        continentDistance,
                        slope,
                        buildability,
                        mountain,
                        basin,
                        plain,
                        coastDistanceRange,
                        continentDistanceRange);

                    if (drawCoastline && IsLandWaterBoundary(dimensions, landMask, x, y))
                        color = MacroLandformPreviewPalette.Overlay(color, MacroLandformPreviewPalette.Coast, 0.80f);

                    if (drawRegionBoundaries && IsRegionBoundary(dimensions, regionIds, neutralZoneIds, x, y))
                        color = MacroLandformPreviewPalette.Darken(color, 86);

                    pixels[textureIndex] = color;
                }
            }

            return pixels;
        }

        private static Color32 EvaluateSampleColor(
            int index,
            MacroLandformPreviewLayer layer,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds,
            NativeArray<float> height,
            NativeArray<byte> landMask,
            NativeArray<byte> oceanMask,
            NativeArray<float> coastDistance,
            NativeArray<float> continentDistance,
            NativeArray<float> slope,
            NativeArray<float> buildability,
            NativeArray<float> mountain,
            NativeArray<float> basin,
            NativeArray<float> plain,
            FieldRange coastDistanceRange,
            FieldRange continentDistanceRange)
        {
            switch (layer)
            {
                case MacroLandformPreviewLayer.Composite:
                    if (landMask[index] != 0)
                        return MacroLandformPreviewPalette.CompositeLand(height[index], slope[index], mountain[index]);

                    if (oceanMask[index] != 0)
                        return MacroLandformPreviewPalette.CompositeOcean(coastDistance[index]);

                    return MacroLandformPreviewPalette.Invalid;

                case MacroLandformPreviewLayer.BaseHeight:
                    return MacroLandformPreviewPalette.Grayscale(height[index]);

                case MacroLandformPreviewLayer.LandMask:
                    return landMask[index] != 0 ? MacroLandformPreviewPalette.MaskOn : MacroLandformPreviewPalette.MaskOff;

                case MacroLandformPreviewLayer.OceanMask:
                    return oceanMask[index] != 0 ? MacroLandformPreviewPalette.MaskOn : MacroLandformPreviewPalette.MaskOff;

                case MacroLandformPreviewLayer.CoastDistance:
                    return MacroLandformPreviewPalette.Heat(coastDistanceRange.Normalize(coastDistance[index]));

                case MacroLandformPreviewLayer.ContinentDistance:
                    return MacroLandformPreviewPalette.Heat(continentDistanceRange.Normalize(continentDistance[index]));

                case MacroLandformPreviewLayer.Slope:
                    return MacroLandformPreviewPalette.Heat(slope[index]);

                case MacroLandformPreviewLayer.Buildability:
                    return MacroLandformPreviewPalette.Buildability(buildability[index]);

                case MacroLandformPreviewLayer.MountainInfluence:
                    return MacroLandformPreviewPalette.Heat(mountain[index]);

                case MacroLandformPreviewLayer.BasinInfluence:
                    return MacroLandformPreviewPalette.Heat(basin[index]);

                case MacroLandformPreviewLayer.PlainInfluence:
                    return MacroLandformPreviewPalette.Heat(plain[index]);

                case MacroLandformPreviewLayer.RegionOverlay:
                    if (regionIds[index] > 0)
                        return RegionSkeletonPreviewPalette.GetRegionColor(regionIds[index]);

                    if (neutralZoneIds[index] > 0)
                        return RegionSkeletonPreviewPalette.GetNeutralZoneColor(neutralZoneIds[index]);

                    return MacroLandformPreviewPalette.Invalid;

                default:
                    throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unsupported macro landform preview layer.");
            }
        }

        private static bool IsLandWaterBoundary(HeightFieldDimensions dimensions, NativeArray<byte> landMask, int x, int y)
        {
            int index = y * dimensions.Width + x;
            byte center = landMask[index];

            if (x > 0 && landMask[index - 1] != center)
                return true;

            if (x + 1 < dimensions.Width && landMask[index + 1] != center)
                return true;

            if (y > 0 && landMask[index - dimensions.Width] != center)
                return true;

            if (y + 1 < dimensions.Height && landMask[index + dimensions.Width] != center)
                return true;

            return false;
        }

        private static bool IsRegionBoundary(
            HeightFieldDimensions dimensions,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds,
            int x,
            int y)
        {
            int index = y * dimensions.Width + x;
            int region = regionIds[index];
            int neutral = neutralZoneIds[index];

            if (x > 0 && IsDifferentOwner(region, neutral, regionIds[index - 1], neutralZoneIds[index - 1]))
                return true;

            if (x + 1 < dimensions.Width && IsDifferentOwner(region, neutral, regionIds[index + 1], neutralZoneIds[index + 1]))
                return true;

            if (y > 0 && IsDifferentOwner(region, neutral, regionIds[index - dimensions.Width], neutralZoneIds[index - dimensions.Width]))
                return true;

            if (y + 1 < dimensions.Height && IsDifferentOwner(region, neutral, regionIds[index + dimensions.Width], neutralZoneIds[index + dimensions.Width]))
                return true;

            return false;
        }

        private static bool IsDifferentOwner(int aRegion, int aNeutral, int bRegion, int bNeutral)
        {
            return aRegion != bRegion || aNeutral != bNeutral;
        }

        private readonly struct FieldRange
        {
            private readonly float _minimum;
            private readonly float _maximum;

            private FieldRange(float minimum, float maximum)
            {
                _minimum = minimum;
                _maximum = maximum;
            }

            public static FieldRange From(NativeArray<float> samples)
            {
                if (!samples.IsCreated || samples.Length == 0)
                    return new FieldRange(0.0f, 1.0f);

                float minimum = float.PositiveInfinity;
                float maximum = float.NegativeInfinity;

                for (int i = 0; i < samples.Length; i++)
                {
                    float value = samples[i];

                    if (float.IsNaN(value))
                        continue;

                    if (value < minimum)
                        minimum = value;

                    if (value > maximum)
                        maximum = value;
                }

                if (float.IsInfinity(minimum) || float.IsInfinity(maximum))
                    return new FieldRange(0.0f, 1.0f);

                return new FieldRange(minimum, maximum);
            }

            public float Normalize(float value)
            {
                if (float.IsNaN(value))
                    return 0.0f;

                float range = _maximum - _minimum;

                if (range <= 0.000001f)
                    return 0.0f;

                return Mathf.Clamp01((value - _minimum) / range);
            }
        }
    }
}
