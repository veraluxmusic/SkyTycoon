#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Regions
{
    /// <summary>
    /// Managed role metadata used by authoring, previews, validation reports, and assignment policy.
    /// Jobs should receive compact role ids or compiled numeric weights, not this object.
    /// </summary>
    [Serializable]
    public sealed class RegionRoleDefinition
    {
        public RegionRoleDefinition(
            RegionRoleId id,
            string displayName,
            RegionPrimaryIdentity primaryIdentity,
            string secondaryFlavor,
            string requiredLocalStrength,
            string requiredWeakness,
            string exportTarget,
            string importTemptation,
            string scenicIdentity,
            string preferredTerrain,
            string preferredClimate,
            string preferredGeology,
            string preferredConnectivity)
        {
            if (id.IsNone)
                throw new ArgumentException("Region role definition id must not be None.", nameof(id));

            if (primaryIdentity == RegionPrimaryIdentity.Unknown)
                throw new ArgumentOutOfRangeException(nameof(primaryIdentity), primaryIdentity, "Region role primary identity must be explicit.");

            DisplayName = RequireText(displayName, nameof(displayName));
            SecondaryFlavor = RequireText(secondaryFlavor, nameof(secondaryFlavor));
            RequiredLocalStrength = RequireText(requiredLocalStrength, nameof(requiredLocalStrength));
            RequiredWeakness = RequireText(requiredWeakness, nameof(requiredWeakness));
            ExportTarget = RequireText(exportTarget, nameof(exportTarget));
            ImportTemptation = RequireText(importTemptation, nameof(importTemptation));
            ScenicIdentity = RequireText(scenicIdentity, nameof(scenicIdentity));
            PreferredTerrain = RequireText(preferredTerrain, nameof(preferredTerrain));
            PreferredClimate = RequireText(preferredClimate, nameof(preferredClimate));
            PreferredGeology = RequireText(preferredGeology, nameof(preferredGeology));
            PreferredConnectivity = RequireText(preferredConnectivity, nameof(preferredConnectivity));

            Id = id;
            PrimaryIdentity = primaryIdentity;
        }

        public RegionRoleId Id { get; }
        public string DisplayName { get; }
        public RegionPrimaryIdentity PrimaryIdentity { get; }
        public string SecondaryFlavor { get; }
        public string RequiredLocalStrength { get; }
        public string RequiredWeakness { get; }
        public string ExportTarget { get; }
        public string ImportTemptation { get; }
        public string ScenicIdentity { get; }
        public string PreferredTerrain { get; }
        public string PreferredClimate { get; }
        public string PreferredGeology { get; }
        public string PreferredConnectivity { get; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Region role text fields must not be null, empty, or whitespace.", parameterName);

            return value;
        }
    }
}
