using System;
using Lokrain.Burstable.Workspace;
using Unity.Collections;

namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Base class for managed map generation pipelines.
    /// </summary>
    /// <remarks>
    /// A map generation pipeline owns generation ordering. It validates the deterministic
    /// specification and execution settings, creates the field registry, allocates workspace
    /// storage, creates the generation context, runs the ordered generation work, and returns a
    /// disposable <see cref="MapGenerationResult"/>.
    ///
    /// This type is managed orchestration code. It is not intended to be captured by
    /// Burst-compiled jobs. Concrete pipelines should keep jobs as private implementation
    /// details of stages or algorithms and pass only unmanaged data, native containers, and
    /// Burst-compatible policy values into scheduled work.
    ///
    /// The pipeline does not own terrain semantics by itself. Concrete stages and algorithms own
    /// their domain transformations. The workspace owns native field memory after allocation.
    /// The returned result owns workspace disposal after successful generation.
    /// </remarks>
    public abstract class MapGenerationPipeline
    {
        /// <summary>
        /// Generates a map result from a deterministic specification.
        /// </summary>
        /// <param name="spec">Deterministic map generation specification.</param>
        /// <param name="executionSettings">Runtime execution settings for scheduling work.</param>
        /// <param name="allocator">Allocator used for generated workspace field storage.</param>
        /// <returns>A disposable map generation result containing generated field data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> or <paramref name="executionSettings"/> contains
        /// values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values, when
        /// <paramref name="allocator"/> is invalid for native storage allocation, or when the
        /// concrete pipeline returns an invalid field registry.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when concrete pipeline execution leaves required state invalid.
        /// </exception>
        /// <remarks>
        /// If generation fails after workspace allocation, this method disposes the workspace
        /// before rethrowing. On success, workspace ownership is transferred to the returned
        /// <see cref="MapGenerationResult"/>.
        /// </remarks>
        public MapGenerationResult Generate(
            MapGenerationSpec spec,
            MapGenerationExecutionSettings executionSettings,
            Allocator allocator)
        {
            spec.Validate();
            executionSettings.Validate();

            MapFieldRegistry fieldRegistry = CreateFieldRegistry(spec);

            if (fieldRegistry == null)
            {
                throw new InvalidOperationException(
                    "Map generation pipeline must provide a field registry.");
            }

            MapWorkspace workspace = new(
                spec.Dimensions,
                fieldRegistry,
                allocator);

            try
            {
                MapGenerationContext context = new(
                    spec,
                    executionSettings,
                    workspace);

                Execute(context);
                context.ValidateUsable();

                MapGenerationResult result = new(
                    spec,
                    workspace);

                workspace = null;
                return result;
            }
            finally
            {
                if (workspace != null)
                {
                    workspace.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates the field registry required by this pipeline for the supplied specification.
        /// </summary>
        /// <param name="spec">Deterministic map generation specification.</param>
        /// <returns>The field registry required by this pipeline.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> contains values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values.
        /// </exception>
        /// <remarks>
        /// The returned registry determines which workspace fields are allocated before
        /// generation begins. Concrete pipelines should keep registry construction deterministic
        /// and should not depend on ambient random state, system time, engine random state, or
        /// process-specific state.
        /// </remarks>
        protected abstract MapFieldRegistry CreateFieldRegistry(MapGenerationSpec spec);

        /// <summary>
        /// Executes the ordered generation work for this pipeline.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown by implementations when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the context references a disposed workspace.
        /// </exception>
        /// <remarks>
        /// Implementations own stage ordering. They should validate the context at managed stage
        /// boundaries, resolve required field views before scheduling jobs, and ensure scheduled
        /// work that writes workspace-owned storage is completed or safely represented before
        /// returning.
        /// </remarks>
        protected abstract void Execute(MapGenerationContext context);
    }
}