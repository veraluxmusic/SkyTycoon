namespace Lokrain.Burstable.Tiles
{
    /// <summary>
    /// Identifies the deterministic terrain category assigned to a generated tile.
    /// </summary>
    /// <remarks>
    /// Terrain kinds are compact categorical values produced by terrain classification and
    /// consumed by later generation stages, previews, exporters, gameplay systems, or rendering
    /// code.
    ///
    /// This value describes the tile's broad terrain category only. It does not encode
    /// elevation, biome, climate, moisture, temperature, slope, ownership, transport
    /// infrastructure, object placement, or rendering data.
    ///
    /// The value zero is reserved for <see cref="Invalid"/> so clear-initialized workspace
    /// memory does not accidentally represent valid generated terrain. Terrain classification
    /// stages should write every terrain value they own.
    ///
    /// Existing numeric values are part of the generated data contract. Adding new values is
    /// safe. Changing the meaning of an existing value should be treated as a generation-version
    /// or package-contract change.
    /// </remarks>
    public enum TerrainKind : byte
    {
        /// <summary>
        /// Invalid, unknown, unassigned, or not-yet-classified terrain.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Water terrain.
        /// </summary>
        /// <remarks>
        /// Water is assigned to tiles whose elevation is below the configured terrain
        /// classification sea-level threshold.
        /// </remarks>
        Water = 1,

        /// <summary>
        /// Land terrain.
        /// </summary>
        /// <remarks>
        /// Land is assigned to tiles whose elevation is at or above the configured terrain
        /// classification sea-level threshold.
        /// </remarks>
        Land = 2
    }
}