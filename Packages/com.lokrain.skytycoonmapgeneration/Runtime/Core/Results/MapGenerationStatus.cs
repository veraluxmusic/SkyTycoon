#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Core.Results
{
    public enum MapGenerationStatus : byte
    {
        Unknown = 0,
        Succeeded = 1,
        FailedValidation = 2,
        FailedException = 3,
        Cancelled = 4
    }
}
