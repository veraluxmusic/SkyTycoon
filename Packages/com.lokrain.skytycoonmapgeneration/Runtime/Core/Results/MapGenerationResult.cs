#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Results
{
    /// <summary>
    /// Top-level result envelope. Concrete generated fields/artifacts are attached by later slices.
    /// </summary>
    [Serializable]
    public sealed class MapGenerationResult
    {
        public MapGenerationResult(
            MapGenerationRequest request,
            MapGenerationStatus status,
            StableHash128 artifactHash,
            MapValidationReport validationReport)
        {
            Request = request;
            Status = status;
            ArtifactHash = artifactHash;
            ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
        }

        public MapGenerationRequest Request { get; }
        public MapGenerationStatus Status { get; }
        public StableHash128 ArtifactHash { get; }
        public MapValidationReport ValidationReport { get; }

        public bool Succeeded => Status == MapGenerationStatus.Succeeded && ValidationReport.Passed;
    }
}
