namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
   /// <summary>
    /// Texture representation used for height-field preview rendering.
    /// Kept in the domain assembly because preview settings are serialized on authoring assets.
    /// </summary>
    public enum HeightFieldPreviewTextureMode : byte
    {
        Rgba32Grayscale = 0,
        RFloat = 1
    }
}