using System;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Owns the generated output of a map generation run.
    /// </summary>
    /// <remarks>
    /// A generation result pairs the deterministic generation specification with the
    /// workspace containing generated field data.
    ///
    /// This type owns the lifetime of the supplied <see cref="MapWorkspace"/>. Consumers may
    /// read from or write to the workspace while the result is alive, but they should dispose
    /// the result rather than disposing the workspace directly.
    ///
    /// This type does not schedule jobs, allocate fields, define generation order, or own
    /// field semantics. Generation order belongs to the pipeline. Field storage belongs to
    /// the workspace. Field meaning belongs to the stages and consumers that define the field
    /// contracts.
    /// </remarks>
    public sealed class MapGenerationResult : IDisposable
    {
        private readonly MapWorkspace workspace;

        private bool isDisposed;

        /// <summary>
        /// Initializes a new map generation result and takes ownership of the supplied
        /// workspace.
        /// </summary>
        /// <param name="spec">Deterministic specification used to generate the map.</param>
        /// <param name="workspace">Workspace containing generated map field data.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="workspace"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> contains invalid dimensions or shaping values.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values, or when
        /// <paramref name="workspace"/> dimensions do not match the specification dimensions.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="workspace"/> has already been disposed.
        /// </exception>
        /// <remarks>
        /// Ownership transfers only after this constructor completes successfully. If validation
        /// fails, the caller remains responsible for disposing <paramref name="workspace"/>.
        /// </remarks>
        public MapGenerationResult(
            MapGenerationSpec spec,
            MapWorkspace workspace)
        {
            spec.Validate();

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (workspace.IsDisposed)
            {
                throw new InvalidOperationException(
                    "Map generation result cannot take ownership of a disposed workspace.");
            }

            if (workspace.Dimensions != spec.Dimensions)
            {
                throw new ArgumentException(
                    "Map generation result workspace dimensions must match the generation specification dimensions.",
                    nameof(workspace));
            }

            Spec = spec;
            this.workspace = workspace;
        }

        /// <summary>
        /// Gets the deterministic specification used to generate the map.
        /// </summary>
        public MapGenerationSpec Spec { get; }

        /// <summary>
        /// Gets the workspace containing generated map field data.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this result has been disposed.
        /// </exception>
        /// <remarks>
        /// The returned workspace is owned by this result. Consumers should not dispose it
        /// directly unless they intentionally take over ownership outside the normal result
        /// lifetime contract.
        /// </remarks>
        public MapWorkspace Workspace
        {
            get
            {
                ThrowIfDisposed();
                return workspace;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this result has been disposed.
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// Disposes the generated workspace owned by this result.
        /// </summary>
        /// <remarks>
        /// Existing workspace references, field views, and native arrays obtained from this
        /// result become invalid after disposal. Consumers must ensure no scheduled jobs are
        /// still reading from or writing to workspace-owned storage before disposing the result.
        /// </remarks>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            workspace.Dispose();
            isDisposed = true;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throws when this result has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this result has been disposed.
        /// </exception>
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(MapGenerationResult));
            }
        }
    }
}