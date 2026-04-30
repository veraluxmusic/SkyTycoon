#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics
{
    /// <summary>
    /// Immutable runtime report produced by a generation stage.
    /// Reports are intentionally plain data so they can be logged, exported, tested and compared in CI.
    /// </summary>
    public readonly struct GenerationStepReport
    {
        public readonly GenerationStepId StepId;
        public readonly MapGenerationVersion AlgorithmVersion;
        public readonly HeightFieldDimensions Dimensions;
        public readonly HeightFieldValueRange OutputRange;
        public readonly long Seed;
        public readonly int JobTileSize;
        public readonly GenerationDiagnosticsMode DiagnosticsMode;
        public readonly long GenerationTicks;
        public readonly long DiagnosticsTicks;
        public readonly long TotalTicks;
        public readonly ulong OutputHash64;
        public readonly string SettingsFingerprint;

        public GenerationStepReport(
            GenerationStepId stepId,
            MapGenerationVersion algorithmVersion,
            HeightFieldDimensions dimensions,
            HeightFieldValueRange outputRange,
            long seed,
            int jobTileSize,
            GenerationDiagnosticsMode diagnosticsMode,
            long generationTicks,
            long diagnosticsTicks,
            long totalTicks,
            ulong outputHash64,
            string settingsFingerprint)
        {
            if (jobTileSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(jobTileSize), "Job tile size must be greater than zero.");
            if (generationTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(generationTicks), "Generation ticks must be zero or greater.");
            if (diagnosticsTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(diagnosticsTicks), "Diagnostics ticks must be zero or greater.");
            if (totalTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(totalTicks), "Total ticks must be zero or greater.");
            if (string.IsNullOrWhiteSpace(settingsFingerprint))
                throw new ArgumentException("Settings fingerprint must not be null, empty or whitespace.", nameof(settingsFingerprint));

            StepId = stepId;
            AlgorithmVersion = algorithmVersion;
            Dimensions = dimensions;
            OutputRange = outputRange;
            Seed = seed;
            JobTileSize = jobTileSize;
            DiagnosticsMode = diagnosticsMode;
            GenerationTicks = generationTicks;
            DiagnosticsTicks = diagnosticsTicks;
            TotalTicks = totalTicks;
            OutputHash64 = outputHash64;
            SettingsFingerprint = settingsFingerprint;
        }

        public double GenerationMilliseconds => TicksToMilliseconds(GenerationTicks);
        public double DiagnosticsMilliseconds => TicksToMilliseconds(DiagnosticsTicks);
        public double TotalMilliseconds => TicksToMilliseconds(TotalTicks);

        public override string ToString()
        {
            return StepId + " v" + AlgorithmVersion
                + " " + Dimensions
                + " seed=" + Seed
                + " tile=" + JobTileSize
                + " ms=" + TotalMilliseconds.ToString("0.###")
                + " hash=0x" + OutputHash64.ToString("X16");
        }

        private static double TicksToMilliseconds(long ticks)
        {
            return ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }
    }
}
