#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    public static class MapFieldIds
    {
        public static readonly MapFieldId RegionId = MapFieldId.FromStableName("RegionId");
        public static readonly MapFieldId NeutralZoneId = MapFieldId.FromStableName("NeutralZoneId");

        public static readonly MapFieldId Height = MapFieldId.FromStableName("Height");
        public static readonly MapFieldId LandMask = MapFieldId.FromStableName("LandMask");
        public static readonly MapFieldId OceanMask = MapFieldId.FromStableName("OceanMask");
        public static readonly MapFieldId CoastDistance = MapFieldId.FromStableName("CoastDistance");
        public static readonly MapFieldId ContinentDistance = MapFieldId.FromStableName("ContinentDistance");
        public static readonly MapFieldId Slope = MapFieldId.FromStableName("Slope");
        public static readonly MapFieldId Relief = MapFieldId.FromStableName("Relief");
        public static readonly MapFieldId MountainInfluence = MapFieldId.FromStableName("MountainInfluence");
        public static readonly MapFieldId BasinInfluence = MapFieldId.FromStableName("BasinInfluence");
        public static readonly MapFieldId PlainInfluence = MapFieldId.FromStableName("PlainInfluence");

        public static readonly MapFieldId FlowDirection = MapFieldId.FromStableName("FlowDirection");
        public static readonly MapFieldId FlowAccumulation = MapFieldId.FromStableName("FlowAccumulation");
        public static readonly MapFieldId River = MapFieldId.FromStableName("River");
        public static readonly MapFieldId Lake = MapFieldId.FromStableName("Lake");
        public static readonly MapFieldId Floodplain = MapFieldId.FromStableName("Floodplain");

        public static readonly MapFieldId Temperature = MapFieldId.FromStableName("Temperature");
        public static readonly MapFieldId Moisture = MapFieldId.FromStableName("Moisture");
        public static readonly MapFieldId Biome = MapFieldId.FromStableName("Biome");
        public static readonly MapFieldId Fertility = MapFieldId.FromStableName("Fertility");
        public static readonly MapFieldId MineralSuitability = MapFieldId.FromStableName("MineralSuitability");
        public static readonly MapFieldId ForestSuitability = MapFieldId.FromStableName("ForestSuitability");
        public static readonly MapFieldId OilSuitability = MapFieldId.FromStableName("OilSuitability");
        public static readonly MapFieldId Buildability = MapFieldId.FromStableName("Buildability");

        public static readonly MapFieldId TownSuitability = MapFieldId.FromStableName("TownSuitability");
        public static readonly MapFieldId CapitalCandidate = MapFieldId.FromStableName("CapitalCandidate");
        public static readonly MapFieldId IndustryCandidate = MapFieldId.FromStableName("IndustryCandidate");

        public static readonly MapFieldId RoadCost = MapFieldId.FromStableName("RoadCost");
        public static readonly MapFieldId RailCost = MapFieldId.FromStableName("RailCost");
        public static readonly MapFieldId HarborSuitability = MapFieldId.FromStableName("HarborSuitability");
        public static readonly MapFieldId CorridorPressure = MapFieldId.FromStableName("CorridorPressure");
        public static readonly MapFieldId ChokepointPressure = MapFieldId.FromStableName("ChokepointPressure");
    }
}
