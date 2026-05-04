using System;

namespace Lokrain.Burstable.Generation.Stages.Terrain
{
    /// <summary>
    /// Immutable settings for deterministic terrain classification.
    /// </summary>
    /// <remarks>
    /// Terrain classification settings define how generated elevation values are converted into
    /// broad terrain categories.
    ///
    /// This type owns terrain classification policy only. It does not allocate storage, generate
    /// elevation, sample noise, apply map shaping, schedule jobs, classify biomes, or own
    /// workspace lifetime.
    ///
    /// The sea-level threshold is intentionally owned here rather than by elevation generation.
    /// Elevation generation produces scalar values. Terrain classification decides how those
    /// scalar values map to water or land.
    /// </remarks>
    public readonly struct TerrainClassificationSettings : IEquatable<TerrainClassificationSettings>
    {
        /// <summary>
        /// Default sea-level elevation threshold.
        /// </summary>
        public const int DefaultSeaLevelElevation = 32768;

        /// <summary>
        /// Creates immutable terrain classification settings.
        /// </summary>
        /// <param name="seaLevelElevation">
        /// Elevation threshold below which tiles are classified as water.
        /// </param>
        /// <remarks>
        /// All signed 32-bit threshold values are deterministic and supported. Thresholds outside
        /// the generated elevation range are valid, but they may intentionally classify all tiles
        /// as water or all tiles as land.
        /// </remarks>
        public TerrainClassificationSettings(int seaLevelElevation)
        {
            SeaLevelElevation = seaLevelElevation;
        }

        /// <summary>
        /// Gets the elevation threshold below which tiles are classified as water.
        /// </summary>
        /// <remarks>
        /// Tiles with elevation values less than this threshold are classified as water. Tiles
        /// with elevation values greater than or equal to this threshold are classified as land.
        /// </remarks>
        public int SeaLevelElevation { get; }

        /// <summary>
        /// Gets default terrain classification settings suitable for first-run generation and
        /// previews.
        /// </summary>
        public static TerrainClassificationSettings Default => new(
            DefaultSeaLevelElevation);

        /// <summary>
        /// Determines whether these settings are equal to another settings value.
        /// </summary>
        /// <param name="other">Other terrain classification settings.</param>
        /// <returns>
        /// <see langword="true"/> when both settings values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(TerrainClassificationSettings other)
        {
            return SeaLevelElevation == other.SeaLevelElevation;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TerrainClassificationSettings other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return SeaLevelElevation;
        }

        /// <summary>
        /// Determines whether two terrain classification settings values are equal.
        /// </summary>
        /// <param name="left">Left terrain classification settings value.</param>
        /// <param name="right">Right terrain classification settings value.</param>
        /// <returns>
        /// <see langword="true"/> when both values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(
            TerrainClassificationSettings left,
            TerrainClassificationSettings right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two terrain classification settings values are not equal.
        /// </summary>
        /// <param name="left">Left terrain classification settings value.</param>
        /// <param name="right">Right terrain classification settings value.</param>
        /// <returns>
        /// <see langword="true"/> when both values are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(
            TerrainClassificationSettings left,
            TerrainClassificationSettings right)
        {
            return !left.Equals(right);
        }
    }
}