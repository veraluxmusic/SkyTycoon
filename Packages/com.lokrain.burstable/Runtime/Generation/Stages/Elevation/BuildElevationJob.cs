using Lokrain.Burstable.Generation.Shaping;
using Lokrain.Burstable.Math;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    /// <summary>
    /// Builds the primary elevation field for a rectangular tile map.
    /// </summary>
    /// <remarks>
    /// This job is an implementation detail of the elevation stage. It parallelizes the
    /// internal elevation evaluator over a contiguous row-major tile field.
    ///
    /// The job does not own generation policy. Elevation behavior belongs to the configured
    /// evaluator and shaping behavior belongs to <see cref="EdgeFalloff"/>. The job only
    /// converts a linear tile index to coordinates and writes the evaluated value to
    /// workspace-owned storage.
    ///
    /// The caller owns dependency management and must ensure <see cref="ElevationValues"/>
    /// remains valid until the scheduled job has completed.
    /// </remarks>
    [BurstCompile]
    internal struct BuildElevationJob : IJobParallelFor
    {
        /// <summary>
        /// Generated map dimensions.
        /// </summary>
        /// <remarks>
        /// Dimensions are assumed to have been validated before the job is scheduled.
        /// </remarks>
        public MapDimensions Dimensions;

        /// <summary>
        /// Deterministic elevation evaluator used for each tile.
        /// </summary>
        public ElevationAlgorithm Algorithm;

        /// <summary>
        /// Deterministic edge-falloff policy applied to each tile.
        /// </summary>
        public EdgeFalloff EdgeFalloff;

        /// <summary>
        /// Workspace-owned output field for generated elevation values.
        /// </summary>
        /// <remarks>
        /// The array length is assumed to match <see cref="MapDimensions.Length"/> for
        /// <see cref="Dimensions"/>. The job writes every scheduled index exactly once and
        /// does not read previous elevation values.
        /// </remarks>
        [WriteOnly]
        public NativeArray<int> ElevationValues;

        /// <summary>
        /// Executes elevation generation for one row-major tile index.
        /// </summary>
        /// <param name="index">Row-major tile index.</param>
        /// <remarks>
        /// Unity's job scheduler supplies indices in the range requested by the scheduling
        /// call. This method assumes the scheduled range matches <see cref="ElevationValues"/>.
        /// </remarks>
        public void Execute(int index)
        {
            int tileX = TileIndexUtility.ToX(index, Dimensions.Width);
            int tileY = TileIndexUtility.ToY(index, Dimensions.Width);

            ElevationValues[index] = Algorithm.EvaluateUnchecked(
                tileX,
                tileY,
                Dimensions,
                EdgeFalloff);
        }
    }
}