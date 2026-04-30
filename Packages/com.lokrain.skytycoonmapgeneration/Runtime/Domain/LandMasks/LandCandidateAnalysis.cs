#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Domain.LandMasks;
using Unity.Collections;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.LandMasks
{
    /// <summary>
    /// Deterministic analysis utilities for deriving temporary land-candidate masks from
    /// scalar height-field samples.
    ///
    /// This class deliberately does not represent a production map-generation stage.
    /// It exists to make F001 source fields observable before implementing the real
    /// percentile cut, connectivity filtering and area compensation stages.
    /// </summary>
    public static class LandCandidateAnalysis
    {
        public const float MinTargetLandPercent = 0f;
        public const float MaxTargetLandPercent = 100f;

        /// <summary>
        /// Builds only the land-candidate summary. A temporary mask is allocated internally.
        /// </summary>
        public static LandCandidateSummary BuildTopPercentileSummary(
            NativeArray<float> values,
            HeightFieldDimensions dimensions,
            float targetLandPercent,
            Allocator allocator = Allocator.Temp)
        {
            ValidateInputs(values, dimensions);

            using NativeArray<byte> mask = new(
                dimensions.SampleCount,
                allocator,
                NativeArrayOptions.ClearMemory);

            return BuildTopPercentileMask(
                values,
                dimensions,
                targetLandPercent,
                mask,
                allocator);
        }

        /// <summary>
        /// Builds an exact top-percentile mask and returns deterministic structural diagnostics.
        ///
        /// Cells are ranked by descending source value. Ties are resolved by ascending cell index,
        /// which makes the selected set stable across platforms and runs.
        /// </summary>
        public static LandCandidateSummary BuildTopPercentileMask(
            NativeArray<float> values,
            HeightFieldDimensions dimensions,
            float targetLandPercent,
            NativeArray<byte> destinationMask,
            Allocator allocator = Allocator.Temp)
        {
            ValidateInputs(values, dimensions);

            if (!destinationMask.IsCreated)
                throw new ArgumentException("Destination mask must be created.", nameof(destinationMask));

            if (destinationMask.Length != dimensions.SampleCount)
                throw new ArgumentException("Destination mask length must match the requested dimensions.", nameof(destinationMask));

            targetLandPercent = math.clamp(targetLandPercent, MinTargetLandPercent, MaxTargetLandPercent);

            ClearMask(destinationMask);

            LandCandidateSummary summary = LandCandidateSummary.CreateInvalid();
            summary.IsValid = true;
            summary.Width = dimensions.Width;
            summary.Height = dimensions.Height;
            summary.SampleCount = dimensions.SampleCount;
            summary.TargetLandPercent = targetLandPercent;
            summary.TargetLandCellCount = CalculateTargetLandCellCount(dimensions.SampleCount, targetLandPercent);

            using NativeList<LandCandidateRank> ranks = new(
                dimensions.SampleCount,
                allocator);

            int nonFiniteCount = 0;

            for (int index = 0; index < values.Length; index++)
            {
                float value = values[index];

                if (!math.isfinite(value))
                {
                    nonFiniteCount++;
                    continue;
                }

                ranks.Add(new LandCandidateRank(value, index));
            }

            summary.NonFiniteSourceCellCount = nonFiniteCount;

            if (summary.TargetLandCellCount == 0 || ranks.Length == 0)
            {
                summary.Threshold = 0f;
                summary.SelectedLandCellCount = 0;
                summary.SelectedLandPercent = 0f;
                return summary;
            }

            NativeArray<LandCandidateRank> rankArray = ranks.AsArray();
            rankArray.Sort();

            int selectedCount = math.min(summary.TargetLandCellCount, ranks.Length);
            summary.SelectedLandCellCount = selectedCount;
            summary.SelectedLandPercent = Percentage(selectedCount, dimensions.SampleCount);
            summary.Threshold = rankArray[selectedCount - 1].Value;

            for (int i = 0; i < selectedCount; i++)
                destinationMask[rankArray[i].Index] = 1;

            FillConnectivitySummary(destinationMask, dimensions, ref summary, allocator);
            return summary;
        }

        private static int CalculateTargetLandCellCount(int sampleCount, float targetLandPercent)
        {
            return math.clamp(
                (int)math.round(sampleCount * (targetLandPercent / 100f)),
                0,
                sampleCount);
        }

        private static void FillConnectivitySummary(
            NativeArray<byte> mask,
            HeightFieldDimensions dimensions,
            ref LandCandidateSummary summary,
            Allocator allocator)
        {
            using NativeArray<byte> visited = new(
                dimensions.SampleCount,
                allocator,
                NativeArrayOptions.ClearMemory);

            using NativeList<int> queue = new(
                dimensions.SampleCount,
                allocator);

            int componentCount = 0;
            int largestComponentSize = 0;
            int borderLandCount = 0;
            int isolatedLandCount = 0;

            for (int index = 0; index < mask.Length; index++)
            {
                if (mask[index] == 0)
                    continue;

                int x = index % dimensions.Width;
                int y = index / dimensions.Width;

                if (IsBorderCell(x, y, dimensions.Width, dimensions.Height))
                    borderLandCount++;

                if (CountSelectedNeighbours4(mask, dimensions, x, y) == 0)
                    isolatedLandCount++;

                if (visited[index] != 0)
                    continue;

                int componentSize = FloodFillComponent4(
                    mask,
                    visited,
                    dimensions,
                    index,
                    queue);

                componentCount++;

                if (componentSize > largestComponentSize)
                    largestComponentSize = componentSize;
            }

            summary.ComponentCount4Connected = componentCount;
            summary.LargestComponentCellCount = largestComponentSize;
            summary.LargestComponentPercentOfSelected = Percentage(largestComponentSize, summary.SelectedLandCellCount);
            summary.SecondaryComponentCellCount = math.max(0, summary.SelectedLandCellCount - largestComponentSize);
            summary.BorderLandCellCount = borderLandCount;
            summary.IsolatedLandCellCount = isolatedLandCount;
        }

        private static int FloodFillComponent4(
            NativeArray<byte> mask,
            NativeArray<byte> visited,
            HeightFieldDimensions dimensions,
            int seedIndex,
            NativeList<int> queue)
        {
            queue.Clear();
            queue.Add(seedIndex);
            visited[seedIndex] = 1;

            int head = 0;
            int componentSize = 0;

            while (head < queue.Length)
            {
                int index = queue[head++];
                componentSize++;

                int x = index % dimensions.Width;
                int y = index / dimensions.Width;

                TryEnqueue(mask, visited, dimensions, x - 1, y, queue);
                TryEnqueue(mask, visited, dimensions, x + 1, y, queue);
                TryEnqueue(mask, visited, dimensions, x, y - 1, queue);
                TryEnqueue(mask, visited, dimensions, x, y + 1, queue);
            }

            return componentSize;
        }

        private static void TryEnqueue(
            NativeArray<byte> mask,
            NativeArray<byte> visited,
            HeightFieldDimensions dimensions,
            int x,
            int y,
            NativeList<int> queue)
        {
            if ((uint)x >= (uint)dimensions.Width || (uint)y >= (uint)dimensions.Height)
                return;

            int index = y * dimensions.Width + x;

            if (mask[index] == 0 || visited[index] != 0)
                return;

            visited[index] = 1;
            queue.Add(index);
        }

        private static int CountSelectedNeighbours4(
            NativeArray<byte> mask,
            HeightFieldDimensions dimensions,
            int x,
            int y)
        {
            int count = 0;

            if (IsSelected(mask, dimensions, x - 1, y))
                count++;

            if (IsSelected(mask, dimensions, x + 1, y))
                count++;

            if (IsSelected(mask, dimensions, x, y - 1))
                count++;

            if (IsSelected(mask, dimensions, x, y + 1))
                count++;

            return count;
        }

        private static bool IsSelected(
            NativeArray<byte> mask,
            HeightFieldDimensions dimensions,
            int x,
            int y)
        {
            if ((uint)x >= (uint)dimensions.Width || (uint)y >= (uint)dimensions.Height)
                return false;

            return mask[y * dimensions.Width + x] != 0;
        }

        private static bool IsBorderCell(int x, int y, int width, int height)
        {
            return x == 0 || y == 0 || x == width - 1 || y == height - 1;
        }

        private static void ClearMask(NativeArray<byte> mask)
        {
            for (int i = 0; i < mask.Length; i++)
                mask[i] = 0;
        }

        private static float Percentage(int value, int total)
        {
            return total > 0 ? value * 100f / total : 0f;
        }

        private static void ValidateInputs(
            NativeArray<float> values,
            HeightFieldDimensions dimensions)
        {
            dimensions.Validate();

            if (!values.IsCreated)
                throw new ArgumentException("Value buffer must be created.", nameof(values));

            if (values.Length != dimensions.SampleCount)
                throw new ArgumentException("Value buffer length must match the requested dimensions.", nameof(values));
        }

        private readonly struct LandCandidateRank : IComparable<LandCandidateRank>
        {
            public readonly float Value;
            public readonly int Index;

            public LandCandidateRank(float value, int index)
            {
                Value = value;
                Index = index;
            }

            public int CompareTo(LandCandidateRank other)
            {
                int valueComparison = other.Value.CompareTo(Value);

                if (valueComparison != 0)
                    return valueComparison;

                return Index.CompareTo(other.Index);
            }
        }
    }
}