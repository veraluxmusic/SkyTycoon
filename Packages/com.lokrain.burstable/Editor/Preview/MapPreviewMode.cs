namespace Lokrain.Burstable.Editor
{
    /// <summary>
    /// Defines the generated map data layer rendered by the editor preview window.
    /// </summary>
    /// <remarks>
    /// This enum is editor-only. It selects how an existing <c>MapWorkspace</c> is visualized;
    /// it must not affect map generation, runtime data, determinism, or serialized map output.
    /// </remarks>
    public enum MapPreviewMode : byte
    {
        /// <summary>
        /// Renders the final terrain classification layer.
        /// </summary>
        /// <remarks>
        /// Uses the generated terrain field, including sea, coast, grass, rough terrain,
        /// forest, desert, snow, mountain, river, and lake categories where available.
        /// This is the primary designer-facing preview mode.
        /// </remarks>
        Terrain = 1,

        /// <summary>
        /// Renders the discrete elevation field as a grayscale height map.
        /// </summary>
        /// <remarks>
        /// Useful for validating continent shape, island falloff, mountain distribution,
        /// sea-level balance, and terracing behavior before terrain classification.
        /// </remarks>
        Elevation = 2,

        /// <summary>
        /// Renders the generated moisture field as a normalized diagnostic overlay.
        /// </summary>
        /// <remarks>
        /// Useful for validating climate distribution and biome classification inputs.
        /// This mode visualizes source data, not the final biome result.
        /// </remarks>
        Moisture = 3,

        /// <summary>
        /// Renders the generated temperature field as a normalized diagnostic overlay.
        /// </summary>
        /// <remarks>
        /// Useful for validating latitude, elevation penalty, and temperature noise
        /// before biome classification.
        /// </remarks>
        Temperature = 4,

        /// <summary>
        /// Renders the final biome classification layer.
        /// </summary>
        /// <remarks>
        /// Uses the generated biome field after terrain, moisture, temperature, and
        /// elevation have been evaluated. This is the best mode for inspecting climate
        /// results at a gameplay-design level.
        /// </remarks>
        Biome = 5,

        /// <summary>
        /// Renders the local elevation delta field as a grayscale slope map.
        /// </summary>
        /// <remarks>
        /// Useful for validating rough terrain, mountain classification, settlement
        /// suitability, road/rail placement cost, and other transport-related constraints.
        /// </remarks>
        Slope = 6
    }
}