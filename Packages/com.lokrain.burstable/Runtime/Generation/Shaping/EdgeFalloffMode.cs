namespace Lokrain.Burstable.Generation.Shaping
{
    /// <summary>
    /// Identifies the deterministic edge-falloff curve used to shape rectangular maps.
    /// </summary>
    /// <remarks>
    /// Edge falloff is map-shaping policy. It reduces generated values near rectangular
    /// map borders so default maps do not read like hard cut-outs from infinite terrain.
    ///
    /// This enum is part of the generation spec and therefore affects deterministic output.
    /// Adding new modes is safe; changing the behavior assigned to an existing value should
    /// be treated as a generation-version change.
    /// </remarks>
    public enum EdgeFalloffMode : byte
    {
        /// <summary>
        /// Disables edge falloff. Generated values are not reduced near map borders.
        /// </summary>
        None = 0,

        /// <summary>
        /// Applies a linear fixed-point multiplier based on distance to the nearest map edge.
        /// </summary>
        Linear = 1
    }
}