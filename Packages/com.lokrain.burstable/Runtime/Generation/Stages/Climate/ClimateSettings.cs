using System;

namespace Lokrain.Burstable.Generation.Stages.Climate
{
    /// <summary>
    /// Defines deterministic scalar settings used by the climate generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Climate settings describe scalar parameters used when generating temperature and moisture
    /// fields. They do not allocate workspace storage, resolve field definitions, access map data,
    /// schedule jobs, classify biomes, or own generation lifetime.
    /// </para>
    /// <para>
    /// This contract intentionally does not include spatial climate variation. Variation settings
    /// should be added only after the package owns an explicit deterministic variation model,
    /// including spatial scale, seed mixing, interpolation behavior, and boundary behavior.
    /// </para>
    /// <para>
    /// This type is immutable and safe to pass by value.
    /// </para>
    /// </remarks>
    public readonly struct ClimateSettings : IEquatable<ClimateSettings>
    {
        /// <summary>
        /// Default scalar value used as the base temperature before climate modifiers are applied.
        /// </summary>
        public const int DefaultBaseTemperature = 0;

        /// <summary>
        /// Default scalar value used as the base moisture before climate modifiers are applied.
        /// </summary>
        public const int DefaultBaseMoisture = 0;

        /// <summary>
        /// Default scalar temperature reduction applied per positive elevation unit.
        /// </summary>
        public const int DefaultElevationTemperaturePenalty = 0;

        /// <summary>
        /// Gets the default climate settings.
        /// </summary>
        /// <remarks>
        /// The default settings are neutral and deterministic. They do not introduce implicit
        /// climate variation or elevation-sensitive temperature changes.
        /// </remarks>
        public static ClimateSettings Default => new(
            DefaultBaseTemperature,
            DefaultBaseMoisture,
            DefaultElevationTemperaturePenalty);

        /// <summary>
        /// Initializes a new instance of the <see cref="ClimateSettings"/> struct.
        /// </summary>
        /// <param name="baseTemperature">
        /// Base scalar temperature value before deterministic climate modifiers are applied.
        /// </param>
        /// <param name="baseMoisture">
        /// Base scalar moisture value before deterministic climate modifiers are applied.
        /// </param>
        /// <param name="elevationTemperaturePenalty">
        /// Non-negative scalar temperature reduction applied per positive elevation unit.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="elevationTemperaturePenalty"/> is negative.
        /// </exception>
        public ClimateSettings(
            int baseTemperature,
            int baseMoisture,
            int elevationTemperaturePenalty)
        {
            ValidateElevationTemperaturePenalty(
                elevationTemperaturePenalty,
                nameof(elevationTemperaturePenalty));

            BaseTemperature = baseTemperature;
            BaseMoisture = baseMoisture;
            ElevationTemperaturePenalty = elevationTemperaturePenalty;
        }

        /// <summary>
        /// Gets the base scalar temperature value before deterministic climate modifiers are applied.
        /// </summary>
        public int BaseTemperature { get; }

        /// <summary>
        /// Gets the base scalar moisture value before deterministic climate modifiers are applied.
        /// </summary>
        public int BaseMoisture { get; }

        /// <summary>
        /// Gets the non-negative scalar temperature reduction applied per positive elevation unit.
        /// </summary>
        /// <remarks>
        /// A value of <c>0</c> disables elevation-sensitive temperature reduction. A value of
        /// <c>1</c> reduces temperature by one scalar unit for every positive elevation unit.
        /// </remarks>
        public int ElevationTemperaturePenalty { get; }

        /// <summary>
        /// Validates this climate settings value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="ElevationTemperaturePenalty"/> is negative.
        /// </exception>
        /// <remarks>
        /// This method exists so stages can revalidate settings at scheduling boundaries,
        /// matching the validation style used by other generation stages.
        /// </remarks>
        public void Validate()
        {
            ValidateElevationTemperaturePenalty(
                ElevationTemperaturePenalty,
                nameof(ElevationTemperaturePenalty));
        }

        /// <summary>
        /// Determines whether two <see cref="ClimateSettings"/> values are equal.
        /// </summary>
        /// <param name="left">The first settings value.</param>
        /// <param name="right">The second settings value.</param>
        /// <returns>
        /// <see langword="true"/> if both settings values contain the same scalar configuration;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(ClimateSettings left, ClimateSettings right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="ClimateSettings"/> values are not equal.
        /// </summary>
        /// <param name="left">The first settings value.</param>
        /// <param name="right">The second settings value.</param>
        /// <returns>
        /// <see langword="true"/> if the settings values contain different scalar configuration;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(ClimateSettings left, ClimateSettings right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public bool Equals(ClimateSettings other)
        {
            return BaseTemperature == other.BaseTemperature
                && BaseMoisture == other.BaseMoisture
                && ElevationTemperaturePenalty == other.ElevationTemperaturePenalty;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ClimateSettings other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = BaseTemperature;
                hashCode = (hashCode * 397) ^ BaseMoisture;
                hashCode = (hashCode * 397) ^ ElevationTemperaturePenalty;
                return hashCode;
            }
        }

        private static void ValidateElevationTemperaturePenalty(
            int elevationTemperaturePenalty,
            string parameterName)
        {
            if (elevationTemperaturePenalty < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    elevationTemperaturePenalty,
                    "Elevation temperature penalty must be greater than or equal to zero.");
            }
        }
    }
}