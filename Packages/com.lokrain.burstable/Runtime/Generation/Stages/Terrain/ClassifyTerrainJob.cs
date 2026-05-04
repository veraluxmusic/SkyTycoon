using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Terrain
{
    /// <summary>
    /// Classifies terrain kinds from generated elevation values.
    /// </summary>
    /// <remarks>
    /// This job is an implementation detail of the terrain classification stage. It parallelizes
    /// <see cref="TerrainClassificationAlgorithm"/> over contiguous row-major map fields.
    ///
    /// The job does not own terrain policy. Terrain classification behavior belongs to
    /// <see cref="TerrainClassificationAlgorithm"/>. The job only reads generated elevation
    /// values and writes compact terrain kind values to workspace-owned storage.
    ///
    /// The caller owns dependency management and must ensure <see cref="ElevationValues"/> and
    /// <see cref="TerrainKindValues"/> remain valid until the scheduled job has completed.
    /// </remarks>
    [BurstCompile]
    internal struct ClassifyTerrainJob : IJobParallelFor
    {
        /// <summary>
        /// Workspace-owned input field containing generated scalar elevation values.
        /// </summary>
        /// <remarks>
        /// The array length is assumed to match the generated map tile count.
        /// </remarks>
        [ReadOnly]
        public NativeArray<int> ElevationValues;

        /// <summary>
        /// Deterministic terrain classification algorithm evaluated for each tile.
        /// </summary>
        public TerrainClassificationAlgorithm Algorithm;

        /// <summary>
        /// Workspace-owned output field for compact terrain kind values.
        /// </summary>
        /// <remarks>
        /// Values written by this job correspond to the numeric values of
        /// <see cref="Lokrain.Burstable.Tiles.TerrainKind"/>.
        /// </remarks>
        public NativeArray<byte> TerrainKindValues;

        /// <summary>
        /// Executes terrain classification for one row-major tile index.
        /// </summary>
        /// <param name="index">Row-major tile index.</param>
        /// <remarks>
        /// Unity's job scheduler supplies indices in the range requested by the scheduling
        /// call. This method assumes the scheduled range matches both input and output field
        /// lengths.
        /// </remarks>
        public void Execute(int index)
        {
            TerrainKindValues[index] = Algorithm.ClassifyToByte(
                ElevationValues[index]);
        }
    }
}