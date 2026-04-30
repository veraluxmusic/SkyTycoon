#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles
{
    /// <summary>
    /// Serializable authoring representation of a region role.
    /// Runtime generation uses RegionRoleDefinition and compact RegionRoleId values instead.
    /// </summary>
    [Serializable]
    public sealed class RegionRoleDefinitionAuthoring
    {
        [SerializeField] private string _stableName = "Tycoon.RegionRole.Custom";
        [SerializeField] private string _displayName = "Custom Region Role";
        [SerializeField] private RegionPrimaryIdentity _primaryIdentity = RegionPrimaryIdentity.BalancedFrontier;
        [SerializeField, TextArea(1, 3)] private string _secondaryFlavor = "Flexible regional identity.";
        [SerializeField, TextArea(1, 3)] private string _requiredLocalStrength = "Moderate early viability.";
        [SerializeField, TextArea(1, 3)] private string _requiredWeakness = "No dominant top-tier advantage.";
        [SerializeField, TextArea(1, 3)] private string _exportTarget = "Flexible mid-value exports.";
        [SerializeField, TextArea(1, 3)] private string _importTemptation = "Premium goods and specialized inputs.";
        [SerializeField, TextArea(1, 3)] private string _scenicIdentity = "Mixed terrain and frontier towns.";
        [SerializeField, TextArea(1, 3)] private string _preferredTerrain = "Plains, hills, river margins, and transition zones.";
        [SerializeField, TextArea(1, 3)] private string _preferredClimate = "Moderate mixed climate.";
        [SerializeField, TextArea(1, 3)] private string _preferredGeology = "Mixed low-intensity geology.";
        [SerializeField, TextArea(1, 3)] private string _preferredConnectivity = "Good route optionality without monopoly pressure.";

        public string StableName => _stableName;
        public string DisplayName => _displayName;
        public RegionPrimaryIdentity PrimaryIdentity => _primaryIdentity;
        public string SecondaryFlavor => _secondaryFlavor;
        public string RequiredLocalStrength => _requiredLocalStrength;
        public string RequiredWeakness => _requiredWeakness;
        public string ExportTarget => _exportTarget;
        public string ImportTemptation => _importTemptation;
        public string ScenicIdentity => _scenicIdentity;
        public string PreferredTerrain => _preferredTerrain;
        public string PreferredClimate => _preferredClimate;
        public string PreferredGeology => _preferredGeology;
        public string PreferredConnectivity => _preferredConnectivity;

        public RegionRoleDefinition ToRuntimeDefinition()
        {
            return new RegionRoleDefinition(
                RegionRoleId.FromStableName(RequireText(_stableName, nameof(_stableName))),
                RequireText(_displayName, nameof(_displayName)),
                _primaryIdentity,
                RequireText(_secondaryFlavor, nameof(_secondaryFlavor)),
                RequireText(_requiredLocalStrength, nameof(_requiredLocalStrength)),
                RequireText(_requiredWeakness, nameof(_requiredWeakness)),
                RequireText(_exportTarget, nameof(_exportTarget)),
                RequireText(_importTemptation, nameof(_importTemptation)),
                RequireText(_scenicIdentity, nameof(_scenicIdentity)),
                RequireText(_preferredTerrain, nameof(_preferredTerrain)),
                RequireText(_preferredClimate, nameof(_preferredClimate)),
                RequireText(_preferredGeology, nameof(_preferredGeology)),
                RequireText(_preferredConnectivity, nameof(_preferredConnectivity)));
        }

        public static RegionRoleDefinitionAuthoring FromRuntimeDefinition(RegionRoleDefinition definition, string stableName)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            RegionRoleDefinitionAuthoring authoring = new()

            {
                _stableName = RequireText(stableName, nameof(stableName)),
                _displayName = definition.DisplayName,
                _primaryIdentity = definition.PrimaryIdentity,
                _secondaryFlavor = definition.SecondaryFlavor,
                _requiredLocalStrength = definition.RequiredLocalStrength,
                _requiredWeakness = definition.RequiredWeakness,
                _exportTarget = definition.ExportTarget,
                _importTemptation = definition.ImportTemptation,
                _scenicIdentity = definition.ScenicIdentity,
                _preferredTerrain = definition.PreferredTerrain,
                _preferredClimate = definition.PreferredClimate,
                _preferredGeology = definition.PreferredGeology,
                _preferredConnectivity = definition.PreferredConnectivity
            };

            return authoring;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Region role authoring text must not be null, empty, or whitespace.", parameterName);

            return value.Trim();
        }
    }
}
