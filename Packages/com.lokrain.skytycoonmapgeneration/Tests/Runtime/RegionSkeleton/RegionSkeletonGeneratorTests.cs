#nullable enable

using NUnit.Framework;
using Unity.Collections;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.RegionSkeleton
{
    public sealed class RegionSkeletonGeneratorTests
    {
        [Test]
        public void DefaultGeneratorCreatesEightRegionsAndOneNeutralZone()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 12345u);
            RegionSkeletonGenerator generator = new();

            using RegionSkeletonResult result = generator.GenerateDefault(request, Allocator.TempJob);

            Assert.AreEqual(8, result.Settings.PlayerRegionCount);
            Assert.AreEqual(1, result.Settings.NeutralZoneCount);
            Assert.AreEqual(8, result.RoleAssignments.Count);
            Assert.AreEqual(8, result.AdjacencyGraph.RegionCount);
            Assert.AreEqual(1, result.AdjacencyGraph.NeutralZoneCount);
        }

        [Test]
        public void DefaultGeneratorAssignsEverySampleToExactlyOneOwnerType()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 98765u);
            RegionSkeletonGenerator generator = new();

            using RegionSkeletonResult result = generator.GenerateDefault(request, Allocator.TempJob);

            for (int i = 0; i < result.RegionIdField.Length; i++)
            {
                bool hasRegion = result.RegionIdField.Samples[i] > 0;
                bool hasNeutral = result.NeutralZoneIdField.Samples[i] > 0;
                Assert.AreNotEqual(hasRegion, hasNeutral, "Sample must have exactly one owner type at index " + i);
            }
        }

        [Test]
        public void DefaultGeneratorGivesEveryRegionAtLeastTwoDirectNeighbors()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 34567u);
            RegionSkeletonGenerator generator = new();

            using RegionSkeletonResult result = generator.GenerateDefault(request, Allocator.TempJob);

            for (int i = 1; i <= result.Settings.PlayerRegionCount; i++)
            {
                int neighborCount = result.AdjacencyGraph.GetRegionNeighborCount(new RegionId(i));
                Assert.GreaterOrEqual(neighborCount, 2, "Region should have at least two useful direct neighbors: " + i);
            }
        }

        [Test]
        public void DefaultValidatorPassesGeneratedSkeleton()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 45678u);
            RegionSkeletonGenerator generator = new();
            RegionSkeletonValidator validator = new();

            using RegionSkeletonResult result = generator.GenerateDefault(request, Allocator.TempJob);
            MapValidationReport report = validator.Validate(result);

            Assert.IsTrue(report.Passed, "Generated default skeleton should pass validation.");
            Assert.AreEqual(0, report.FatalIssueCount);
            Assert.AreEqual(0, report.ErrorIssueCount);
        }

        [Test]
        public void SameSeedProducesSameArtifactHash()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 11111u);
            RegionSkeletonGenerator generator = new();

            StableHash128 firstHash;
            StableHash128 secondHash;

            using (RegionSkeletonResult first = generator.GenerateDefault(request, Allocator.TempJob))
                firstHash = first.ArtifactHash;

            using (RegionSkeletonResult second = generator.GenerateDefault(request, Allocator.TempJob))
                secondHash = second.ArtifactHash;

            Assert.AreEqual(firstHash, secondHash);
        }

        [Test]
        public void DifferentSeedsUsuallyProduceDifferentArtifactHashes()
        {
            RegionSkeletonGenerator generator = new();

            using RegionSkeletonResult first = generator.GenerateDefault(MapGenerationRequest.CreateDefaultPreview(128, 128, 22222u), Allocator.TempJob);
            using RegionSkeletonResult second = generator.GenerateDefault(MapGenerationRequest.CreateDefaultPreview(128, 128, 33333u), Allocator.TempJob);

            Assert.AreNotEqual(first.ArtifactHash, second.ArtifactHash);
        }
    }
}
