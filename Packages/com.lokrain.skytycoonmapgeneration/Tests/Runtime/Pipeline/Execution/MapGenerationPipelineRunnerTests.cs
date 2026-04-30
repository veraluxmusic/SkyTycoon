#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Results;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Execution;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.Pipeline.Execution
{
    public sealed class MapGenerationPipelineRunnerTests
    {
        [Test]
        public void Run_WithDefaultCompiledPlan_ProducesPassingRegionSkeletonResult()
        {
            CompiledMapGenerationPlan plan = CreateDefaultPlan();
            MapGenerationPipelineRunner runner = new();
            MapGenerationExecutionResult result = runner.Run(plan, Allocator.Persistent);

            try
            {
                Assert.That(result.Status, Is.EqualTo(MapGenerationStatus.Succeeded));
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.ValidationReport.Passed, Is.True);
                Assert.That(result.RegionSkeletonResult.RegionIdField.IsCreated, Is.True);
                Assert.That(result.RegionSkeletonResult.NeutralZoneIdField.IsCreated, Is.True);
                Assert.That(result.RegionSkeletonResult.Dimensions, Is.EqualTo(plan.Request.Dimensions));
                Assert.That(result.RegionSkeletonResult.RoleAssignments.Count, Is.EqualTo(plan.Request.PlayerCount));
                Assert.That(result.ArtifactHash.IsZero, Is.False);
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void Run_WithSameCompiledPlan_ProducesStableArtifactHash()
        {
            CompiledMapGenerationPlan plan = CreateDefaultPlan();
            MapGenerationPipelineRunner runner = new();

            MapGenerationExecutionResult first = runner.Run(plan, Allocator.Persistent);
            MapGenerationExecutionResult second = runner.Run(plan, Allocator.Persistent);

            try
            {
                Assert.That(first.ArtifactHash, Is.EqualTo(second.ArtifactHash));
                Assert.That(first.RegionSkeletonResult.ArtifactHash, Is.EqualTo(second.RegionSkeletonResult.ArtifactHash));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }

        [Test]
        public void RegionSkeletonResult_AfterExecutionResultDisposed_ThrowsObjectDisposedException()
        {
            CompiledMapGenerationPlan plan = CreateDefaultPlan();
            MapGenerationPipelineRunner runner = new();
            MapGenerationExecutionResult result = runner.Run(plan, Allocator.Persistent);

            result.Dispose();

            Assert.That(result.IsDisposed, Is.True);
            Assert.Throws<System.ObjectDisposedException>(() => _ = result.RegionSkeletonResult);
        }

        private static CompiledMapGenerationPlan CreateDefaultPlan()
        {
            MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(
                width: 128,
                height: 128,
                seed: 12345u);

            RegionSkeletonSettings settings = RegionSkeletonSettings.CreateDefault(request);
            RegionRoleCatalog roleCatalog = RegionRoleCatalog.CreateTycoonEightRegionDefault();
            CompiledRegionSkeletonStage regionSkeletonStage = new(settings, roleCatalog);
            return new CompiledMapGenerationPlan(request, regionSkeletonStage);
        }
    }
}
