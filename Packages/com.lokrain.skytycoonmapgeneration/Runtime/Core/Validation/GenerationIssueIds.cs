#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Core.Validation
{
    public static class GenerationIssueIds
    {
        public static readonly GenerationIssueId RequestInvalid = GenerationIssueId.FromStableName("RequestInvalid");
        public static readonly GenerationIssueId FieldMissing = GenerationIssueId.FromStableName("FieldMissing");
        public static readonly GenerationIssueId FieldRangeViolation = GenerationIssueId.FromStableName("FieldRangeViolation");
        public static readonly GenerationIssueId StageFailed = GenerationIssueId.FromStableName("StageFailed");
        public static readonly GenerationIssueId DeterminismFailure = GenerationIssueId.FromStableName("DeterminismFailure");
        public static readonly GenerationIssueId RegionConnectivityFailure = GenerationIssueId.FromStableName("RegionConnectivityFailure");
        public static readonly GenerationIssueId RegionViabilityFailure = GenerationIssueId.FromStableName("RegionViabilityFailure");
        public static readonly GenerationIssueId AntiMonopolyFailure = GenerationIssueId.FromStableName("AntiMonopolyFailure");
    }
}
