#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Owns the generated height-field samples and their diagnostics.
    /// Disposing the result disposes the sample buffer. Do not copy this value and dispose multiple copies.
    /// </summary>
    public struct HeightFieldGenerationResult : IDisposable
    {
        private NativeArray<float> samples;

        public HeightFieldGenerationResult(
            NativeArray<float> samples,
            HeightFieldSummary summary,
            GenerationStepReport report)
        {
            if (!samples.IsCreated)
                throw new ArgumentException("Generated sample buffer must be created.", nameof(samples));

            this.samples = samples;
            Summary = summary;
            Report = report;
        }

        public readonly NativeArray<float> Samples => samples;
        public HeightFieldSummary Summary { get; }
        public GenerationStepReport Report { get; }
        public bool IsCreated => samples.IsCreated;

        public void Dispose()
        {
            if (samples.IsCreated)
                samples.Dispose();
        }
    }
}
