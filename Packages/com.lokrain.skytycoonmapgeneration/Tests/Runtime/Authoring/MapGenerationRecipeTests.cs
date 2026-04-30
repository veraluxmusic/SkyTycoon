#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Recipes;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using NUnit.Framework;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Tests.Authoring
{
    public sealed class MapGenerationRecipeTests
    {
        [Test]
        public void EmptyRecipeThrowsWhenRegionSkeletonProfileIsMissing()
        {
            MapGenerationRecipe recipe = ScriptableObject.CreateInstance<MapGenerationRecipe>();

            try
            {
                Assert.Throws<System.InvalidOperationException>(() =>
                    recipe.CompileRegionSkeletonSettings(MapGenerationRequest.CreateDefaultPreview(128, 128, 12345u)));
            }
            finally
            {
                Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void RegionSkeletonProfileCanCompileOutsideRecipe()
        {
            RegionSkeletonProfile profile = RegionSkeletonProfile.CreateTransientTycoonDefault();

            try
            {
                Assert.DoesNotThrow(() => profile.CompileSettings(MapGenerationRequest.CreateDefaultPreview(128, 128, 67890u)));
                Assert.DoesNotThrow(() => profile.CreateRoleCatalog());
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
