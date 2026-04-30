#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Execution;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.Tests.MacroLandform
{
    public sealed class MacroLandformPreviewTextureBuilderTests
    {
        [Test]
        public void BuildPixelBuffer_ReturnsOnePixelPerSample_ForEveryLayer()
        {
            using MapGenerationExecutionResult result = GenerateResult(64, 64, 111u);

            foreach (MacroLandformPreviewLayer layer in System.Enum.GetValues(typeof(MacroLandformPreviewLayer)))
            {
                Color32[] pixels = MacroLandformPreviewTextureBuilder.BuildPixelBuffer(
                    result.RegionSkeletonResult,
                    result.MacroLandformResult,
                    layer,
                    drawRegionBoundaries: true,
                    drawCoastline: true);

                Assert.That(pixels, Is.Not.Null);
                Assert.That(pixels.Length, Is.EqualTo(result.Request.Dimensions.SampleCount));
            }
        }

        [Test]
        public void CreateTexture_CreatesExpectedTexture()
        {
            using MapGenerationExecutionResult result = GenerateResult(48, 40, 222u);

            Texture2D texture = MacroLandformPreviewTextureBuilder.CreateTexture(
                result.RegionSkeletonResult,
                result.MacroLandformResult,
                MacroLandformPreviewLayer.Composite,
                drawRegionBoundaries: true,
                drawCoastline: true);

            try
            {
                Assert.That(texture.width, Is.EqualTo(48));
                Assert.That(texture.height, Is.EqualTo(40));
                Assert.That(texture.format, Is.EqualTo(TextureFormat.RGBA32));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void BuildPixelBuffer_Throws_WhenStageDimensionsDoNotMatch()
        {
            using MapGenerationExecutionResult first = GenerateResult(64, 64, 333u);
            using MapGenerationExecutionResult second = GenerateResult(80, 64, 334u);

            Assert.That(
                () => MacroLandformPreviewTextureBuilder.BuildPixelBuffer(
                    first.RegionSkeletonResult,
                    second.MacroLandformResult,
                    MacroLandformPreviewLayer.Composite,
                    drawRegionBoundaries: true,
                    drawCoastline: true),
                Throws.ArgumentException);
        }

        private static MapGenerationExecutionResult GenerateResult(int width, int height, uint seed)
        {
            RegionSkeletonProfile regionProfile = RegionSkeletonProfile.CreateTransientTycoonDefault();
            MacroLandformProfile landformProfile = MacroLandformProfile.CreateTransientDefault();

            try
            {
                MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(width, height, seed);
                CompiledMapGenerationPlan plan = new CompiledMapGenerationPlan(
                    request,
                    new CompiledRegionSkeletonStage(
                        regionProfile.CompileSettings(request),
                        regionProfile.CreateRoleCatalog()),
                    new CompiledMacroLandformStage(
                        landformProfile.CompileSettings(request)));

                MapGenerationPipelineRunner runner = new MapGenerationPipelineRunner();
                return runner.Run(plan, Allocator.Persistent);
            }
            finally
            {
                Object.DestroyImmediate(regionProfile);
                Object.DestroyImmediate(landformProfile);
            }
        }
    }
}
