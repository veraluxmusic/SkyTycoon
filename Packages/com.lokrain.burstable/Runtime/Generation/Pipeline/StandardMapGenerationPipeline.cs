using System;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Generation.Stages.Terrain;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Default managed map generation pipeline.
    /// </summary>
    /// <remarks>
    /// The standard pipeline owns the default generation ordering for core map fields. It
    /// declares the workspace fields required by its stages, allocates those fields through the
    /// base pipeline flow, and executes stages in deterministic order.
    ///
    /// This type is managed orchestration code. It is not intended to be captured by
    /// Burst-compiled jobs. Individual stages own their algorithms and job scheduling details.
    ///
    /// The current standard pipeline generates scalar elevation first, then classifies terrain
    /// from that elevation. Additional stages should be added here only after their field
    /// contracts, settings, algorithms, and jobs exist.
    /// </remarks>
    public sealed class StandardMapGenerationPipeline : MapGenerationPipeline
    {
        /// <summary>
        /// Initializes a new standard map generation pipeline with default stage settings.
        /// </summary>
        public StandardMapGenerationPipeline()
            : this(
                ElevationStage.Default,
                TerrainClassificationStage.Default)
        {
        }

        /// <summary>
        /// Initializes a new standard map generation pipeline.
        /// </summary>
        /// <param name="elevationStage">Elevation generation stage.</param>
        /// <param name="terrainClassificationStage">Terrain classification stage.</param>
        public StandardMapGenerationPipeline(
            ElevationStage elevationStage,
            TerrainClassificationStage terrainClassificationStage)
        {
            ElevationStage = elevationStage;
            TerrainClassificationStage = terrainClassificationStage;
        }

        /// <summary>
        /// Gets the elevation generation stage used by this pipeline.
        /// </summary>
        public ElevationStage ElevationStage { get; }

        /// <summary>
        /// Gets the terrain classification stage used by this pipeline.
        /// </summary>
        public TerrainClassificationStage TerrainClassificationStage { get; }

        /// <summary>
        /// Creates the field registry required by the standard pipeline.
        /// </summary>
        /// <param name="spec">Deterministic map generation specification.</param>
        /// <returns>The field registry required by this pipeline.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> contains dimensions or shaping values outside
        /// supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values.
        /// </exception>
        /// <remarks>
        /// The returned registry defines the workspace storage allocated before any stage
        /// executes. Field order is deterministic and should remain stable unless the pipeline
        /// contract intentionally changes.
        /// </remarks>
        protected override MapFieldRegistry CreateFieldRegistry(MapGenerationSpec spec)
        {
            spec.Validate();

            return new MapFieldRegistry(
                new[]
                {
                    ElevationFields.Elevation,
                    TerrainClassificationFields.TerrainKind
                });
        }

        /// <summary>
        /// Executes standard map generation stages in deterministic order.
        /// </summary>
        /// <param name="context">Validated generation context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when <paramref name="context"/> references a disposed workspace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when stage settings or context values are outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when required workspace fields are missing, have the wrong value type, or have
        /// invalid lengths.
        /// </exception>
        /// <remarks>
        /// Stage execution order is part of this pipeline's deterministic output contract.
        /// Terrain classification must run after elevation because it consumes the elevation
        /// field.
        /// </remarks>
        protected override void Execute(MapGenerationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ValidateUsable();

            ElevationStage.Execute(context);
            TerrainClassificationStage.Execute(context);

            context.ValidateUsable();
        }
    }
}