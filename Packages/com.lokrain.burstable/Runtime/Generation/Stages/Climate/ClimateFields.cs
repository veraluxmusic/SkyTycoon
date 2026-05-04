using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Stages.Climate
{
    /// <summary>
    /// Defines the generated field contract owned by the climate generation stage.
    /// </summary>
    /// <remarks>
    /// Climate fields are stage-owned workspace fields produced by climate generation and
    /// consumed by later biome classification, preview, export, gameplay, or rendering systems.
    ///
    /// This type defines stable identifiers and metadata only. It does not allocate storage,
    /// generate climate values, read elevation or terrain, schedule jobs, classify biomes, or
    /// own workspace lifetime.
    ///
    /// Field identifiers and names are part of the package data contract. Changing an existing
    /// identifier or name should be treated as a package contract break.
    /// </remarks>
    public static class ClimateFields
    {
        /// <summary>
        /// Raw identifier value for the primary temperature field.
        /// </summary>
        public const int TemperatureIdValue = 3;

        /// <summary>
        /// Raw identifier value for the primary moisture field.
        /// </summary>
        public const int MoistureIdValue = 4;

        /// <summary>
        /// Stable symbolic name for the primary temperature field.
        /// </summary>
        public const string TemperatureName = "temperature";

        /// <summary>
        /// Stable symbolic name for the primary moisture field.
        /// </summary>
        public const string MoistureName = "moisture";

        /// <summary>
        /// Gets the stable identifier for the primary temperature field.
        /// </summary>
        /// <remarks>
        /// The primary temperature field stores deterministic signed 32-bit scalar temperature
        /// values. The numeric range and scale are owned by the climate settings and algorithm
        /// contract, not by the workspace field registry.
        /// </remarks>
        public static MapFieldId TemperatureId => new(TemperatureIdValue);

        /// <summary>
        /// Gets the stable identifier for the primary moisture field.
        /// </summary>
        /// <remarks>
        /// The primary moisture field stores deterministic signed 32-bit scalar moisture values.
        /// The numeric range and scale are owned by the climate settings and algorithm contract,
        /// not by the workspace field registry.
        /// </remarks>
        public static MapFieldId MoistureId => new(MoistureIdValue);

        /// <summary>
        /// Gets the workspace field definition for the primary temperature field.
        /// </summary>
        /// <remarks>
        /// The field uses <see cref="MapFieldValueType.Int32"/> so climate generation can store
        /// deterministic scalar values without floating-point platform variance.
        /// </remarks>
        public static MapFieldDefinition Temperature => new(
            TemperatureId,
            MapFieldValueType.Int32,
            TemperatureName);

        /// <summary>
        /// Gets the workspace field definition for the primary moisture field.
        /// </summary>
        /// <remarks>
        /// The field uses <see cref="MapFieldValueType.Int32"/> so climate generation can store
        /// deterministic scalar values without floating-point platform variance.
        /// </remarks>
        public static MapFieldDefinition Moisture => new(
            MoistureId,
            MapFieldValueType.Int32,
            MoistureName);
    }
}