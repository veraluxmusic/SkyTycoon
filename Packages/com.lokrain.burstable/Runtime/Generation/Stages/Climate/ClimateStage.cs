using System;
using Lokrain.Burstable.Generation.Pipeline;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Workspace;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Climate
{
    /// <summary>
    /// Managed generation stage that builds the primary climate fields.
    /// </summary>
    /// <remarks>
    /// The climate stage owns the climate transformation boundary. It resolves the
    /// workspace-owned elevation, temperature, and moisture fields, then schedules the internal
    /// climate build job.
    ///
    /// This type does not own native field memory. Field memory belongs to
    /// <see cref="MapWorkspace"/>. This type also does not define generation ordering across
    /// unrelated stages; ordering belongs to the map generation pipeline.
    ///
    /// Jobs are private implementation details of the stage. Public consumers should depend on
    /// stage settings, field contracts, pipeline contracts, and generated workspace data rather
    /// than on job types.
    /// </remarks>
    public readonly struct ClimateStage : IEquatable<ClimateStage>
    {
        /// <summary>
        /// Creates a climate generation stage.
        /// </summary>
        /// <param name="settings">Climate scalar generation settings.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="settings"/> contains invalid climate values.
        /// </exception>
        public ClimateStage(ClimateSettings settings)
        {
            settings.Validate();

            Settings = settings;
        }

        /// <summary>
        /// Gets the climate scalar generation settings used by this stage.
        /// </summary>
        public ClimateSettings Settings { get; }

        /// <summary>
        /// Gets the default climate generation stage.
        /// </summary>
        public static ClimateStage Default => new(ClimateSettings.Default);

        /// <summary>
        /// Schedules climate generation work.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <param name="dependency">
        /// Input job dependency that must complete before this stage reads elevation and writes
        /// climate fields.
        /// </param>
        /// <returns>A job handle representing scheduled climate generation work.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context, execution settings, dimensions, or stage settings contain
        /// values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the elevation, temperature, or moisture field is missing, when a field has
        /// the wrong value type, or when its length does not match the map tile count.
        /// </exception>
        /// <remarks>
        /// This method schedules work but does not complete it. The caller owns dependency
        /// chaining and must ensure the returned handle completes before reading generated climate
        /// data or disposing the owning workspace.
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
            Settings.Validate();

            MapField<int> elevationField = context.Workspace.GetInt32Field(
                ElevationFields.ElevationId);

            MapField<int> temperatureField = context.Workspace.GetInt32Field(
                ClimateFields.TemperatureId);

            MapField<int> moistureField = context.Workspace.GetInt32Field(
                ClimateFields.MoistureId);

            elevationField.ValidateLength(context.Length);
            temperatureField.ValidateLength(context.Length);
            moistureField.ValidateLength(context.Length);

            BuildClimateJob job = CreateJob(
                elevationField,
                temperatureField,
                moistureField);

            return job.Schedule(
                context.Length,
                context.ExecutionSettings.InnerLoopBatchCount,
                dependency);
        }

        /// <summary>
        /// Executes climate generation and completes it before returning.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context, execution settings, dimensions, or stage settings contain
        /// values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the elevation, temperature, or moisture field is missing, when a field has
        /// the wrong value type, or when its length does not match the map tile count.
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
                default);

            handle.Complete();
        }

        /// <summary>
        /// Determines whether this stage is equal to another climate stage.
        /// </summary>
        /// <param name="other">Other climate stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(ClimateStage other)
        {
            return Settings == other.Settings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ClimateStage other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Settings.GetHashCode();
        }

        /// <summary>
        /// Determines whether two climate stages are equal.
        /// </summary>
        /// <param name="left">Left climate stage.</param>
        /// <param name="right">Right climate stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(ClimateStage left, ClimateStage right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two climate stages are not equal.
        /// </summary>
        /// <param name="left">Left climate stage.</param>
        /// <param name="right">Right climate stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(ClimateStage left, ClimateStage right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Creates the internal climate build job for the supplied field views.
        /// </summary>
        /// <param name="elevationField">Workspace-owned elevation field view.</param>
        /// <param name="temperatureField">Workspace-owned temperature field view.</param>
        /// <param name="moistureField">Workspace-owned moisture field view.</param>
        /// <returns>A configured climate build job.</returns>
        private BuildClimateJob CreateJob(
            MapField<int> elevationField,
            MapField<int> temperatureField,
            MapField<int> moistureField)
        {
            return new BuildClimateJob
            {
                Settings = Settings,
                ElevationValues = elevationField.AsNativeArray(),
                TemperatureValues = temperatureField.AsNativeArray(),
                MoistureValues = moistureField.AsNativeArray()
            };
        }
    }
}