#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.MacroLandform
{
    public sealed class MacroLandformGeneratorTests
    {
        [Test]
        public void DefaultMacroLandform_GeneratesExactTargetLandCount()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 12345u);

            using RegionSkeletonResult skeleton = new RegionSkeletonGenerator().GenerateDefault(request, Allocator.Persistent);
            using MacroLandformResult landform = new MacroLandformGenerator().Generate(
                MacroLandformSettings.CreateDefault(request),
                skeleton,
                RegionRoleCatalog.CreateTycoonEightRegionDefault(),
                Allocator.Persistent);

            Assert.That(landform.LandSampleCount, Is.EqualTo(landform.Settings.TargetLandSampleCount));
        }

        [Test]
        public void DefaultMacroLandform_HasHardWaterBorder()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 22222u);

            using RegionSkeletonResult skeleton = new RegionSkeletonGenerator().GenerateDefault(request, Allocator.Persistent);
            using MacroLandformResult landform = new MacroLandformGenerator().Generate(
                MacroLandformSettings.CreateDefault(request),
                skeleton,
                RegionRoleCatalog.CreateTycoonEightRegionDefault(),
                Allocator.Persistent);

            int border = landform.Settings.HardWaterBorderThickness;

            for (int y = 0; y < landform.Dimensions.Height; y++)
            {
                for (int x = 0; x < landform.Dimensions.Width; x++)
                {
                    bool isBorder = x < border
                        || y < border
                        || x >= landform.Dimensions.Width - border
                        || y >= landform.Dimensions.Height - border;

                    if (!isBorder)
                        continue;

                    int index = y * landform.Dimensions.Width + x;
                    Assert.That(landform.LandMaskField.Samples[index], Is.EqualTo(0));
                    Assert.That(landform.OceanMaskField.Samples[index], Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void DefaultMacroLandform_PassesValidation()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 33333u);

            using RegionSkeletonResult skeleton = new RegionSkeletonGenerator().GenerateDefault(request, Allocator.Persistent);
            using MacroLandformResult landform = new MacroLandformGenerator().Generate(
                MacroLandformSettings.CreateDefault(request),
                skeleton,
                RegionRoleCatalog.CreateTycoonEightRegionDefault(),
                Allocator.Persistent);

            MapValidationReport report = new MacroLandformValidator().Validate(landform);

            Assert.That(report.Passed, Is.True);
            Assert.That(report.FatalIssueCount, Is.EqualTo(0));
            Assert.That(report.ErrorIssueCount, Is.EqualTo(0));
        }

        [Test]
        public void SameSeed_ProducesSameMacroLandformHash()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 44444u);
            RegionRoleCatalog catalog = RegionRoleCatalog.CreateTycoonEightRegionDefault();
            MacroLandformSettings settings = MacroLandformSettings.CreateDefault(request);

            StableHash128 firstHash;
            StableHash128 secondHash;

            using (RegionSkeletonResult skeleton = new RegionSkeletonGenerator().GenerateDefault(request, Allocator.Persistent))
            using (MacroLandformResult landform = new MacroLandformGenerator().Generate(settings, skeleton, catalog, Allocator.Persistent))
            {
                firstHash = landform.ArtifactHash;
            }

            using (RegionSkeletonResult skeleton = new RegionSkeletonGenerator().GenerateDefault(request, Allocator.Persistent))
            using (MacroLandformResult landform = new MacroLandformGenerator().Generate(settings, skeleton, catalog, Allocator.Persistent))
            {
                secondHash = landform.ArtifactHash;
            }

            Assert.That(firstHash, Is.EqualTo(secondHash));
        }

        [Test]
        public void DifferentSeeds_UsuallyProduceDifferentMacroLandformHash()
        {
            RegionRoleCatalog catalog = RegionRoleCatalog.CreateTycoonEightRegionDefault();

            MapGenerationRequest firstRequest = MapGenerationRequest.CreateDefaultPreview(128, 128, 55555u);
            MapGenerationRequest secondRequest = MapGenerationRequest.CreateDefaultPreview(128, 128, 66666u);

            using RegionSkeletonResult firstSkeleton = new RegionSkeletonGenerator().GenerateDefault(firstRequest, Allocator.Persistent);
            using RegionSkeletonResult secondSkeleton = new RegionSkeletonGenerator().GenerateDefault(secondRequest, Allocator.Persistent);

            using MacroLandformResult first = new MacroLandformGenerator().Generate(
                MacroLandformSettings.CreateDefault(firstRequest),
                firstSkeleton,
                catalog,
                Allocator.Persistent);

            using MacroLandformResult second = new MacroLandformGenerator().Generate(
                MacroLandformSettings.CreateDefault(secondRequest),
                secondSkeleton,
                catalog,
                Allocator.Persistent);

            Assert.That(first.ArtifactHash, Is.Not.EqualTo(second.ArtifactHash));
        }
    }
}
