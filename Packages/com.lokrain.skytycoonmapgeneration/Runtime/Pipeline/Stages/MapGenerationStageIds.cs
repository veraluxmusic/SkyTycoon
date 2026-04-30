#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages
{
    /// <summary>
    /// Stable stage identifiers from the v0.0.1 map-generation plan.
    /// </summary>
    public static class MapGenerationStageIds
    {
        public static readonly MapGenerationStageId CompetitiveEconomySkeleton = MapGenerationStageId.FromStableName("CompetitiveEconomySkeleton");
        public static readonly MapGenerationStageId MacroLandform = MapGenerationStageId.FromStableName("MacroLandform");
        public static readonly MapGenerationStageId Hydrology = MapGenerationStageId.FromStableName("Hydrology");
        public static readonly MapGenerationStageId Climate = MapGenerationStageId.FromStableName("Climate");
        public static readonly MapGenerationStageId Biomes = MapGenerationStageId.FromStableName("Biomes");
        public static readonly MapGenerationStageId GeologyAndResources = MapGenerationStageId.FromStableName("GeologyAndResources");
        public static readonly MapGenerationStageId TownSiting = MapGenerationStageId.FromStableName("TownSiting");
        public static readonly MapGenerationStageId IndustryPlacement = MapGenerationStageId.FromStableName("IndustryPlacement");
        public static readonly MapGenerationStageId TransportAnalysis = MapGenerationStageId.FromStableName("TransportAnalysis");
        public static readonly MapGenerationStageId RegionOpportunityScoring = MapGenerationStageId.FromStableName("RegionOpportunityScoring");
        public static readonly MapGenerationStageId ValidationAndRepair = MapGenerationStageId.FromStableName("ValidationAndRepair");
        public static readonly MapGenerationStageId ConstrainedBeautyPass = MapGenerationStageId.FromStableName("ConstrainedBeautyPass");
        public static readonly MapGenerationStageId ArtifactExport = MapGenerationStageId.FromStableName("ArtifactExport");
    }
}
