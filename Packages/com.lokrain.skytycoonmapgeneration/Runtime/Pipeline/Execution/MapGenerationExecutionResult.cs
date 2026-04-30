#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Results;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Execution
{
    /// <summary>
    /// Owns the output of a compiled map-generation run.
    ///
    /// Stage outputs are explicit typed properties with clear disposal ownership.
    /// </summary>
    public sealed class MapGenerationExecutionResult : IDisposable
    {
        private RegionSkeletonResult? _regionSkeletonResult;
        private MacroLandformResult? _macroLandformResult;
        private bool _disposed;

        public MapGenerationExecutionResult(
            MapGenerationResult summary,
            RegionSkeletonResult regionSkeletonResult,
            MacroLandformResult macroLandformResult)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            _regionSkeletonResult = regionSkeletonResult ?? throw new ArgumentNullException(nameof(regionSkeletonResult));
            _macroLandformResult = macroLandformResult ?? throw new ArgumentNullException(nameof(macroLandformResult));
        }

        public MapGenerationResult Summary { get; }

        public MapGenerationRequest Request => Summary.Request;
        public MapGenerationStatus Status => Summary.Status;
        public StableHash128 ArtifactHash => Summary.ArtifactHash;
        public MapValidationReport ValidationReport => Summary.ValidationReport;
        public bool Succeeded => Summary.Succeeded;
        public bool IsDisposed => _disposed;

        public RegionSkeletonResult RegionSkeletonResult
        {
            get
            {
                ThrowIfDisposed();
                return _regionSkeletonResult!;
            }
        }

        public MacroLandformResult MacroLandformResult
        {
            get
            {
                ThrowIfDisposed();
                return _macroLandformResult!;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_macroLandformResult != null)
            {
                _macroLandformResult.Dispose();
                _macroLandformResult = null;
            }

            if (_regionSkeletonResult != null)
            {
                _regionSkeletonResult.Dispose();
                _regionSkeletonResult = null;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MapGenerationExecutionResult));
        }
    }
}
