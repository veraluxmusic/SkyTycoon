using System;
using Lokrain.Burstable.Generation.Shaping;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Immutable deterministic specification for map generation.
    /// </summary>
    /// <remarks>
    /// The generation specification describes what map should be generated. It owns
    /// deterministic input data such as seed, dimensions, and shaping settings.
    ///
    /// Runtime scheduling concerns belong in <see cref="MapGenerationExecutionSettings"/>,
    /// not in this type. Changing execution settings must not change deterministic output.
    ///
    /// This type does not allocate workspace memory, schedule jobs, sample noise, classify
    /// terrain, or build preview data.
    /// </remarks>
    public readonly struct MapGenerationSpec : IEquatable<MapGenerationSpec>
    {
        /// <summary>
        /// Creates an immutable deterministic generation specification.
        /// </summary>
        /// <param name="seed">
        /// Explicit deterministic seed consumed by generation algorithms.
        /// </param>
        /// <param name="dimensions">
        /// Dimensions of the generated rectangular tile map.
        /// </param>
        /// <param name="shapeSettings">
        /// Map-shaping settings applied during generation.
        /// </param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// the specification to allocate workspaces or execute generation.
        /// </remarks>
        public MapGenerationSpec(
            MapGenerationSeed seed,
            MapDimensions dimensions,
            MapShapeSettings shapeSettings)
        {
            Seed = seed;
            Dimensions = dimensions;
            ShapeSettings = shapeSettings;
        }

        /// <summary>
        /// Gets the explicit deterministic seed consumed by generation algorithms.
        /// </summary>
        /// <remarks>
        /// The seed is deterministic because generation receives it as explicit value data.
        /// The package does not derive it from ambient random state, system time, engine random
        /// state, or process-specific state.
        /// </remarks>
        public MapGenerationSeed Seed { get; }

        /// <summary>
        /// Gets the dimensions of the generated rectangular tile map.
        /// </summary>
        public MapDimensions Dimensions { get; }

        /// <summary>
        /// Gets the map-shaping settings applied during generation.
        /// </summary>
        public MapShapeSettings ShapeSettings { get; }

        /// <summary>
        /// Gets the default deterministic generation specification.
        /// </summary>
        public static MapGenerationSpec Default => new(
            MapGenerationSeed.Default,
            MapDimensions.Default,
            MapShapeSettings.Default);

        /// <summary>
        /// Validates that this specification can be used for deterministic map generation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when dimensions or shaping values are outside their supported ranges.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the specification contains an unsupported shaping mode.
        /// </exception>
        public void Validate()
        {
            Dimensions.Validate();
            ShapeSettings.Validate();
        }

        /// <summary>
        /// Determines whether this specification is equal to another specification.
        /// </summary>
        /// <param name="other">Other generation specification.</param>
        /// <returns>
        /// <see langword="true"/> when both specifications are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapGenerationSpec other)
        {
            return Seed == other.Seed &&
                   Dimensions == other.Dimensions &&
                   ShapeSettings == other.ShapeSettings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapGenerationSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Seed.GetHashCode();
                hashCode = (hashCode * 397) ^ Dimensions.GetHashCode();
                hashCode = (hashCode * 397) ^ ShapeSettings.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two generation specifications are equal.
        /// </summary>
        public static bool operator ==(MapGenerationSpec left, MapGenerationSpec right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two generation specifications are not equal.
        /// </summary>
        public static bool operator !=(MapGenerationSpec left, MapGenerationSpec right)
        {
            return !left.Equals(right);
        }
    }
}