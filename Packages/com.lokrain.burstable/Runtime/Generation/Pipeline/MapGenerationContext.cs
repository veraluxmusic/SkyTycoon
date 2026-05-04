using System;
using Lokrain.Burstable.Generation.Shaping;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Provides validated generation state to map generation pipeline stages.
    /// </summary>
    /// <remarks>
    /// A generation context carries the deterministic generation specification, execution
    /// settings, derived shaping policies, and workspace reference used during a single
    /// generation run.
    ///
    /// This type does not allocate field storage, own workspace lifetime, schedule jobs, define
    /// generation order, or own field semantics. The workspace owns native field memory. The
    /// pipeline owns stage ordering. Individual stages own their domain transformations.
    ///
    /// This is managed orchestration state and is not intended to be captured by Burst-compiled
    /// jobs. Stages should resolve the required field views and pass native arrays or unmanaged
    /// policy values to jobs.
    /// </remarks>
    public sealed class MapGenerationContext
    {
        private readonly MapWorkspace workspace;

        /// <summary>
        /// Initializes a new generation context.
        /// </summary>
        /// <param name="spec">Deterministic generation specification.</param>
        /// <param name="executionSettings">Runtime execution settings for scheduling work.</param>
        /// <param name="workspace">Workspace containing generation field storage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="workspace"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> or <paramref name="executionSettings"/> contains
        /// values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values, or when
        /// <paramref name="workspace"/> dimensions do not match <paramref name="spec"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="workspace"/> has already been disposed.
        /// </exception>
        /// <remarks>
        /// The context does not take ownership of <paramref name="workspace"/>. Workspace
        /// lifetime remains with the caller, usually a pipeline/facade during generation and a
        /// <see cref="MapGenerationResult"/> after successful generation.
        /// </remarks>
        public MapGenerationContext(
            MapGenerationSpec spec,
            MapGenerationExecutionSettings executionSettings,
            MapWorkspace workspace)
        {
            spec.Validate();
            executionSettings.Validate();

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (workspace.IsDisposed)
            {
                throw new InvalidOperationException(
                    "Map generation context cannot use a disposed workspace.");
            }

            if (workspace.Dimensions != spec.Dimensions)
            {
                throw new ArgumentException(
                    "Map generation context workspace dimensions must match the generation specification dimensions.",
                    nameof(workspace));
            }

            Spec = spec;
            ExecutionSettings = executionSettings;
            EdgeFalloff = EdgeFalloff.FromSettings(spec.ShapeSettings);
            this.workspace = workspace;
        }

        /// <summary>
        /// Gets the deterministic generation specification.
        /// </summary>
        public MapGenerationSpec Spec { get; }

        /// <summary>
        /// Gets the runtime execution settings for scheduling generation work.
        /// </summary>
        /// <remarks>
        /// These settings affect execution strategy only. They must not affect deterministic
        /// generated output.
        /// </remarks>
        public MapGenerationExecutionSettings ExecutionSettings { get; }

        /// <summary>
        /// Gets the deterministic edge-falloff policy derived from the generation specification.
        /// </summary>
        public EdgeFalloff EdgeFalloff { get; }

        /// <summary>
        /// Gets the generated map dimensions.
        /// </summary>
        public MapDimensions Dimensions => Spec.Dimensions;

        /// <summary>
        /// Gets the number of tiles represented by each map field.
        /// </summary>
        public int Length => Spec.Dimensions.Length;

        /// <summary>
        /// Gets the workspace containing generation field storage.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the referenced workspace has been disposed.
        /// </exception>
        /// <remarks>
        /// The returned workspace is not owned by this context. Consumers must not dispose it
        /// through the context unless they own the surrounding generation lifetime.
        /// </remarks>
        public MapWorkspace Workspace
        {
            get
            {
                ThrowIfWorkspaceDisposed();
                return workspace;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the referenced workspace has been disposed.
        /// </summary>
        public bool IsWorkspaceDisposed => workspace.IsDisposed;

        /// <summary>
        /// Validates that this context can still be used by generation stages.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the referenced workspace has been disposed.
        /// </exception>
        /// <remarks>
        /// Call this at managed stage boundaries before resolving field views or scheduling
        /// jobs that use workspace-owned native storage.
        /// </remarks>
        public void ValidateUsable()
        {
            ThrowIfWorkspaceDisposed();
        }

        /// <summary>
        /// Throws when the referenced workspace has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the referenced workspace has been disposed.
        /// </exception>
        private void ThrowIfWorkspaceDisposed()
        {
            if (workspace.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MapWorkspace));
            }
        }
    }
}