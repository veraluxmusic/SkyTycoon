#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Owns the output of the competitive economy skeleton stage.
    /// Dispose this result when the generated fields are no longer needed.
    /// </summary>
    public sealed class RegionSkeletonResult : IDisposable
    {
        private NativeField2D<int> _regionIdField;
        private NativeField2D<int> _neutralZoneIdField;
        private bool _disposed;

        public RegionSkeletonResult(
            RegionSkeletonSettings settings,
            NativeField2D<int> regionIdField,
            NativeField2D<int> neutralZoneIdField,
            RegionRoleAssignment[] roleAssignments,
            RegionAdjacencyGraph adjacencyGraph,
            StableHash128 artifactHash)
        {
            if (!regionIdField.IsCreated)
                throw new ArgumentException("Region id field must be created.", nameof(regionIdField));

            if (!neutralZoneIdField.IsCreated)
                throw new ArgumentException("Neutral zone id field must be created.", nameof(neutralZoneIdField));

            if (regionIdField.Dimensions != settings.Dimensions)
                throw new ArgumentException("Region id field dimensions do not match skeleton settings.", nameof(regionIdField));

            if (neutralZoneIdField.Dimensions != settings.Dimensions)
                throw new ArgumentException("Neutral zone id field dimensions do not match skeleton settings.", nameof(neutralZoneIdField));

            if (roleAssignments == null)
                throw new ArgumentNullException(nameof(roleAssignments));

            if (roleAssignments.Length != settings.PlayerRegionCount)
                throw new ArgumentException("Role assignment count must match player region count.", nameof(roleAssignments));

            Settings = settings;
            Dimensions = settings.Dimensions;
            _regionIdField = regionIdField;
            _neutralZoneIdField = neutralZoneIdField;
            RoleAssignments = Array.AsReadOnly(roleAssignments);
            AdjacencyGraph = adjacencyGraph ?? throw new ArgumentNullException(nameof(adjacencyGraph));
            ArtifactHash = artifactHash;
        }

        public RegionSkeletonSettings Settings { get; }
        public HeightFieldDimensions Dimensions { get; }
        public NativeField2D<int> RegionIdField => _regionIdField;
        public NativeField2D<int> NeutralZoneIdField => _neutralZoneIdField;
        public IReadOnlyList<RegionRoleAssignment> RoleAssignments { get; }
        public RegionAdjacencyGraph AdjacencyGraph { get; }
        public StableHash128 ArtifactHash { get; }

        public void Dispose()
        {
            if (_disposed)
                return;

            _regionIdField.Dispose();
            _neutralZoneIdField.Dispose();
            _disposed = true;
        }
    }
}
