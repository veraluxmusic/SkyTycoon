#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    /// <summary>
    /// Managed catalog of available region roles. This belongs to orchestration, authoring, previews,
    /// and validation reports. Jobs should use compact role ids or compiled numeric weights.
    /// </summary>
    public sealed class RegionRoleCatalog
    {
        private readonly RegionRoleDefinition[] _definitions;
        private readonly Dictionary<RegionRoleId, RegionRoleDefinition> _definitionById;

        public RegionRoleCatalog(IReadOnlyList<RegionRoleDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            if (definitions.Count == 0)
                throw new ArgumentException("Region role catalog must contain at least one role definition.", nameof(definitions));

            _definitions = new RegionRoleDefinition[definitions.Count];
            _definitionById = new Dictionary<RegionRoleId, RegionRoleDefinition>(definitions.Count);

            for (int i = 0; i < definitions.Count; i++)
            {
                RegionRoleDefinition definition = definitions[i];

                if (definition == null)
                    throw new ArgumentException("Region role catalog must not contain null definitions.", nameof(definitions));

                if (_definitionById.ContainsKey(definition.Id))
                    throw new ArgumentException("Duplicate region role id in catalog: " + definition.Id, nameof(definitions));

                _definitions[i] = definition;
                _definitionById.Add(definition.Id, definition);
            }
        }

        public int Count => _definitions.Length;

        public RegionRoleDefinition this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_definitions.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Role catalog index is outside the valid range.");

                return _definitions[index];
            }
        }

        public bool Contains(RegionRoleId roleId)
        {
            return _definitionById.ContainsKey(roleId);
        }

        public RegionRoleDefinition GetRequired(RegionRoleId roleId)
        {
            roleId.Validate();

            if (!_definitionById.TryGetValue(roleId, out RegionRoleDefinition definition))
                throw new KeyNotFoundException("Region role id is not present in the catalog: " + roleId);

            return definition;
        }

        public RegionRoleDefinition[] CopyDefinitions()
        {
            RegionRoleDefinition[] copy = new RegionRoleDefinition[_definitions.Length];
            Array.Copy(_definitions, copy, _definitions.Length);
            return copy;
        }

        public void ValidateForRegionCount(int regionCount)
        {
            if (regionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionCount), regionCount, "Region count must be positive.");

            if (_definitions.Length < regionCount)
            {
                throw new InvalidOperationException(
                    "Region role catalog does not contain enough role definitions for the requested region count. Required: "
                    + regionCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ", available: "
                    + _definitions.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public static RegionRoleCatalog CreateTycoonEightRegionDefault()
        {
            return new RegionRoleCatalog(new[]
            {
                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.FertileBreadbasket"),
                    "Fertile Breadbasket",
                    RegionPrimaryIdentity.FertileBreadbasket,
                    "Open agrarian lowlands with strong early food security.",
                    "Reliable grain and farm production.",
                    "Weak ore and heavy industry inputs.",
                    "Food, grain, livestock, and processed agricultural goods.",
                    "Ore, tools, coal, and advanced construction inputs.",
                    "Broad green plains cut by settled river valleys.",
                    "Plains, floodplains, and low hills.",
                    "Mild and sufficiently wet.",
                    "Sedimentary plains with low mineral intensity.",
                    "Easy internal roads, moderate rail, several external exits."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.MiningUplands"),
                    "Mining Uplands",
                    RegionPrimaryIdentity.MiningUplands,
                    "Rugged uplands with valuable mineral belts.",
                    "Iron, copper, stone, and quarry opportunities.",
                    "Weak fertile land and harder local food supply.",
                    "Ore, stone, copper, and heavy raw materials.",
                    "Food, timber, and consumer goods.",
                    "Hard mountain shoulders, passes, and quarry towns.",
                    "Mountains, hills, ridges, and passes.",
                    "Cooler uplands with mixed moisture.",
                    "Mineral belts and exposed bedrock.",
                    "Valuable but constrained corridors; rail grades matter."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.Timberland"),
                    "Timberland",
                    RegionPrimaryIdentity.Timberland,
                    "Forested region with strong construction-chain support.",
                    "Logging, sawmills, and forest-derived production.",
                    "Weak large-scale agriculture and some heavy industry inputs.",
                    "Timber, lumber, paper, and construction materials.",
                    "Food, fuel, and metal inputs.",
                    "Dense forests, lakes, and wooded valleys.",
                    "Forested hills, wet uplands, and lake districts.",
                    "Wet temperate or boreal-leaning.",
                    "Mixed geology with moderate stone, low-to-moderate ore.",
                    "Road flexibility is strong; rail prefers valley corridors."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.PortTradeCoast"),
                    "Port-Trade Coast",
                    RegionPrimaryIdentity.PortTradeCoast,
                    "Coastal trade region with strong import/export leverage.",
                    "Ports, fisheries, and flexible trade routing.",
                    "Weaker inland extraction and possible route bottlenecks.",
                    "Fish, port throughput, imported goods, and coastal trade.",
                    "Inland raw materials and bulk industrial inputs.",
                    "Bays, harbors, coastal towns, and shoreline industry.",
                    "Coasts, bays, river mouths, and low coastal shelves.",
                    "Humid coastal climate.",
                    "Coastal sedimentary basins and mixed lowland geology.",
                    "Strong harbor access; inland exits must stay redundant."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.RiverlandGrowth"),
                    "Riverland Growth Region",
                    RegionPrimaryIdentity.RiverlandGrowth,
                    "River-rich region optimized for city growth and logistics.",
                    "Town growth, river-adjacent fertility, and crossings.",
                    "May lack premium raw extraction or deep ports.",
                    "Food, city demand, logistics throughput, and river-valley goods.",
                    "Ore, fuel, and specialized resources.",
                    "Large river bends, deltas, crossings, and fertile corridors.",
                    "River valleys, floodplains, and gentle basins.",
                    "Moist river-influenced climate.",
                    "Alluvial and sedimentary basins.",
                    "High corridor value; bridges must create choices, not locks."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.IndustrialPlateau"),
                    "Industrial Plateau",
                    RegionPrimaryIdentity.IndustrialPlateau,
                    "Elevated industrial region with solid buildable shelves and fuel demand.",
                    "Industrial siting, construction materials, and medium extraction.",
                    "Less efficient food and potentially expensive grade changes.",
                    "Manufactured goods, bricks, stone, and mid-chain industry.",
                    "Food, high-quality ore, and port access.",
                    "Broad plateaus, escarpments, terraces, and factory towns.",
                    "Plateaus, foothills, escarpments, and upland basins.",
                    "Moderate to dry, altitude-influenced.",
                    "Sedimentary shelves, stone, clay, and fuel-adjacent basins.",
                    "Strong internal rails on shelves; hard transitions at edges."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.DryExtractorBasin"),
                    "Dry Extractor Basin",
                    RegionPrimaryIdentity.DryExtractorBasin,
                    "Dry basin with special extraction identity rather than fertile abundance.",
                    "Oil, salt, minerals, livestock, or dry-basin extraction.",
                    "Weak food security and limited forest resources.",
                    "Oil, salt, minerals, and dry-region specialty goods.",
                    "Food, timber, water-adjacent goods, and construction inputs.",
                    "Arid basins, mesas, salt flats, and sparse industrial outposts.",
                    "Dry basins, steppe, mesas, and ancient seabed zones.",
                    "Semi-arid to arid with possible river oasis effects.",
                    "Sedimentary basins, evaporites, and hydrocarbon potential.",
                    "Long open routes, few natural barriers, high import temptation."),

                new RegionRoleDefinition(
                    RegionRoleId.FromStableName("Tycoon.RegionRole.BalancedFrontier"),
                    "Balanced Frontier",
                    RegionPrimaryIdentity.BalancedFrontier,
                    "Flexible frontier region that adapts to neighboring opportunities.",
                    "Moderate early viability across several chains.",
                    "No dominant top-tier advantage by default.",
                    "Flexible mid-value exports based on nearby geography.",
                    "Premium goods and specialized inputs.",
                    "Mixed terrain, frontier towns, and varied scenic transitions.",
                    "Plains, hills, river margins, and transition zones.",
                    "Moderate mixed climate.",
                    "Mixed low-intensity geology.",
                    "Good route optionality without monopoly pressure.")
            });
        }
    }
}
