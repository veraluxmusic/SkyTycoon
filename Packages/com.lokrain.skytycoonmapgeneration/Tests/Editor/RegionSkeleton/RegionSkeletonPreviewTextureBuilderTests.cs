#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.Tests.RegionSkeleton
{
    public sealed class RegionSkeletonPreviewTextureBuilderTests
    {
        [Test]
        public void BuildPixelBufferReturnsOnePixelPerGeneratedSample()
        {
            RegionSkeletonGenerator generator = new RegionSkeletonGenerator();
            using RegionSkeletonResult result = generator.GenerateDefault(
                MapGenerationRequest.CreateDefaultPreview(64, 64, 12345u),
                Allocator.TempJob);

            Color32[] pixels = RegionSkeletonPreviewTextureBuilder.BuildPixelBuffer(
                result,
                RegionSkeletonPreviewLayer.Composite,
                drawBoundaries: true);

            Assert.AreEqual(result.Dimensions.SampleCount, pixels.Length);
        }

        [Test]
        public void CreateTextureMatchesSkeletonDimensions()
        {
            RegionSkeletonGenerator generator = new RegionSkeletonGenerator();
            using RegionSkeletonResult result = generator.GenerateDefault(
                MapGenerationRequest.CreateDefaultPreview(64, 48, 12345u),
                Allocator.TempJob);

            Texture2D? texture = null;

            try
            {
                texture = RegionSkeletonPreviewTextureBuilder.CreateTexture(
                    result,
                    RegionSkeletonPreviewLayer.Composite,
                    drawBoundaries: true);

                Assert.AreEqual(64, texture.width);
                Assert.AreEqual(48, texture.height);
                Assert.AreEqual(FilterMode.Point, texture.filterMode);
                Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            }
            finally
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void BoundaryLayerContainsBoundaryPixels()
        {
            RegionSkeletonGenerator generator = new RegionSkeletonGenerator();
            using RegionSkeletonResult result = generator.GenerateDefault(
                MapGenerationRequest.CreateDefaultPreview(64, 64, 98765u),
                Allocator.TempJob);

            Color32[] pixels = RegionSkeletonPreviewTextureBuilder.BuildPixelBuffer(
                result,
                RegionSkeletonPreviewLayer.Boundaries,
                drawBoundaries: true);

            int boundaryPixelCount = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r == RegionSkeletonPreviewPalette.Boundary.r
                    && pixels[i].g == RegionSkeletonPreviewPalette.Boundary.g
                    && pixels[i].b == RegionSkeletonPreviewPalette.Boundary.b)
                {
                    boundaryPixelCount++;
                }
            }

            Assert.Greater(boundaryPixelCount, 0);
        }
    }
}
