using System;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    /// <summary>
    /// Immutable settings for deterministic elevation generation.
    /// </summary>
    /// <remarks>
    /// Elevation settings define the numeric output contract for the elevation stage.
    /// Generated elevation values are signed 32-bit integer scalar values stored in the
    /// primary elevation field.
    ///
    /// This type owns configuration invariants only. It does not allocate storage, sample
    /// noise, apply edge falloff, classify terrain, schedule jobs, or define water/land
    /// semantics.
    ///
    /// Terrain thresholds such as sea level belong to terrain classification settings, not to
    /// elevation settings.
    /// </remarks>
    public readonly struct ElevationSettings : IEquatable<ElevationSettings>
    {
        /// <summary>
        /// Default minimum generated elevation value.
        /// </summary>
        public const int DefaultMinimumElevation = 0;

        /// <summary>
        /// Default maximum generated elevation value.
        /// </summary>
        /// <remarks>
        /// The default range is inclusive, so the default settings produce values from
        /// <c>0</c> through <c>65535</c>, giving exactly 65,536 possible scalar elevation
        /// values.
        /// </remarks>
        public const int DefaultMaximumElevation = 65535;

        /// <summary>
        /// Minimum supported difference between maximum and minimum elevation.
        /// </summary>
        /// <remarks>
        /// A range of <c>1</c> means the generated domain contains at least two possible
        /// values: the configured minimum and the configured maximum.
        /// </remarks>
        public const int MinimumElevationRange = 1;

        /// <summary>
        /// Creates immutable elevation generation settings.
        /// </summary>
        /// <param name="minimumElevation">Minimum generated elevation value.</param>
        /// <param name="maximumElevation">Maximum generated elevation value.</param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// these settings to construct generation specifications, algorithms, stages, or jobs.
        /// </remarks>
        public ElevationSettings(
            int minimumElevation,
            int maximumElevation)
        {
            MinimumElevation = minimumElevation;
            MaximumElevation = maximumElevation;
        }

        /// <summary>
        /// Gets the minimum generated elevation value.
        /// </summary>
        public int MinimumElevation { get; }

        /// <summary>
        /// Gets the maximum generated elevation value.
        /// </summary>
        public int MaximumElevation { get; }

        /// <summary>
        /// Gets the difference between maximum and minimum generated elevation.
        /// </summary>
        /// <remarks>
        /// This value is returned as a signed 64-bit integer so callers can inspect invalid
        /// settings without causing signed 32-bit overflow. Valid generation settings still
        /// require this range to fit in a signed 32-bit integer.
        ///
        /// This is not the number of possible generated values. Because the elevation range is
        /// inclusive, the possible generated value count is <c>ElevationRange + 1</c> after
        /// validation has passed.
        /// </remarks>
        public long ElevationRange => (long)MaximumElevation - MinimumElevation;

        /// <summary>
        /// Gets default elevation settings suitable for first-run generation and previews.
        /// </summary>
        public static ElevationSettings Default => new(
            DefaultMinimumElevation,
            DefaultMaximumElevation);

        /// <summary>
        /// Validates that these settings can be used for deterministic elevation generation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the elevation range is smaller than <see cref="MinimumElevationRange"/>
        /// or does not fit in a signed 32-bit integer.
        /// </exception>
        public void Validate()
        {
            long range = ElevationRange;

            if (range < MinimumElevationRange)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumElevation),
                    MaximumElevation,
                    "Maximum elevation must be greater than minimum elevation.");
            }

            if (range > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumElevation),
                    MaximumElevation,
                    "Elevation range must fit in a signed 32-bit integer.");
            }
        }

        /// <summary>
        /// Determines whether these settings are equal to another settings value.
        /// </summary>
        /// <param name="other">Other elevation settings.</param>
        /// <returns>
        /// <see langword="true"/> when both settings values contain the same minimum and
        /// maximum elevation values; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(ElevationSettings other)
        {
            return MinimumElevation == other.MinimumElevation &&
                   MaximumElevation == other.MaximumElevation;
        }

        /// <summary>
        /// Determines whether this settings value is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this settings value.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is an
        /// <see cref="ElevationSettings"/> value equal to this value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ElevationSettings other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this settings value.
        /// </summary>
        /// <returns>A hash code derived from the minimum and maximum elevation values.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (MinimumElevation * 397) ^ MaximumElevation;
            }
        }

        /// <summary>
        /// Determines whether two elevation settings values are equal.
        /// </summary>
        /// <param name="left">Left elevation settings value.</param>
        /// <param name="right">Right elevation settings value.</param>
        /// <returns>
        /// <see langword="true"/> when both values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(ElevationSettings left, ElevationSettings right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two elevation settings values are not equal.
        /// </summary>
        /// <param name="left">Left elevation settings value.</param>
        /// <param name="right">Right elevation settings value.</param>
        /// <returns>
        /// <see langword="true"/> when the values are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(ElevationSettings left, ElevationSettings right)
        {
            return !left.Equals(right);
        }
    }
}