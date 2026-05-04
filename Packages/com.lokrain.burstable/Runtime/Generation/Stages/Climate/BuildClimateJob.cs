using Lokrain.Burstable.Workspace;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Climate
{
    /// <summary>
    /// Builds deterministic climate fields for the configured map.
    /// </summary>
    /// <remarks>
    /// This job is an implementation detail of <see cref="ClimateStage"/>. Public consumers
    /// should depend on climate settings, field contracts, stage contracts, and generated
    /// workspace data rather than on this job type.
    ///
    /// The job reads the workspace-owned elevation field and writes workspace-owned temperature
    /// and moisture fields. It does not allocate native memory, resolve workspace fields,
    /// validate field metadata, own workspace lifetime, or define generation ordering.
    ///
    /// Each job index maps to one tile in row-major workspace order. The job does not require
    /// tile coordinates because the current climate algorithm is scalar and elevation-driven.
    /// </remarks>
    [BurstCompile]
    internal struct BuildClimateJob : IJobParallelFor
    {
        /// <summary>
        /// Climate scalar calculation settings.
        /// </summary>
        public ClimateSettings Settings;

        /// <summary>
        /// Workspace-owned elevation values consumed by climate generation.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> ElevationValues;

        /// <summary>
        /// Workspace-owned temperature values produced by climate generation.
        /// </summary>
        public NativeArray<int> TemperatureValues;

        /// <summary>
        /// Workspace-owned moisture values produced by climate generation.
        /// </summary>
        public NativeArray<int> MoistureValues;

        /// <summary>
        /// Generates climate values for one tile.
        /// </summary>
        /// <param name="index">Zero-based row-major tile index.</param>
        public void Execute(int index)
        {
            int elevation = ElevationValues[index];

            TemperatureValues[index] = ClimateAlgorithm.CalculateTemperature(
                elevation,
                Settings);

            MoistureValues[index] = ClimateAlgorithm.CalculateMoisture(Settings);
        }
    }
}