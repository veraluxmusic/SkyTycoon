using System;
using Lokrain.Burstable.Generation.Pipeline;
using Lokrain.Burstable.Workspace;
using Unity.Jobs;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    /// <summary>
    /// Managed generation stage that builds the primary elevation field.
    /// </summary>
    /// <remarks>
    /// The elevation stage owns the managed boundary for primary elevation generation. It
    /// resolves the workspace-owned elevation field, validates the generation context, creates
    /// the internal deterministic evaluator, and schedules the elevation build job.
    ///
    /// This type does not own native field memory. Field memory belongs to
    /// <see cref="MapWorkspace"/>. This type also does not define generation ordering across
    /// unrelated stages; ordering belongs to the map generation pipeline.
    ///
    /// Jobs and evaluator details are private implementation details of the stage. Public
    /// consumers should depend on stage settings, field contracts, pipeline contracts, and
    /// generated workspace data rather than on internal job or algorithm types.
    /// </remarks>
    public readonly struct ElevationStage : IEquatable<ElevationStage>
    {
        /// <summary>
        /// Creates an elevation generation stage.
        /// </summary>
        /// <param name="settings">Elevation output range settings.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="settings"/> contains an invalid elevation range.
        /// </exception>
        public ElevationStage(ElevationSettings settings)
        {
            settings.Validate();

            Settings = settings;
        }

        /// <summary>
        /// Gets the elevation output range settings used by this stage.
        /// </summary>
        public ElevationSettings Settings { get; }

        /// <summary>
        /// Gets the default elevation generation stage.
        /// </summary>
        public static ElevationStage Default => new(ElevationSettings.Default);

        /// <summary>
        /// Schedules elevation generation work.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <param name="dependency">
        /// Input job dependency that must complete before this stage writes elevation.
        /// </param>
        /// <returns>A job handle representing scheduled elevation generation work.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context, execution settings, dimensions, shaping policy, or stage
        /// settings contain values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the context contains unsupported shaping values, when the elevation
        /// field is missing, when the field has the wrong value type, or when its length does
        /// not match the map tile count.
        /// </exception>
        /// <remarks>
        /// This method schedules work but does not complete it. The caller owns dependency
        /// chaining and must ensure the returned handle completes before reading generated
        /// elevation data or disposing the owning workspace.
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
            context.EdgeFalloff.Validate();

            MapField<int> elevationField = context.Workspace.GetInt32Field(
                ElevationFields.ElevationId);

            elevationField.ValidateLength(context.Length);

            BuildElevationJob job = CreateJob(
                context,
                elevationField);

            return job.Schedule(
                context.Length,
                context.ExecutionSettings.InnerLoopBatchCount,
                dependency);
        }

        /// <summary>
        /// Executes elevation generation and completes it before returning.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the context, execution settings, dimensions, shaping policy, or stage
        /// settings contain values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the context contains unsupported shaping values, when the elevation
        /// field is missing, when the field has the wrong value type, or when its length does
        /// not match the map tile count.
        /// </exception>
        /// <remarks>
        /// This convenience method is appropriate for synchronous generation and editor preview
        /// paths. Pipelines that compose multiple jobs should use <see cref="Schedule"/> and
        /// complete the final dependency explicitly.
        /// </remarks>
        public void Execute(MapGenerationContext context)
        {
            JobHandle handle = Schedule(
                context,
                default(JobHandle));

            handle.Complete();
        }

        /// <summary>
        /// Determines whether this stage is equal to another elevation stage.
        /// </summary>
        /// <param name="other">Other elevation stage.</param>
        /// <returns>
        /// <see langword="true"/> when both stages contain equal settings; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(ElevationStage other)
        {
            return Settings == other.Settings;
        }

        /// <summary>
        /// Determines whether this stage is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this stage.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is an
        /// <see cref="ElevationStage"/> value equal to this value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ElevationStage other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this stage.
        /// </summary>
        /// <returns>A hash code derived from this stage's settings.</returns>
        public override int GetHashCode()
        {
            return Settings.GetHashCode();
        }

        /// <summary>
        /// Determines whether two elevation stages are equal.
        /// </summary>
        /// <param name="left">Left elevation stage.</param>
        /// <param name="right">Right elevation stage.</param>
        /// <returns>
        /// <see langword="true"/> when both values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(ElevationStage left, ElevationStage right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two elevation stages are not equal.
        /// </summary>
        /// <param name="left">Left elevation stage.</param>
        /// <param name="right">Right elevation stage.</param>
        /// <returns>
        /// <see langword="true"/> when the values are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(ElevationStage left, ElevationStage right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Creates the internal elevation build job for the supplied context and field view.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <param name="elevationField">Workspace-owned elevation field view.</param>
        /// <returns>A configured elevation build job.</returns>
        private BuildElevationJob CreateJob(
            MapGenerationContext context,
            MapField<int> elevationField)
        {
            ElevationAlgorithm algorithm = new(
                context.Spec.Seed,
                Settings);

            return new BuildElevationJob
            {
                Dimensions = context.Dimensions,
                Algorithm = algorithm,
                EdgeFalloff = context.EdgeFalloff,
                ElevationValues = elevationField.AsNativeArray()
            };
        }
    }
}