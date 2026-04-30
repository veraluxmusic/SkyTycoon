#nullable enable

using System;
using System.Globalization;
using Unity.Collections;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Deterministic validation for the competitive economy skeleton stage.
    /// This is intentionally explicit and report-producing; validation failures must be inspectable.
    /// </summary>
    public sealed class RegionSkeletonValidator
    {
        public MapValidationReport Validate(RegionSkeletonResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            result.Settings.Validate();

            MapValidationReportBuilder builder = new();
            NativeArray<int> regionIds = result.RegionIdField.Samples;
            NativeArray<int> neutralZoneIds = result.NeutralZoneIdField.Samples;
            HeightFieldDimensions dimensions = result.Dimensions;

            ValidateFieldOwnership(result, builder, regionIds, neutralZoneIds);

            int[] regionCellCounts = CountRegionCells(regionIds, result.Settings.PlayerRegionCount);
            int[] neutralCellCounts = CountNeutralZoneCells(neutralZoneIds, result.Settings.NeutralZoneCount);

            ValidateRegionAreas(result.Settings, builder, regionCellCounts, dimensions.SampleCount);
            ValidateNeutralZones(result, builder, neutralCellCounts, dimensions.SampleCount);
            ValidateRegionNeighbors(result, builder);
            ValidateRegionConnectivity(result.Settings, builder, regionIds);

            return builder.Build(result.ArtifactHash);
        }

        private static void ValidateFieldOwnership(
            RegionSkeletonResult result,
            MapValidationReportBuilder builder,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds)
        {
            for (int i = 0; i < regionIds.Length; i++)
            {
                int region = regionIds[i];
                int neutral = neutralZoneIds[i];

                bool hasRegion = region > 0;
                bool hasNeutral = neutral > 0;

                if (hasRegion == hasNeutral)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.FieldOwnershipMismatch,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Each skeleton cell must belong to exactly one home region or one neutral zone. Invalid sample index: "
                        + i.ToString(CultureInfo.InvariantCulture));

                    return;
                }

                if (region < 0 || region > result.Settings.PlayerRegionCount)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.FieldOwnershipMismatch,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Region id sample is outside the requested player region range at sample index "
                        + i.ToString(CultureInfo.InvariantCulture));

                    return;
                }

                if (neutral < 0 || neutral > result.Settings.NeutralZoneCount)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.FieldOwnershipMismatch,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.NeutralZoneId,
                        "Neutral zone id sample is outside the requested neutral zone range at sample index "
                        + i.ToString(CultureInfo.InvariantCulture));

                    return;
                }
            }
        }

        private static int[] CountRegionCells(NativeArray<int> regionIds, int regionCount)
        {
            int[] counts = new int[regionCount];

            for (int i = 0; i < regionIds.Length; i++)
            {
                int region = regionIds[i];

                if (region > 0 && region <= regionCount)
                    counts[region - 1]++;
            }

            return counts;
        }

        private static int[] CountNeutralZoneCells(NativeArray<int> neutralZoneIds, int neutralZoneCount)
        {
            int[] counts = new int[Math.Max(1, neutralZoneCount)];

            for (int i = 0; i < neutralZoneIds.Length; i++)
            {
                int neutral = neutralZoneIds[i];

                if (neutral > 0 && neutral <= neutralZoneCount)
                    counts[neutral - 1]++;
            }

            return counts;
        }

        private static void ValidateRegionAreas(
            RegionSkeletonSettings settings,
            MapValidationReportBuilder builder,
            int[] regionCellCounts,
            int sampleCount)
        {
            int minimumCells = Math.Max(1, (int)Math.Ceiling(sampleCount * settings.MinRegionAreaPercentOfMap));

            for (int i = 0; i < settings.PlayerRegionCount; i++)
            {
                int regionValue = i + 1;
                int count = regionCellCounts[i];

                if (count == 0)
                {
                    builder.AddFatal(
                        RegionSkeletonIssueIds.RegionMissingCells,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Region " + regionValue.ToString(CultureInfo.InvariantCulture) + " has no cells.");

                    continue;
                }

                if (count < minimumCells)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.RegionTooSmall,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Region "
                        + regionValue.ToString(CultureInfo.InvariantCulture)
                        + " is below minimum area. Cells: "
                        + count.ToString(CultureInfo.InvariantCulture)
                        + ", required: "
                        + minimumCells.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void ValidateNeutralZones(
            RegionSkeletonResult result,
            MapValidationReportBuilder builder,
            int[] neutralCellCounts,
            int sampleCount)
        {
            RegionSkeletonSettings settings = result.Settings;

            if (settings.NeutralZoneCount == 0)
                return;

            int minimumCells = Math.Max(1, (int)Math.Ceiling(sampleCount * settings.MinNeutralAreaPercentOfMap));

            for (int i = 0; i < settings.NeutralZoneCount; i++)
            {
                int zoneValue = i + 1;
                int count = neutralCellCounts[i];

                if (count == 0)
                {
                    builder.AddFatal(
                        RegionSkeletonIssueIds.NeutralZoneMissingCells,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.NeutralZoneId,
                        "Neutral zone " + zoneValue.ToString(CultureInfo.InvariantCulture) + " has no cells.");

                    continue;
                }

                if (count < minimumCells)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.NeutralZoneTooSmall,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.NeutralZoneId,
                        "Neutral zone "
                        + zoneValue.ToString(CultureInfo.InvariantCulture)
                        + " is below minimum area. Cells: "
                        + count.ToString(CultureInfo.InvariantCulture)
                        + ", required: "
                        + minimumCells.ToString(CultureInfo.InvariantCulture));
                }

                int touchingRegions = result.AdjacencyGraph.GetNeutralZoneContactRegionCount(new NeutralZoneId(zoneValue));

                if (touchingRegions < settings.MinRegionsTouchingNeutralZone)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.NeutralZonePoorAccess,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.NeutralZoneId,
                        "Neutral zone "
                        + zoneValue.ToString(CultureInfo.InvariantCulture)
                        + " touches too few home regions. Touching regions: "
                        + touchingRegions.ToString(CultureInfo.InvariantCulture)
                        + ", required: "
                        + settings.MinRegionsTouchingNeutralZone.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void ValidateRegionNeighbors(RegionSkeletonResult result, MapValidationReportBuilder builder)
        {
            for (int i = 0; i < result.Settings.PlayerRegionCount; i++)
            {
                RegionId regionId = new(i + 1);
                int neighborCount = result.AdjacencyGraph.GetRegionNeighborCount(regionId);

                if (neighborCount < result.Settings.MinUsefulRegionNeighbors)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.RegionTooFewNeighbors,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Region "
                        + regionId.Value.ToString(CultureInfo.InvariantCulture)
                        + " has too few direct useful neighbors. Neighbors: "
                        + neighborCount.ToString(CultureInfo.InvariantCulture)
                        + ", required: "
                        + result.Settings.MinUsefulRegionNeighbors.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static void ValidateRegionConnectivity(
            RegionSkeletonSettings settings,
            MapValidationReportBuilder builder,
            NativeArray<int> regionIds)
        {
            HeightFieldDimensions dimensions = settings.Dimensions;
            int sampleCount = dimensions.SampleCount;
            bool[] visited = new bool[sampleCount];
            int[] queue = new int[sampleCount];

            for (int regionValue = 1; regionValue <= settings.PlayerRegionCount; regionValue++)
            {
                Array.Clear(visited, 0, visited.Length);

                int firstIndex = -1;
                int totalCells = 0;

                for (int i = 0; i < regionIds.Length; i++)
                {
                    if (regionIds[i] != regionValue)
                        continue;

                    totalCells++;

                    if (firstIndex < 0)
                        firstIndex = i;
                }

                if (firstIndex < 0)
                    continue;

                int connectedCells = FloodFillRegion(regionIds, dimensions, regionValue, firstIndex, visited, queue);

                if (connectedCells != totalCells)
                {
                    builder.AddError(
                        RegionSkeletonIssueIds.RegionFragmented,
                        MapGenerationStageIds.CompetitiveEconomySkeleton,
                        MapFieldIds.RegionId,
                        "Region "
                        + regionValue.ToString(CultureInfo.InvariantCulture)
                        + " is fragmented. Connected cells: "
                        + connectedCells.ToString(CultureInfo.InvariantCulture)
                        + ", total cells: "
                        + totalCells.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private static int FloodFillRegion(
            NativeArray<int> regionIds,
            HeightFieldDimensions dimensions,
            int regionValue,
            int startIndex,
            bool[] visited,
            int[] queue)
        {
            int read = 0;
            int write = 0;
            int connected = 0;

            queue[write++] = startIndex;
            visited[startIndex] = true;

            while (read < write)
            {
                int index = queue[read++];
                connected++;

                int y = index / dimensions.Width;
                int x = index - y * dimensions.Width;

                TryEnqueue(index - 1, x > 0, regionValue, regionIds, visited, queue, ref write);
                TryEnqueue(index + 1, x + 1 < dimensions.Width, regionValue, regionIds, visited, queue, ref write);
                TryEnqueue(index - dimensions.Width, y > 0, regionValue, regionIds, visited, queue, ref write);
                TryEnqueue(index + dimensions.Width, y + 1 < dimensions.Height, regionValue, regionIds, visited, queue, ref write);
            }

            return connected;
        }

        private static void TryEnqueue(
            int index,
            bool inBounds,
            int regionValue,
            NativeArray<int> regionIds,
            bool[] visited,
            int[] queue,
            ref int write)
        {
            if (!inBounds)
                return;

            if (visited[index])
                return;

            if (regionIds[index] != regionValue)
                return;

            visited[index] = true;
            queue[write++] = index;
        }
    }
}
