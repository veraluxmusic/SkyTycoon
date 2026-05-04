namespace Lokrain.Burstable.Generation.Stages.Climate
{
    /// <summary>
    /// Provides deterministic scalar climate calculations for the climate generation stage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type owns climate scalar calculations only. It does not allocate workspace storage,
    /// resolve field definitions, schedule jobs, read or write map fields, classify biomes,
    /// generate spatial noise, or own stage execution lifetime.
    /// </para>
    /// <para>
    /// The algorithm intentionally avoids coordinate-based variation until the package owns an
    /// explicit climate variation contract, including spatial scale, interpolation behavior,
    /// boundary behavior, and deterministic seed mixing. Adding hash-per-tile variation here would
    /// produce noisy salt-and-pepper climate data and would make the public behavior harder to
    /// replace without a contract break.
    /// </para>
    /// <para>
    /// All calculations use integer arithmetic. Overflow-sensitive operations are widened to
    /// 64-bit integers and clamped back to signed 32-bit values.
    /// </para>
    /// </remarks>
    public static class ClimateAlgorithm
    {
        /// <summary>
        /// Calculates the deterministic temperature value for one tile.
        /// </summary>
        /// <param name="elevation">
        /// The scalar elevation value for the tile.
        /// </param>
        /// <param name="settings">
        /// The scalar climate generation settings.
        /// </param>
        /// <returns>
        /// The deterministic scalar temperature value for the tile.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Temperature is calculated from <see cref="ClimateSettings.BaseTemperature"/> minus the
        /// elevation-derived temperature penalty.
        /// </para>
        /// <para>
        /// Negative elevation does not increase temperature. It contributes no elevation penalty.
        /// </para>
        /// </remarks>
        public static int CalculateTemperature(int elevation, ClimateSettings settings)
        {
            int elevationPenalty = CalculateElevationTemperaturePenalty(elevation, settings);

            return ClampToInt32((long)settings.BaseTemperature - elevationPenalty);
        }

        /// <summary>
        /// Calculates the deterministic moisture value for one tile.
        /// </summary>
        /// <param name="settings">
        /// The scalar climate generation settings.
        /// </param>
        /// <returns>
        /// The deterministic scalar moisture value for the tile.
        /// </returns>
        /// <remarks>
        /// Moisture currently resolves directly to <see cref="ClimateSettings.BaseMoisture"/>.
        /// Spatial moisture variation should be added only after the package owns an explicit
        /// deterministic climate variation contract.
        /// </remarks>
        public static int CalculateMoisture(ClimateSettings settings)
        {
            return settings.BaseMoisture;
        }

        /// <summary>
        /// Calculates the deterministic temperature penalty contributed by tile elevation.
        /// </summary>
        /// <param name="elevation">
        /// The scalar elevation value for the tile.
        /// </param>
        /// <param name="settings">
        /// The scalar climate generation settings.
        /// </param>
        /// <returns>
        /// The non-negative scalar temperature penalty contributed by elevation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The penalty is calculated as:
        /// </para>
        /// <code>
        /// elevation * settings.ElevationTemperaturePenalty
        /// </code>
        /// <para>
        /// Negative elevation and zero penalty produce no reduction. The multiplication is widened
        /// to 64-bit integer arithmetic and clamped to the signed 32-bit range.
        /// </para>
        /// </remarks>
        public static int CalculateElevationTemperaturePenalty(
            int elevation,
            ClimateSettings settings)
        {
            if (elevation <= 0 || settings.ElevationTemperaturePenalty <= 0)
            {
                return 0;
            }

            long penalty = (long)elevation * settings.ElevationTemperaturePenalty;

            return ClampToInt32(penalty);
        }

        private static int ClampToInt32(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }
    }
}