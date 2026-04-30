#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Validation
{
    /// <summary>
    /// Deterministic validation/report issue. Message is for humans; IssueId is for tests and tools.
    /// </summary>
    [Serializable]
    public readonly struct GenerationIssue
    {
        public GenerationIssue(
            GenerationIssueId id,
            GenerationIssueSeverity severity,
            MapGenerationStageId stageId,
            MapFieldId fieldId,
            string message)
        {
            if (id.IsNone)
                throw new ArgumentException("Generation issue id must not be None.", nameof(id));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Generation issue message must not be null, empty, or whitespace.", nameof(message));

            Id = id;
            Severity = severity;
            StageId = stageId;
            FieldId = fieldId;
            Message = message;
        }

        public GenerationIssueId Id { get; }
        public GenerationIssueSeverity Severity { get; }
        public MapGenerationStageId StageId { get; }
        public MapFieldId FieldId { get; }
        public string Message { get; }

        public bool IsFailure => Severity == GenerationIssueSeverity.Error || Severity == GenerationIssueSeverity.Fatal;
    }
}
