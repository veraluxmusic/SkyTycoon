#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using NUnit.Framework;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.Authoring
{
    public sealed class MacroLandformProfileTests
    {
        [Test]
        public void DefaultProfileCompilesToRuntimeSettings()
        {
            MacroLandformProfile profile = MacroLandformProfile.CreateTransientDefault();

            try
            {
                MapGenerationRequest request = MapGenerationRequest.CreateDefaultPreview(128, 128, 12345u);
                MacroLandformSettings settings = profile.CompileSettings(request);

                Assert.That(settings.Dimensions, Is.EqualTo(request.Dimensions));
                Assert.That(settings.Seed.Value, Is.Not.EqualTo(0u));
                Assert.That(settings.TargetLandPercent, Is.EqualTo(0.70f).Within(0.0001f));
                Assert.That(settings.HardWaterBorderThickness, Is.EqualTo(1));
                Assert.DoesNotThrow(() => settings.Validate());
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
