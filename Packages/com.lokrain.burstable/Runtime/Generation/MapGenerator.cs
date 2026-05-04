using System;
using Lokrain.Burstable.Generation.Pipeline;
using Unity.Collections;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Provides a public facade for deterministic map generation.
    /// </summary>
    /// <remarks>
    /// A map generator is a thin convenience entry point over a
    /// <see cref="MapGenerationPipeline"/>. It owns no generation algorithms, no stage ordering,
    /// no field definitions, no workspace memory, and no preview logic.
    ///
    /// Pipeline implementations own generation ordering and stage composition. Stages own their
    /// domain transformations and job scheduling details. The returned
    /// <see cref="MapGenerationResult"/> owns generated workspace lifetime.
    ///
    /// This type is intended for package consumers that want a stable high-level API without
    /// constructing pipeline objects directly.
    /// </remarks>
    public sealed class MapGenerator
    {
        private readonly MapGenerationPipeline pipeline;

        /// <summary>
        /// Initializes a new map generator using the standard generation pipeline.
        /// </summary>
        public MapGenerator()
            : this(new StandardMapGenerationPipeline())
        {
        }

        /// <summary>
        /// Initializes a new map generator using a supplied generation pipeline.
        /// </summary>
        /// <param name="pipeline">Generation pipeline used by this generator.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="pipeline"/> is null.
        /// </exception>
        public MapGenerator(MapGenerationPipeline pipeline)
        {
            this.pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        /// <summary>
        /// Gets the generation pipeline used by this generator.
        /// </summary>
        public MapGenerationPipeline Pipeline => pipeline;

        /// <summary>
        /// Generates a map using default deterministic generation and execution settings.
        /// </summary>
        /// <returns>A disposable map generation result containing generated field data.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the configured pipeline or allocator rejects the generated setup.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configured pipeline execution leaves required state invalid.
        /// </exception>
        /// <remarks>
        /// This method uses <see cref="Allocator.Persistent"/> because the returned result may
        /// outlive the current frame or editor callback. Consumers must dispose the returned
        /// result when generated data is no longer needed.
        /// </remarks>
        public MapGenerationResult Generate()
        {
            return Generate(
                MapGenerationSpec.Default,
                MapGenerationExecutionSettings.Default,
                Allocator.Persistent);
        }

        /// <summary>
        /// Generates a map using the supplied deterministic generation specification.
        /// </summary>
        /// <param name="spec">Deterministic map generation specification.</param>
        /// <returns>A disposable map generation result containing generated field data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> contains values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values, or when the
        /// configured pipeline rejects the generated setup.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configured pipeline execution leaves required state invalid.
        /// </exception>
        /// <remarks>
        /// This method uses default execution settings and <see cref="Allocator.Persistent"/>.
        /// Consumers must dispose the returned result when generated data is no longer needed.
        /// </remarks>
        public MapGenerationResult Generate(MapGenerationSpec spec)
        {
            return Generate(
                spec,
                MapGenerationExecutionSettings.Default,
                Allocator.Persistent);
        }

        /// <summary>
        /// Generates a map using the supplied deterministic specification and execution
        /// settings.
        /// </summary>
        /// <param name="spec">Deterministic map generation specification.</param>
        /// <param name="executionSettings">Runtime execution settings for scheduling work.</param>
        /// <returns>A disposable map generation result containing generated field data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="spec"/> or <paramref name="executionSettings"/> contains
        /// values outside supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="spec"/> contains unsupported shaping values, or when the
        /// configured pipeline rejects the generated setup.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configured pipeline execution leaves required state invalid.
        /// </exception>
        /// <remarks>
        /// This method uses <see cref="Allocator.Persistent"/> because the returned result may
        /// outlive the current frame or editor callback. Consumers must dispose the returned
        /// result when generated data is no longer needed.
        /// </remarks>
        public MapGenerationResult Generate(
            MapGenerationSpec spec,
            MapGenerationExecutionSettings executionSettings)
        {
            return Generate(
                spec,
                executionSettings,
                Allocator.Persistent);
        }

        /// <summary>
        /// Generates a map using the supplied deterministic specification, execution settings,
        /// and allocator.
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
        /// configured pipeline rejects the generated setup.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configured pipeline execution leaves required state invalid.
        /// </exception>
        /// <remarks>
        /// The returned result owns generated workspace memory. Consumers must dispose the
        /// result before the allocator lifetime ends.
        /// </remarks>
        public MapGenerationResult Generate(
            MapGenerationSpec spec,
            MapGenerationExecutionSettings executionSettings,
            Allocator allocator)
        {
            return pipeline.Generate(
                spec,
                executionSettings,
                allocator);
        }
    }
}