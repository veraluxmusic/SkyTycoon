#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.Authoring
{
    public sealed class RegionSkeletonProfileTests
    {
        [Test]
        public void TycoonDefaultProfileCompilesToRuntimeSettings()
        {
            RegionSkeletonProfile profile = RegionSkeletonProfile.CreateTransientTycoonDefault();

            try
            {
                MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 12345u);
                RegionSkeletonSettings settings = profile.CompileSettings(request);
                RegionRoleCatalog catalog = profile.CreateRoleCatalog();

                Assert.AreEqual(8, settings.PlayerRegionCount);
                Assert.AreEqual(1, settings.NeutralZoneCount);
                Assert.AreEqual(RegionSkeletonNeutralPolicy.CentralCore, settings.NeutralPolicy);
                Assert.GreaterOrEqual(catalog.Count, settings.PlayerRegionCount);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ProfileCompiledSettingsCanGenerateAndValidateSkeleton()
        {
            RegionSkeletonProfile profile = RegionSkeletonProfile.CreateTransientTycoonDefault();

            try
            {
                MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 98765u);
                RegionSkeletonSettings settings = profile.CompileSettings(request);
                RegionRoleCatalog catalog = profile.CreateRoleCatalog();
                RegionSkeletonGenerator generator = new();
                RegionSkeletonValidator validator = new();

                using RegionSkeletonResult result = generator.Generate(settings, catalog, Allocator.TempJob);

                Assert.IsTrue(validator.Validate(result).Passed);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PreviewOverridesAreReflectedInCompiledSettings()
        {
            RegionSkeletonProfile profile = RegionSkeletonProfile.CreateTransientTycoonDefault();

            try
            {
                profile.ApplyPreviewOverrides(
                    RegionSkeletonRoleAssignmentPolicy.FixedCatalogOrder,
                    0.20f,
                    0.04f,
                    3,
                    0.05f,
                    0.03f,
                    5);

                RegionSkeletonSettings settings = profile.CompileSettings(MapGenerationRequest.CreateDefaultPreview(128, 128, 55555u));

                Assert.AreEqual(RegionSkeletonRoleAssignmentPolicy.FixedCatalogOrder, settings.RoleAssignmentPolicy);
                Assert.AreEqual(0.20f, settings.NeutralCoreRadiusPercentOfMinDimension, 0.0001f);
                Assert.AreEqual(0.04f, settings.BoundaryWarpAmplitudeTurns, 0.0001f);
                Assert.AreEqual(3, settings.MinUsefulRegionNeighbors);
                Assert.AreEqual(0.05f, settings.MinRegionAreaPercentOfMap, 0.0001f);
                Assert.AreEqual(0.03f, settings.MinNeutralAreaPercentOfMap, 0.0001f);
                Assert.AreEqual(5, settings.MinRegionsTouchingNeutralZone);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
