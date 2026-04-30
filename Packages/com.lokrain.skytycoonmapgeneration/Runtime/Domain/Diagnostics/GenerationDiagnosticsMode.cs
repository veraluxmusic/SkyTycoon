#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics
{
    /// <summary>
    /// Controls how much deterministic diagnostic data a generation call produces.
    /// </summary>
    public enum GenerationDiagnosticsMode : byte
    {
        /// <summary>
        /// Generate only the output samples. This is useful for hot paths and micro-benchmarks.
        /// </summary>
        None = 0,

        /// <summary>
        /// Generate summary statistics, a quantized output hash and a histogram.
        /// </summary>
        Summary = 1
    }
}
