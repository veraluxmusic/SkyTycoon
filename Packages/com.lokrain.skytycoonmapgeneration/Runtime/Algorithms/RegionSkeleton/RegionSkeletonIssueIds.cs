#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Core.Validation;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton
{
    public static class RegionSkeletonIssueIds
    {
        public static readonly GenerationIssueId RoleCatalogInvalid = GenerationIssueId.FromStableName("RegionSkeleton.RoleCatalogInvalid");
        public static readonly GenerationIssueId RegionMissingCells = GenerationIssueId.FromStableName("RegionSkeleton.RegionMissingCells");
        public static readonly GenerationIssueId RegionTooSmall = GenerationIssueId.FromStableName("RegionSkeleton.RegionTooSmall");
        public static readonly GenerationIssueId RegionTooFewNeighbors = GenerationIssueId.FromStableName("RegionSkeleton.RegionTooFewNeighbors");
        public static readonly GenerationIssueId RegionFragmented = GenerationIssueId.FromStableName("RegionSkeleton.RegionFragmented");
        public static readonly GenerationIssueId NeutralZoneMissingCells = GenerationIssueId.FromStableName("RegionSkeleton.NeutralZoneMissingCells");
        public static readonly GenerationIssueId NeutralZoneTooSmall = GenerationIssueId.FromStableName("RegionSkeleton.NeutralZoneTooSmall");
        public static readonly GenerationIssueId NeutralZonePoorAccess = GenerationIssueId.FromStableName("RegionSkeleton.NeutralZonePoorAccess");
        public static readonly GenerationIssueId FieldOwnershipMismatch = GenerationIssueId.FromStableName("RegionSkeleton.FieldOwnershipMismatch");
    }
}
