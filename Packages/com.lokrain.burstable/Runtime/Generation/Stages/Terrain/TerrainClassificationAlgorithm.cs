using System;
using Lokrain.Burstable.Tiles;

namespace Lokrain.Burstable.Generation.Stages.Terrain
{
    /// <summary>
    /// Deterministic algorithm for classifying terrain from scalar elevation values.
    /// </summary>
    /// <remarks>
    /// The terrain classification algorithm converts generated elevation values into compact
    /// terrain categories using <see cref="TerrainClassificationSettings"/>.
    ///
    /// This type owns reusable terrain classification behavior. It does not allocate field
    /// storage, generate elevation, sample noise, apply edge falloff, schedule jobs, classify
    /// biomes, build previews, or own workspace lifetime.
    ///
    /// The current terrain contract is intentionally small: elevation values below the configured
    /// sea-level threshold become <see cref="TerrainKind.Water"/>; elevation values greater than
    /// or equal to the threshold become <see cref="TerrainKind.Land"/>.
    /// </remarks>
    public readonly struct TerrainClassificationAlgorithm :
        IEquatable<TerrainClassificationAlgorithm>
    {
        /// <summary>
        /// Creates a deterministic terrain classification algorithm.
        /// </summary>
        /// <param name="settings">Terrain classification settings.</param>
        public TerrainClassificationAlgorithm(TerrainClassificationSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets the terrain classification settings consumed by this algorithm.
        /// </summary>
        public TerrainClassificationSettings Settings { get; }

        /// <summary>
        /// Classifies a scalar elevation value as a terrain kind.
        /// </summary>
        /// <param name="elevation">Generated scalar elevation value.</param>
        /// <returns>The deterministic terrain kind for the supplied elevation value.</returns>
        /// <remarks>
        /// Values below <see cref="TerrainClassificationSettings.SeaLevelElevation"/> are
        /// classified as <see cref="TerrainKind.Water"/>. Values greater than or equal to
        /// <see cref="TerrainClassificationSettings.SeaLevelElevation"/> are classified as
        /// <see cref="TerrainKind.Land"/>.
        /// </remarks>
        public TerrainKind Classify(int elevation)
        {
            return elevation < Settings.SeaLevelElevation
                ? TerrainKind.Water
                : TerrainKind.Land;
        }

        /// <summary>
        /// Classifies a scalar elevation value as a compact unsigned 8-bit terrain kind value.
        /// </summary>
        /// <param name="elevation">Generated scalar elevation value.</param>
        /// <returns>
        /// The deterministic terrain kind value for the supplied elevation value.
        /// </returns>
        /// <remarks>
        /// This method is intended for jobs that write terrain classifications directly into
        /// workspace-owned <see cref="byte"/> field storage.
        /// </remarks>
        public byte ClassifyToByte(int elevation)
        {
            return (byte)Classify(elevation);
        }

        /// <summary>
        /// Determines whether this algorithm is equal to another terrain classification
        /// algorithm.
        /// </summary>
        /// <param name="other">Other terrain classification algorithm.</param>
        /// <returns>
        /// <see langword="true"/> when both algorithms are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(TerrainClassificationAlgorithm other)
        {
            return Settings == other.Settings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is TerrainClassificationAlgorithm other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Settings.GetHashCode();
        }

        /// <summary>
        /// Determines whether two terrain classification algorithms are equal.
        /// </summary>
        /// <param name="left">Left terrain classification algorithm.</param>
        /// <param name="right">Right terrain classification algorithm.</param>
        /// <returns>
        /// <see langword="true"/> when both algorithms are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(
            TerrainClassificationAlgorithm left,
            TerrainClassificationAlgorithm right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two terrain classification algorithms are not equal.
        /// </summary>
        /// <param name="left">Left terrain classification algorithm.</param>
        /// <param name="right">Right terrain classification algorithm.</param>
        /// <returns>
        /// <see langword="true"/> when both algorithms are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(
            TerrainClassificationAlgorithm left,
            TerrainClassificationAlgorithm right)
        {
            return !left.Equals(right);
        }
    }
}