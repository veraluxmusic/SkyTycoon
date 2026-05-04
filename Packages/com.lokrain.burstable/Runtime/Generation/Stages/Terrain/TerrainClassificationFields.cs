using Lokrain.Burstable.Tiles;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Stages.Terrain
{
    /// <summary>
    /// Defines the generated field contract owned by the terrain classification stage.
    /// </summary>
    /// <remarks>
    /// Terrain classification fields are stage-owned workspace fields produced by terrain
    /// classification and consumed by later climate, biome, preview, export, gameplay, or
    /// rendering systems.
    ///
    /// This type defines stable identifiers and metadata only. It does not allocate storage,
    /// classify terrain, read elevation, schedule jobs, or own workspace lifetime.
    ///
    /// Field identifiers and names are part of the package data contract. Changing an existing
    /// identifier or name should be treated as a package contract break.
    /// </remarks>
    public static class TerrainClassificationFields
    {
        /// <summary>
        /// Raw identifier value for the primary terrain kind field.
        /// </summary>
        public const int TerrainKindIdValue = 2;

        /// <summary>
        /// Stable symbolic name for the primary terrain kind field.
        /// </summary>
        public const string TerrainKindName = "terrain-kind";

        /// <summary>
        /// Gets the stable identifier for the primary terrain kind field.
        /// </summary>
        /// <remarks>
        /// The primary terrain kind field stores compact categorical values matching
        /// <see cref="TerrainKind"/>.
        /// </remarks>
        public static MapFieldId TerrainKindId => new(TerrainKindIdValue);

        /// <summary>
        /// Gets the workspace field definition for the primary terrain kind field.
        /// </summary>
        /// <remarks>
        /// The field uses <see cref="MapFieldValueType.UInt8"/> so terrain classification can
        /// store compact categorical values without managed enum arrays in workspace storage.
        /// </remarks>
        public static MapFieldDefinition TerrainKind => new(
            TerrainKindId,
            MapFieldValueType.UInt8,
            TerrainKindName);
    }
}