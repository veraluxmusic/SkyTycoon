#nullable enable

using System;
using System.Globalization;
using Unity.Collections;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Managed adjacency graph for region-vs-region and region-vs-neutral contacts.
    /// Built from generated fields and used by validation, reports, and previews.
    /// </summary>
    public sealed class RegionAdjacencyGraph
    {
        private readonly bool[,] _regionEdges;
        private readonly bool[,] _regionNeutralEdges;
        private readonly int[] _regionNeighborCounts;
        private readonly int[] _neutralZoneContactCounts;

        public RegionAdjacencyGraph(int regionCount, int neutralZoneCount)
        {
            if (regionCount < RegionSkeletonSettings.MinPlayerRegionCount || regionCount > RegionSkeletonSettings.MaxPlayerRegionCount)
                throw new ArgumentOutOfRangeException(nameof(regionCount), regionCount, "Region count is outside the supported range.");

            if (neutralZoneCount < 0 || neutralZoneCount > RegionSkeletonSettings.MaxNeutralZoneCount)
                throw new ArgumentOutOfRangeException(nameof(neutralZoneCount), neutralZoneCount, "Neutral zone count is outside the supported range.");

            RegionCount = regionCount;
            NeutralZoneCount = neutralZoneCount;
            _regionEdges = new bool[regionCount, regionCount];
            _regionNeutralEdges = new bool[regionCount, Math.Max(1, neutralZoneCount)];
            _regionNeighborCounts = new int[regionCount];
            _neutralZoneContactCounts = new int[Math.Max(1, neutralZoneCount)];
        }

        public int RegionCount { get; }
        public int NeutralZoneCount { get; }
        public int RegionEdgeCount { get; private set; }
        public int RegionNeutralEdgeCount { get; private set; }

        public bool HasRegionEdge(RegionId a, RegionId b)
        {
            ValidateRegion(a);
            ValidateRegion(b);
            return _regionEdges[a.Value - 1, b.Value - 1];
        }

        public bool HasRegionNeutralEdge(RegionId regionId, NeutralZoneId neutralZoneId)
        {
            ValidateRegion(regionId);
            ValidateNeutralZone(neutralZoneId);
            return _regionNeutralEdges[regionId.Value - 1, neutralZoneId.Value - 1];
        }

        public int GetRegionNeighborCount(RegionId regionId)
        {
            ValidateRegion(regionId);
            return _regionNeighborCounts[regionId.Value - 1];
        }

        public int GetNeutralZoneContactRegionCount(NeutralZoneId neutralZoneId)
        {
            ValidateNeutralZone(neutralZoneId);
            return _neutralZoneContactCounts[neutralZoneId.Value - 1];
        }

        public RegionId[] GetRegionNeighbors(RegionId regionId)
        {
            ValidateRegion(regionId);

            int count = _regionNeighborCounts[regionId.Value - 1];
            RegionId[] neighbors = new RegionId[count];
            int writeIndex = 0;

            for (int i = 0; i < RegionCount; i++)
            {
                if (_regionEdges[regionId.Value - 1, i])
                    neighbors[writeIndex++] = new RegionId(i + 1);
            }

            return neighbors;
        }

        public void AddRegionEdge(RegionId a, RegionId b)
        {
            ValidateRegion(a);
            ValidateRegion(b);

            if (a == b)
                return;

            int ai = a.Value - 1;
            int bi = b.Value - 1;

            if (_regionEdges[ai, bi])
                return;

            _regionEdges[ai, bi] = true;
            _regionEdges[bi, ai] = true;
            _regionNeighborCounts[ai]++;
            _regionNeighborCounts[bi]++;
            RegionEdgeCount++;
        }

        public void AddRegionNeutralEdge(RegionId regionId, NeutralZoneId neutralZoneId)
        {
            ValidateRegion(regionId);
            ValidateNeutralZone(neutralZoneId);

            int ri = regionId.Value - 1;
            int zi = neutralZoneId.Value - 1;

            if (_regionNeutralEdges[ri, zi])
                return;

            _regionNeutralEdges[ri, zi] = true;
            _neutralZoneContactCounts[zi]++;
            RegionNeutralEdgeCount++;
        }

        public static RegionAdjacencyGraph BuildFromFields(
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds,
            HeightFieldDimensions dimensions,
            int regionCount,
            int neutralZoneCount)
        {
            dimensions.Validate();

            if (!regionIds.IsCreated)
                throw new ArgumentException("Region id field must be created.", nameof(regionIds));

            if (!neutralZoneIds.IsCreated)
                throw new ArgumentException("Neutral zone id field must be created.", nameof(neutralZoneIds));

            if (regionIds.Length != dimensions.SampleCount)
                throw new ArgumentException("Region id field length does not match dimensions.", nameof(regionIds));

            if (neutralZoneIds.Length != dimensions.SampleCount)
                throw new ArgumentException("Neutral zone id field length does not match dimensions.", nameof(neutralZoneIds));

            RegionAdjacencyGraph graph = new(regionCount, neutralZoneCount);

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int index = y * dimensions.Width + x;

                    if (x + 1 < dimensions.Width)
                        AddContact(graph, regionIds, neutralZoneIds, index, index + 1, regionCount, neutralZoneCount);

                    if (y + 1 < dimensions.Height)
                        AddContact(graph, regionIds, neutralZoneIds, index, index + dimensions.Width, regionCount, neutralZoneCount);
                }
            }

            return graph;
        }

        private static void AddContact(
            RegionAdjacencyGraph graph,
            NativeArray<int> regionIds,
            NativeArray<int> neutralZoneIds,
            int aIndex,
            int bIndex,
            int regionCount,
            int neutralZoneCount)
        {
            int aRegion = regionIds[aIndex];
            int bRegion = regionIds[bIndex];
            int aNeutral = neutralZoneIds[aIndex];
            int bNeutral = neutralZoneIds[bIndex];

            if (aRegion > 0 && bRegion > 0 && aRegion != bRegion)
            {
                if (aRegion <= regionCount && bRegion <= regionCount)
                    graph.AddRegionEdge(new RegionId(aRegion), new RegionId(bRegion));

                return;
            }

            if (neutralZoneCount <= 0)
                return;

            if (aRegion > 0 && bNeutral > 0 && bNeutral <= neutralZoneCount && aRegion <= regionCount)
            {
                graph.AddRegionNeutralEdge(new RegionId(aRegion), new NeutralZoneId(bNeutral));
                return;
            }

            if (bRegion > 0 && aNeutral > 0 && aNeutral <= neutralZoneCount && bRegion <= regionCount)
                graph.AddRegionNeutralEdge(new RegionId(bRegion), new NeutralZoneId(aNeutral));
        }

        private void ValidateRegion(RegionId regionId)
        {
            regionId.Validate();

            if (regionId.Value > RegionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(regionId),
                    regionId.Value,
                    "Region id exceeds graph region count " + RegionCount.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private void ValidateNeutralZone(NeutralZoneId neutralZoneId)
        {
            neutralZoneId.Validate();

            if (neutralZoneId.Value > NeutralZoneCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(neutralZoneId),
                    neutralZoneId.Value,
                    "Neutral zone id exceeds graph neutral zone count " + NeutralZoneCount.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }
    }
}
