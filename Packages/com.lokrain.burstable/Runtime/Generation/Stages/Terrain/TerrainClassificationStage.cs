using System;
using Lokrain.Burstable.Generation.Pipeline;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Workspace;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Terrain
{
    /// <summary>
    /// Managed generation stage that classifies terrain kinds from generated elevation values.
    /// </summary>
    /// <remarks>
    /// The terrain classification stage owns the terrain transformation boundary. It resolves
    /// the workspace-owned elevation input field and terrain-kind output field, constructs the
    /// deterministic <see cref="TerrainClassificationAlgorithm"/>, and schedules the internal
    /// terrain classification job.
    ///
    /// This type does not own native field memory. Field memory belongs to
    /// <see cref="MapWorkspace"/>. This type also does not define generation ordering across
    /// unrelated stages; ordering belongs to the map generation pipeline.
    ///
    /// Jobs are private implementation details of the stage. Public consumers should depend on
    /// stage settings, field contracts, pipeline contracts, and generated workspace data rather
    /// than on job types.
    /// </remarks>
    public readonly struct TerrainClassificationStage : IEquatable<TerrainClassificationStage>
    {
        /// <summary>
        /// Creates a terrain classification stage.
        /// </summary>
        /// <param name="settings">Terrain classification settings.</param>
        public TerrainClassificationStage(TerrainClassificationSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets the terrain classification settings used by this stage.
        /// </summary>
        public TerrainClassificationSettings Settings { get; }

        /// <summary>
        /// Gets the default terrain classification stage.
        /// </summary>
        public static TerrainClassificationStage Default => new(
            TerrainClassificationSettings.Default);

        /// <summary>
        /// Schedules terrain classification work.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <param name="dependency">
        /// Input job dependency that must complete before this stage reads elevation or writes
        /// terrain kind values.
        /// </param>
        /// <returns>A job handle representing scheduled terrain classification work.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context or execution settings contain values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when required workspace fields are missing, have the wrong value type, or have
        /// lengths that do not match the map tile count.
        /// </exception>
        /// <remarks>
        /// This method schedules work but does not complete it. The caller owns dependency
        /// chaining and must ensure the returned handle completes before reading generated
        /// terrain data or disposing the owning workspace.
        /// </remarks>
        public JobHandle Schedule(
            MapGenerationContext context,
            JobHandle dependency)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ValidateUsable();

            MapWorkspace workspace = context.Workspace;

            MapField<int> elevationField = workspace.GetInt32Field(
                ElevationFields.ElevationId);

            MapField<byte> terrainKindField = workspace.GetUInt8Field(
                TerrainClassificationFields.TerrainKindId);

            elevationField.ValidateLength(context.Length);
            terrainKindField.ValidateLength(context.Length);

            ClassifyTerrainJob job = CreateJob(
                elevationField,
                terrainKindField);

            return job.Schedule(
                context.Length,
                context.ExecutionSettings.InnerLoopBatchCount,
                dependency);
        }

        /// <summary>
        /// Executes terrain classification and completes it before returning.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context or execution settings contain values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when required workspace fields are missing, have the wrong value type, or have
        /// lengths that do not match the map tile count.
        /// </exception>
        /// <remarks>
        /// This convenience method is appropriate for the initial synchronous pipeline. More
        /// advanced pipelines can use <see cref="Schedule"/> to compose dependencies across
        /// multiple stages.
        /// </remarks>
        public void Execute(MapGenerationContext context)
        {
            JobHandle handle = Schedule(
                context,
                default(JobHandle));

            handle.Complete();
        }

        /// <summary>
        /// Determines whether this stage is equal to another terrain classification stage.
        /// </summary>
        /// <param name="other">Other terrain classification stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(TerrainClassificationStage other)
        {
            return Settings == other.Settings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TerrainClassificationStage other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Settings.GetHashCode();
        }

        /// <summary>
        /// Determines whether two terrain classification stages are equal.
        /// </summary>
        /// <param name="left">Left terrain classification stage.</param>
        /// <param name="right">Right terrain classification stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(
            TerrainClassificationStage left,
            TerrainClassificationStage right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two terrain classification stages are not equal.
        /// </summary>
        /// <param name="left">Left terrain classification stage.</param>
        /// <param name="right">Right terrain classification stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(
            TerrainClassificationStage left,
            TerrainClassificationStage right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Creates the internal terrain classification job for the supplied field views.
        /// </summary>
        /// <param name="elevationField">Workspace-owned elevation input field view.</param>
        /// <param name="terrainKindField">Workspace-owned terrain-kind output field view.</param>
        /// <returns>A configured terrain classification job.</returns>
        private ClassifyTerrainJob CreateJob(
            MapField<int> elevationField,
            MapField<byte> terrainKindField)
        {
            TerrainClassificationAlgorithm algorithm = new(
                Settings);

            return new ClassifyTerrainJob
            {
                ElevationValues = elevationField.AsNativeArray(),
                Algorithm = algorithm,
                TerrainKindValues = terrainKindField.AsNativeArray()
            };
        }
    }
}