using System;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Immutable deterministic seed used by map generation.
    /// </summary>
    /// <remarks>
    /// This value object defines the canonical seed representation consumed by generation
    /// specifications and algorithms.
    ///
    /// A seed is deterministic when the same explicit value is supplied to generation. This
    /// type prevents generation APIs from depending on ambient random sources, system time,
    /// engine random state, or process-specific state.
    ///
    /// The package cannot prove how a caller originally selected a seed. It can guarantee that
    /// generation receives a stable, explicit, value-based seed contract.
    /// </remarks>
    public readonly struct MapGenerationSeed : IEquatable<MapGenerationSeed>
    {
        /// <summary>
        /// Default seed value used by first-run generation and tests.
        /// </summary>
        public const uint DefaultValue = 1u;

        /// <summary>
        /// Creates an immutable map-generation seed.
        /// </summary>
        /// <param name="value">Explicit deterministic seed value.</param>
        public MapGenerationSeed(uint value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the explicit deterministic seed value.
        /// </summary>
        public uint Value { get; }

        /// <summary>
        /// Gets the default deterministic map-generation seed.
        /// </summary>
        public static MapGenerationSeed Default => new(DefaultValue);

        /// <summary>
        /// Gets a zero-valued deterministic map-generation seed.
        /// </summary>
        /// <remarks>
        /// Zero is a valid explicit seed. Algorithms must handle it deterministically.
        /// </remarks>
        public static MapGenerationSeed Zero => new(0u);

        /// <summary>
        /// Determines whether this seed is equal to another seed.
        /// </summary>
        /// <param name="other">Other seed value.</param>
        /// <returns>
        /// <see langword="true"/> when both seed values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapGenerationSeed other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapGenerationSeed other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        /// <summary>
        /// Returns the seed value as an unsigned invariant-culture string.
        /// </summary>
        /// <returns>The seed value as a string.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Determines whether two seed values are equal.
        /// </summary>
        public static bool operator ==(MapGenerationSeed left, MapGenerationSeed right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two seed values are not equal.
        /// </summary>
        public static bool operator !=(MapGenerationSeed left, MapGenerationSeed right)
        {
            return !left.Equals(right);
        }
    }
}