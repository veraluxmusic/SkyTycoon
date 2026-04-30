#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    /// <summary>
    /// Central metadata catalog for known generated fields. This is intentionally managed-only.
    /// It supports editor preview, reports, and artifact manifests; jobs should use raw arrays.
    /// </summary>
    public sealed class MapFieldDescriptorCatalog
    {
        private readonly Dictionary<MapFieldId, MapFieldDescriptor> _descriptors;

        private MapFieldDescriptorCatalog(IReadOnlyList<MapFieldDescriptor> descriptors)
        {
            _descriptors = new Dictionary<MapFieldId, MapFieldDescriptor>(descriptors.Count);

            for (int i = 0; i < descriptors.Count; i++)
            {
                MapFieldDescriptor descriptor = descriptors[i];

                if (_descriptors.ContainsKey(descriptor.Id))
                    throw new InvalidOperationException("Duplicate map field descriptor id: " + descriptor.Id);

                _descriptors.Add(descriptor.Id, descriptor);
            }
        }

        public int Count => _descriptors.Count;

        public static MapFieldDescriptorCatalog CreateDefault()
        {
            return new MapFieldDescriptorCatalog(new[]
            {
                new MapFieldDescriptor(MapFieldIds.RegionId, "RegionId", FieldElementType.Int32, FieldRange.Unbounded, FieldPreviewHint.RegionOwnership, MapGenerationStageIds.CompetitiveEconomySkeleton),
                new MapFieldDescriptor(MapFieldIds.NeutralZoneId, "NeutralZoneId", FieldElementType.Int32, FieldRange.Unbounded, FieldPreviewHint.Categorical, MapGenerationStageIds.CompetitiveEconomySkeleton),

                new MapFieldDescriptor(MapFieldIds.Height, "Height", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Grayscale01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.LandMask, "LandMask", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.OceanMask, "OceanMask", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.CoastDistance, "CoastDistance", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.ContinentDistance, "ContinentDistance", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.Slope, "Slope", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.Relief, "Relief", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.MountainInfluence, "MountainInfluence", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.BasinInfluence, "BasinInfluence", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),
                new MapFieldDescriptor(MapFieldIds.PlainInfluence, "PlainInfluence", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),

                new MapFieldDescriptor(MapFieldIds.FlowDirection, "FlowDirection", FieldElementType.Int32, FieldRange.Unbounded, FieldPreviewHint.Categorical, MapGenerationStageIds.Hydrology),
                new MapFieldDescriptor(MapFieldIds.FlowAccumulation, "FlowAccumulation", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.Heatmap01, MapGenerationStageIds.Hydrology),
                new MapFieldDescriptor(MapFieldIds.River, "River", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.Hydrology),
                new MapFieldDescriptor(MapFieldIds.Lake, "Lake", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.Hydrology),
                new MapFieldDescriptor(MapFieldIds.Floodplain, "Floodplain", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.Hydrology),

                new MapFieldDescriptor(MapFieldIds.Temperature, "Temperature", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.Climate),
                new MapFieldDescriptor(MapFieldIds.Moisture, "Moisture", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.Climate),
                new MapFieldDescriptor(MapFieldIds.Biome, "Biome", FieldElementType.Int32, FieldRange.Unbounded, FieldPreviewHint.Categorical, MapGenerationStageIds.Biomes),
                new MapFieldDescriptor(MapFieldIds.Fertility, "Fertility", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.Climate),

                new MapFieldDescriptor(MapFieldIds.MineralSuitability, "MineralSuitability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.GeologyAndResources),
                new MapFieldDescriptor(MapFieldIds.ForestSuitability, "ForestSuitability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.GeologyAndResources),
                new MapFieldDescriptor(MapFieldIds.OilSuitability, "OilSuitability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.GeologyAndResources),
                new MapFieldDescriptor(MapFieldIds.Buildability, "Buildability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.MacroLandform),

                new MapFieldDescriptor(MapFieldIds.TownSuitability, "TownSuitability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.TownSiting),
                new MapFieldDescriptor(MapFieldIds.CapitalCandidate, "CapitalCandidate", FieldElementType.Byte, FieldRange.Normalized01, FieldPreviewHint.BinaryMask, MapGenerationStageIds.TownSiting),
                new MapFieldDescriptor(MapFieldIds.IndustryCandidate, "IndustryCandidate", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.IndustryPlacement),

                new MapFieldDescriptor(MapFieldIds.RoadCost, "RoadCost", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.TransportCost, MapGenerationStageIds.TransportAnalysis),
                new MapFieldDescriptor(MapFieldIds.RailCost, "RailCost", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.TransportCost, MapGenerationStageIds.TransportAnalysis),
                new MapFieldDescriptor(MapFieldIds.HarborSuitability, "HarborSuitability", FieldElementType.Float32, FieldRange.Normalized01, FieldPreviewHint.Heatmap01, MapGenerationStageIds.TransportAnalysis),
                new MapFieldDescriptor(MapFieldIds.CorridorPressure, "CorridorPressure", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.Heatmap01, MapGenerationStageIds.TransportAnalysis),
                new MapFieldDescriptor(MapFieldIds.ChokepointPressure, "ChokepointPressure", FieldElementType.Float32, FieldRange.Unbounded, FieldPreviewHint.Heatmap01, MapGenerationStageIds.TransportAnalysis)
            });
        }

        public bool TryGet(MapFieldId id, out MapFieldDescriptor descriptor)
        {
            return _descriptors.TryGetValue(id, out descriptor);
        }

        public MapFieldDescriptor Get(MapFieldId id)
        {
            if (!_descriptors.TryGetValue(id, out MapFieldDescriptor descriptor))
                throw new KeyNotFoundException("Unknown map field id: " + id);

            return descriptor;
        }
    }
}
