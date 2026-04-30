#nullable enable

using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Validation
{
    /// <summary>
    /// Managed builder used by validation/orchestration. Do not pass this into Burst jobs.
    /// </summary>
    public sealed class MapValidationReportBuilder
    {
        private readonly List<GenerationIssue> _issues = new(32);

        public int IssueCount => _issues.Count;

        public void Clear()
        {
            _issues.Clear();
        }

        public void Add(
            GenerationIssueId id,
            GenerationIssueSeverity severity,
            MapGenerationStageId stageId,
            MapFieldId fieldId,
            string message)
        {
            _issues.Add(new GenerationIssue(id, severity, stageId, fieldId, message));
        }

        public void AddError(
            GenerationIssueId id,
            MapGenerationStageId stageId,
            MapFieldId fieldId,
            string message)
        {
            Add(id, GenerationIssueSeverity.Error, stageId, fieldId, message);
        }

        public void AddFatal(
            GenerationIssueId id,
            MapGenerationStageId stageId,
            MapFieldId fieldId,
            string message)
        {
            Add(id, GenerationIssueSeverity.Fatal, stageId, fieldId, message);
        }

        public MapValidationReport Build(StableHash128 artifactHash)
        {
            return new MapValidationReport(artifactHash, _issues);
        }
    }
}
