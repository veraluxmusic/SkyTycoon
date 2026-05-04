using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    /// <summary>
    /// Defines the generated field contract owned by the elevation generation stage.
    /// </summary>
    /// <remarks>
    /// Elevation fields are stage-owned workspace fields produced by elevation generation and
    /// consumed by later terrain, climate, biome, preview, export, or gameplay systems.
    ///
    /// This type defines stable identifiers and metadata only. It does not allocate storage,
    /// generate elevation values, apply shaping, sample noise, schedule jobs, or own workspace
    /// lifetime.
    ///
    /// Field identifiers and names are part of the package data contract. Changing an existing
    /// identifier or name should be treated as a package contract break.
    /// </remarks>
    public static class ElevationFields
    {
        /// <summary>
        /// Raw identifier value for the primary elevation field.
        /// </summary>
        public const int ElevationIdValue = 1;

        /// <summary>
        /// Stable symbolic name for the primary elevation field.
        /// </summary>
        public const string ElevationName = "elevation";

        /// <summary>
        /// Gets the stable identifier for the primary elevation field.
        /// </summary>
        /// <remarks>
        /// The primary elevation field stores deterministic signed 32-bit scalar elevation
        /// values. The numeric range and scale are owned by the elevation algorithm/settings
        /// contract, not by the workspace field registry.
        /// </remarks>
        public static MapFieldId ElevationId => new(ElevationIdValue);

        /// <summary>
        /// Gets the workspace field definition for the primary elevation field.
        /// </summary>
        /// <remarks>
        /// The field uses <see cref="MapFieldValueType.Int32"/> so generation can store
        /// deterministic scalar values without floating-point platform variance.
        /// </remarks>
        public static MapFieldDefinition Elevation => new(
            ElevationId,
            MapFieldValueType.Int32,
            ElevationName);
    }
}