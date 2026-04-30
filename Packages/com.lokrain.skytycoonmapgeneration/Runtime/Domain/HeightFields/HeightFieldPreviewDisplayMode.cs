#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Controls what semantic layer is displayed by height-field preview tooling.
    /// This does not change the generated source data; it only changes how the generated
    /// samples are visualized and diagnosed.
    /// </summary>
    public enum HeightFieldPreviewDisplayMode
    {
        /// <summary>
        /// Displays the generated scalar height-field values as grayscale.
        /// </summary>
        RawHeightField = 0,

        /// <summary>
        /// Displays the exact top-percentile land-candidate mask derived from the scalar field.
        /// This is a diagnostic preview only. It is not the final land/water output stage.
        /// </summary>
        TopPercentileLandCandidate = 1
    }
}