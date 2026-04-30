#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Domain.LandMasks;
using Lokrain.SkyTycoon.MapGeneration.Generation.LandMasks;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.Tests
{
    public sealed class LandCandidateAnalysisTests
    {
        [Test]
        public void BuildTopPercentileMask_SelectsExactTargetCount()
        {
            HeightFieldDimensions dimensions = new HeightFieldDimensions(4, 4);

            using NativeArray<float> values = CreateValues(
                0.10f, 0.20f, 0.30f, 0.40f,
                0.50f, 0.90f, 0.80f, 0.15f,
                0.25f, 0.70f, 0.60f, 0.35f,
                0.45f, 0.05f, 0.12f, 0.18f);

            using NativeArray<byte> mask = new NativeArray<byte>(
                dimensions.SampleCount,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);

            LandCandidateSummary summary = LandCandidateAnalysis.BuildTopPercentileMask(
                values,
                dimensions,
                targetLandPercent: 25f,
                mask,
                Allocator.Temp);

            Assert.IsTrue(summary.IsValid);
            Assert.AreEqual(4, summary.TargetLandCellCount);
            Assert.AreEqual(4, summary.SelectedLandCellCount);
            Assert.AreEqual(25f, summary.SelectedLandPercent, 0.0001f);
            Assert.AreEqual(0.60f, summary.Threshold, 0.0001f);

            AssertMaskSelected(mask, 5, 6, 9, 10);
        }

        [Test]
        public void BuildTopPercentileMask_ReportsConnectedCandidate()
        {
            HeightFieldDimensions dimensions = new HeightFieldDimensions(4, 4);

            using NativeArray<float> values = CreateValues(
                0.10f, 0.20f, 0.30f, 0.40f,
                0.50f, 0.90f, 0.80f, 0.15f,
                0.25f, 0.70f, 0.60f, 0.35f,
                0.45f, 0.05f, 0.12f, 0.18f);

            using NativeArray<byte> mask = new NativeArray<byte>(
                dimensions.SampleCount,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);

            LandCandidateSummary summary = LandCandidateAnalysis.BuildTopPercentileMask(
                values,
                dimensions,
                targetLandPercent: 25f,
                mask,
                Allocator.Temp);

            Assert.AreEqual(1, summary.ComponentCount4Connected);
            Assert.AreEqual(4, summary.LargestComponentCellCount);
            Assert.AreEqual(100f, summary.LargestComponentPercentOfSelected, 0.0001f);
            Assert.AreEqual(0, summary.SecondaryComponentCellCount);
            Assert.AreEqual(0, summary.BorderLandCellCount);
            Assert.AreEqual(0, summary.IsolatedLandCellCount);
            Assert.IsTrue(summary.IsSingleComponentCandidate);
            Assert.IsFalse(summary.HasSecondaryComponents);
        }

        [Test]
        public void BuildTopPercentileMask_ReportsFragmentedCandidate()
        {
            HeightFieldDimensions dimensions = new HeightFieldDimensions(4, 4);

            using NativeArray<float> values = CreateValues(
                0.90f, 0.01f, 0.80f, 0.02f,
                0.03f, 0.04f, 0.05f, 0.06f,
                0.70f, 0.07f, 0.60f, 0.08f,
                0.09f, 0.10f, 0.11f, 0.12f);

            using NativeArray<byte> mask = new NativeArray<byte>(
                dimensions.SampleCount,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);

            LandCandidateSummary summary = LandCandidateAnalysis.BuildTopPercentileMask(
                values,
                dimensions,
                targetLandPercent: 25f,
                mask,
                Allocator.Temp);

            Assert.AreEqual(4, summary.SelectedLandCellCount);
            Assert.AreEqual(4, summary.ComponentCount4Connected);
            Assert.AreEqual(1, summary.LargestComponentCellCount);
            Assert.AreEqual(25f, summary.LargestComponentPercentOfSelected, 0.0001f);
            Assert.AreEqual(3, summary.SecondaryComponentCellCount);
            Assert.AreEqual(3, summary.BorderLandCellCount);
            Assert.AreEqual(4, summary.IsolatedLandCellCount);
            Assert.IsFalse(summary.IsSingleComponentCandidate);
            Assert.IsTrue(summary.HasSecondaryComponents);

            AssertMaskSelected(mask, 0, 2, 8, 10);
        }

        [Test]
        public void BuildTopPercentileMask_IgnoresNonFiniteValues()
        {
            HeightFieldDimensions dimensions = new HeightFieldDimensions(2, 2);

            using NativeArray<float> values = CreateValues(
                1.0f,
                float.NaN,
                0.5f,
                float.PositiveInfinity);

            using NativeArray<byte> mask = new NativeArray<byte>(
                dimensions.SampleCount,
                Allocator.Temp,
                NativeArrayOptions.ClearMemory);

            LandCandidateSummary summary = LandCandidateAnalysis.BuildTopPercentileMask(
                values,
                dimensions,
                targetLandPercent: 100f,
                mask,
                Allocator.Temp);

            Assert.AreEqual(2, summary.NonFiniteSourceCellCount);
            Assert.AreEqual(4, summary.TargetLandCellCount);
            Assert.AreEqual(2, summary.SelectedLandCellCount);
            AssertMaskSelected(mask, 0, 2);
        }

        private static NativeArray<float> CreateValues(params float[] values)
        {
            NativeArray<float> array = new NativeArray<float>(
                values.Length,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < values.Length; i++)
                array[i] = values[i];

            return array;
        }

        private static void AssertMaskSelected(NativeArray<byte> mask, params int[] selectedIndices)
        {
            for (int i = 0; i < mask.Length; i++)
            {
                bool shouldBeSelected = Contains(selectedIndices, i);
                Assert.AreEqual(
                    shouldBeSelected ? 1 : 0,
                    mask[i],
                    "Unexpected mask value at index " + i);
            }
        }

        private static bool Contains(int[] values, int candidate)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == candidate)
                    return true;
            }

            return false;
        }
    }
}