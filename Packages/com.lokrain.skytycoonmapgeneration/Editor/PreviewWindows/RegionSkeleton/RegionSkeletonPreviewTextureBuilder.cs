#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton
{
    /// <summary>
    /// Converts region skeleton fields into editor preview textures.
    /// This is intentionally editor-only and allocation-explicit; runtime generation remains texture-free.
    /// </summary>
    public static class RegionSkeletonPreviewTextureBuilder
    {
        public static Texture2D CreateTexture(
            RegionSkeletonResult result,
            RegionSkeletonPreviewLayer layer,
            bool drawBoundaries)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            Texture2D texture = new(
                result.Dimensions.Width,
                result.Dimensions.Height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true)
            {
                name = "Region Skeleton Preview",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RebuildTexture(texture, result, layer, drawBoundaries);
            return texture;
        }

        public static void RebuildTexture(
            Texture2D texture,
            RegionSkeletonResult result,
            RegionSkeletonPreviewLayer layer,
            bool drawBoundaries)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            HeightFieldDimensions dimensions = result.Dimensions;

            if (texture.width != dimensions.Width || texture.height != dimensions.Height)
                throw new ArgumentException("Preview texture dimensions must match the region skeleton dimensions.", nameof(texture));

            Color32[] pixels = BuildPixelBuffer(result, layer, drawBoundaries);
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }

        public static Color32[] BuildPixelBuffer(
            RegionSkeletonResult result,
            RegionSkeletonPreviewLayer layer,
            bool drawBoundaries)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            HeightFieldDimensions dimensions = result.Dimensions;
            NativeArray<int> regionIds = result.RegionIdField.Samples;
            NativeArray<int> neutralZoneIds = result.NeutralZoneIdField.Samples;
            Color32[] pixels = new Color32[dimensions.SampleCount];

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int sourceIndex = y * dimensions.Width + x;
                    int textureIndex = (dimensions.Height - 1 - y) * dimensions.Width + x;

                    Color32 color = EvaluateSampleColor(regionIds[sourceIndex], neutralZoneIds[sourceIndex], layer);

                    if ((drawBoundaries || layer == RegionSkeletonPreviewLayer.Boundaries) && IsBoundary(dimensions, regionIds, neutralZoneIds, x, y))
                        color = layer == RegionSkeletonPreviewLayer.Boundaries ? RegionSkeletonPreviewPalette.Boundary : RegionSkeletonPreviewPalette.Darken(color, 92);

                    pixels[textureIndex] = color;
                }
            }

            return pixels;
        }

        private static Color32 EvaluateSampleColor(int regionId, int neutralZoneId, RegionSkeletonPreviewLayer layer)
        {
            bool hasRegion = regionId > 0;
            bool hasNeutral = neutralZoneId > 0;

            if (hasRegion == hasNeutral)
                return RegionSkeletonPreviewPalette.Invalid;

            return layer switch
            {
                RegionSkeletonPreviewLayer.Composite => (Color32)(hasRegion
                                        ? RegionSkeletonPreviewPalette.GetRegionColor(regionId)
                                        : RegionSkeletonPreviewPalette.GetNeutralZoneColor(neutralZoneId)),
                RegionSkeletonPreviewLayer.RegionId => (Color32)(hasRegion
                                        ? RegionSkeletonPreviewPalette.GetRegionColor(regionId)
                                        : RegionSkeletonPreviewPalette.Empty),
                RegionSkeletonPreviewLayer.NeutralZoneId => (Color32)(hasNeutral
                                        ? RegionSkeletonPreviewPalette.GetNeutralZoneColor(neutralZoneId)
                                        : RegionSkeletonPreviewPalette.Empty),
                RegionSkeletonPreviewLayer.Boundaries => (Color32)RegionSkeletonPreviewPalette.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unsupported region skeleton preview layer."),
            };

        }

        private static bool IsBoundary(
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
    }
}
