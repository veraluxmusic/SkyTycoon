#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Core.Requests
{
    public enum MapArchetype : byte
    {
        Unknown = 0,

        /// <summary>
        /// v0.0.1 target: one connected continent, hard ocean boundary,
        /// eight home regions, and neutral economic/trade zones.
        /// </summary>
        SingleContinentEightRegionNeutralCore = 1
    }
}
