#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Core.Validation;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform
{
    public static class MacroLandformIssueIds
    {
        public static readonly GenerationIssueId LandCountMismatch = GenerationIssueId.FromStableName("MacroLandform.LandCountMismatch");
        public static readonly GenerationIssueId HardWaterBorderFailure = GenerationIssueId.FromStableName("MacroLandform.HardWaterBorderFailure");
        public static readonly GenerationIssueId LandConnectivityFailure = GenerationIssueId.FromStableName("MacroLandform.LandConnectivityFailure");
        public static readonly GenerationIssueId FieldRangeFailure = GenerationIssueId.FromStableName("MacroLandform.FieldRangeFailure");
        public static readonly GenerationIssueId DimensionMismatch = GenerationIssueId.FromStableName("MacroLandform.DimensionMismatch");
        public static readonly GenerationIssueId BuildabilityFailure = GenerationIssueId.FromStableName("MacroLandform.BuildabilityFailure");
    }
}
