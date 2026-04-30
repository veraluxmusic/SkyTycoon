#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    public enum FieldPreviewHint : byte
    {
        None = 0,
        Grayscale01 = 1,
        BinaryMask = 2,
        Categorical = 3,
        SignedDiverging = 4,
        Heatmap01 = 5,
        RegionOwnership = 6,
        TransportCost = 7
    }
}
