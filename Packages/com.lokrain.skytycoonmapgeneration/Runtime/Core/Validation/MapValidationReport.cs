#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Validation
{
    /// <summary>
    /// Immutable validation report returned by generation stages and final generation.
    /// </summary>
    [Serializable]
    public sealed class MapValidationReport
    {
        private static readonly GenerationIssue[] EmptyIssues = Array.Empty<GenerationIssue>();

        public MapValidationReport(StableHash128 artifactHash, IReadOnlyList<GenerationIssue> issues)
        {
            ArtifactHash = artifactHash;

            if (issues == null || issues.Count == 0)
            {
                Issues = EmptyIssues;
                Passed = true;
                FatalIssueCount = 0;
                ErrorIssueCount = 0;
                WarningIssueCount = 0;
                return;
            }

            GenerationIssue[] copy = new GenerationIssue[issues.Count];
            int fatalCount = 0;
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < issues.Count; i++)
            {
                GenerationIssue issue = issues[i];
                copy[i] = issue;

                if (issue.Severity == GenerationIssueSeverity.Fatal)
                    fatalCount++;
                else if (issue.Severity == GenerationIssueSeverity.Error)
                    errorCount++;
                else if (issue.Severity == GenerationIssueSeverity.Warning)
                    warningCount++;
            }

            Issues = copy;
            FatalIssueCount = fatalCount;
            ErrorIssueCount = errorCount;
            WarningIssueCount = warningCount;
            Passed = fatalCount == 0 && errorCount == 0;
        }

        public static MapValidationReport PassedReport(StableHash128 artifactHash)
        {
            return new MapValidationReport(artifactHash, EmptyIssues);
        }

        public bool Passed { get; }
        public StableHash128 ArtifactHash { get; }
        public IReadOnlyList<GenerationIssue> Issues { get; }
        public int FatalIssueCount { get; }
        public int ErrorIssueCount { get; }
        public int WarningIssueCount { get; }
    }
}
