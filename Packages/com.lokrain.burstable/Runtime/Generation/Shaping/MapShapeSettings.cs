using System;

namespace Lokrain.Burstable.Generation.Shaping
{
    /// <summary>
    /// Immutable shaping settings for deterministic map generation.
    /// </summary>
    /// <remarks>
    /// Map shape settings describe broad spatial constraints applied to generated fields.
    /// They are part of the generation specification and therefore affect deterministic output.
    ///
    /// This type owns configuration invariants only. It does not evaluate falloff curves,
    /// sample noise, classify terrain, allocate memory, or schedule jobs.
    /// </remarks>
    public readonly struct MapShapeSettings : IEquatable<MapShapeSettings>
    {
        /// <summary>
        /// Default edge-falloff distance in tiles.
        /// </summary>
        public const int DefaultEdgeFalloffDistanceInTiles = 32;

        /// <summary>
        /// Minimum supported edge-falloff distance in tiles when edge falloff is enabled.
        /// </summary>
        public const int MinimumEnabledEdgeFalloffDistanceInTiles = 1;

        /// <summary>
        /// Creates immutable map-shaping settings.
        /// </summary>
        /// <param name="edgeFalloffMode">
        /// Deterministic edge-falloff mode applied near rectangular map borders.
        /// </param>
        /// <param name="edgeFalloffDistanceInTiles">
        /// Distance from each rectangular map border over which edge falloff is applied.
        /// </param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// these settings to build a generation specification or schedule generation work.
        ///
        /// When <paramref name="edgeFalloffMode"/> is <see cref="EdgeFalloffMode.None"/>,
        /// <paramref name="edgeFalloffDistanceInTiles"/> must be zero so disabled falloff has
        /// one canonical representation.
        /// </remarks>
        public MapShapeSettings(
            EdgeFalloffMode edgeFalloffMode,
            int edgeFalloffDistanceInTiles)
        {
            EdgeFalloffMode = edgeFalloffMode;
            EdgeFalloffDistanceInTiles = edgeFalloffDistanceInTiles;
        }

        /// <summary>
        /// Gets the deterministic edge-falloff mode applied near rectangular map borders.
        /// </summary>
        public EdgeFalloffMode EdgeFalloffMode { get; }

        /// <summary>
        /// Gets the distance from each rectangular map border over which edge falloff is applied.
        /// </summary>
        /// <remarks>
        /// A value of zero is valid only when <see cref="EdgeFalloffMode"/> is
        /// <see cref="Shaping.EdgeFalloffMode.None"/>.
        /// </remarks>
        public int EdgeFalloffDistanceInTiles { get; }

        /// <summary>
        /// Gets default map-shaping settings suitable for first-run generation and previews.
        /// </summary>
        public static MapShapeSettings Default => new(
            EdgeFalloffMode.Linear,
            DefaultEdgeFalloffDistanceInTiles);

        /// <summary>
        /// Gets map-shaping settings with edge falloff disabled.
        /// </summary>
        public static MapShapeSettings None => new(
            EdgeFalloffMode.None,
            0);

        /// <summary>
        /// Validates that these settings can be used for deterministic map shaping.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="EdgeFalloffDistanceInTiles"/> is invalid for the selected
        /// <see cref="EdgeFalloffMode"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="EdgeFalloffMode"/> is not a supported value.
        /// </exception>
        public void Validate()
        {
            if (!IsSupportedEdgeFalloffMode(EdgeFalloffMode))
            {
                throw new ArgumentException(
                    "Unsupported edge falloff mode.",
                    nameof(EdgeFalloffMode));
            }

            if (EdgeFalloffMode == EdgeFalloffMode.None)
            {
                if (EdgeFalloffDistanceInTiles != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(EdgeFalloffDistanceInTiles),
                        EdgeFalloffDistanceInTiles,
                        "Edge falloff distance must be zero when edge falloff is disabled.");
                }

                return;
            }

            if (EdgeFalloffDistanceInTiles < MinimumEnabledEdgeFalloffDistanceInTiles)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(EdgeFalloffDistanceInTiles),
                    EdgeFalloffDistanceInTiles,
                    "Edge falloff distance must be positive when edge falloff is enabled.");
            }
        }

        /// <summary>
        /// Determines whether these settings are equal to another settings value.
        /// </summary>
        /// <param name="other">Other settings value.</param>
        /// <returns>
        /// <see langword="true"/> when both settings values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapShapeSettings other)
        {
            return EdgeFalloffMode == other.EdgeFalloffMode &&
                   EdgeFalloffDistanceInTiles == other.EdgeFalloffDistanceInTiles;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapShapeSettings other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)EdgeFalloffMode * 397) ^ EdgeFalloffDistanceInTiles;
            }
        }

        /// <summary>
        /// Determines whether two map-shaping settings values are equal.
        /// </summary>
        public static bool operator ==(MapShapeSettings left, MapShapeSettings right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two map-shaping settings values are not equal.
        /// </summary>
        public static bool operator !=(MapShapeSettings left, MapShapeSettings right)
        {
            return !left.Equals(right);
        }

        private static bool IsSupportedEdgeFalloffMode(EdgeFalloffMode edgeFalloffMode)
        {
            return edgeFalloffMode == EdgeFalloffMode.None ||
                   edgeFalloffMode == EdgeFalloffMode.Linear;
        }
    }
}