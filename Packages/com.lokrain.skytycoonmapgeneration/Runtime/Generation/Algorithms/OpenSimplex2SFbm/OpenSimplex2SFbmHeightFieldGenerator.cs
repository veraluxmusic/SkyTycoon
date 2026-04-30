#nullable enable

using System;
using System.Diagnostics;
using Lokrain.SkyTycoon.MapGeneration.Domain;
using Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Generation.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    /// <summary>
    /// Production generator for the F001 OpenSimplex2S fBM height-field source stage.
    /// It owns the immutable gradient lookup table required by scheduled jobs.
    /// </summary>
    public sealed class OpenSimplex2SFbmHeightFieldGenerator : IDisposable
    {
        private readonly OpenSimplex2SFbmSettings settings;
        private NativeArray<float2> gradients2D;
        private JobHandle outstandingJobs;
        private bool disposed;

        public OpenSimplex2SFbmHeightFieldGenerator(
            in OpenSimplex2SFbmSettings settings,
            Allocator allocator = Allocator.Persistent)
        {
            this.settings = settings;
            gradients2D = new NativeArray<float2>(OpenSimplex2SFbmConstants.GradientTableSize2D, allocator, NativeArrayOptions.UninitializedMemory);
            OpenSimplex2SGradientTable2D.Build(gradients2D);
        }

        public static GenerationStepId StepId => new(OpenSimplex2SFbmConstants.StepId);

        public static MapGenerationVersion AlgorithmVersion => new(
            OpenSimplex2SFbmConstants.AlgorithmVersionMajor,
            OpenSimplex2SFbmConstants.AlgorithmVersionMinor,
            OpenSimplex2SFbmConstants.AlgorithmVersionPatch);

        public OpenSimplex2SFbmSettings Settings => settings;
        public bool IsCreated => gradients2D.IsCreated && !disposed;

        public HeightFieldGenerationResult Generate(
            in HeightFieldRequest request,
            Allocator allocator,
            GenerationDiagnosticsMode diagnosticsMode = GenerationDiagnosticsMode.Summary)
        {
            ThrowIfDisposed();
            request.Validate();

            NativeArray<float> samples = new(request.SampleCount, allocator, NativeArrayOptions.UninitializedMemory);
            long totalStart = Stopwatch.GetTimestamp();
            long generationStart = totalStart;

            int effectiveJobTileSize = request.GetEffectiveJobTileSize(settings.SuggestedJobTileSize);
            Schedule(request, samples).Complete();

            long generationEnd = Stopwatch.GetTimestamp();
            HeightFieldSummary summary = HeightFieldSummary.CreateInvalid();
            long diagnosticsTicks = 0L;

            if (diagnosticsMode == GenerationDiagnosticsMode.Summary)
            {
                long diagnosticsStart = Stopwatch.GetTimestamp();
                summary = HeightFieldDiagnostics.BuildSummary(samples, request.OutputRange, Allocator.TempJob);
                long diagnosticsEnd = Stopwatch.GetTimestamp();
                diagnosticsTicks = diagnosticsEnd - diagnosticsStart;
            }

            long totalEnd = Stopwatch.GetTimestamp();
            ulong hash = summary.IsValid ? summary.QuantizedHash64 : 0UL;

            GenerationStepReport report = new(
                stepId: StepId,
                algorithmVersion: AlgorithmVersion,
                dimensions: request.Dimensions,
                outputRange: request.OutputRange,
                seed: settings.Seed,
                jobTileSize: effectiveJobTileSize,
                diagnosticsMode: diagnosticsMode,
                generationTicks: generationEnd - generationStart,
                diagnosticsTicks: diagnosticsTicks,
                totalTicks: totalEnd - totalStart,
                outputHash64: hash,
                settingsFingerprint: settings.CreateFingerprint());

            return new HeightFieldGenerationResult(samples, summary, report);
        }

        public NativeArray<float> GenerateSamples(in HeightFieldRequest request, Allocator allocator)
        {
            ThrowIfDisposed();
            request.Validate();

            NativeArray<float> samples = new(request.SampleCount, allocator, NativeArrayOptions.UninitializedMemory);
            Schedule(request, samples).Complete();
            return samples;
        }

        public JobHandle Schedule(
    in HeightFieldRequest request,
    NativeArray<float> destination,
    JobHandle dependency = default)
        {
            ThrowIfDisposed();
            request.Validate();

            if (!destination.IsCreated)
                throw new ArgumentException("Destination buffer must be created.", nameof(destination));

            if (destination.Length != request.SampleCount)
                throw new ArgumentException("Destination buffer length must match the requested sample count.", nameof(destination));

            int effectiveJobTileSize = request.GetEffectiveJobTileSize(settings.SuggestedJobTileSize);
            int innerLoopBatchCount = math.max(1, effectiveJobTileSize * effectiveJobTileSize);

            var job = new GenerateOpenSimplex2SFbmHeightFieldJob
            {
                Gradients2D = gradients2D,
                Destination = destination,

                Seed = settings.Seed,

                Width = request.Width,
                Height = request.Height,

                OctaveCount = settings.OctaveCount,

                BaseFrequency = settings.BaseFrequency,
                Persistence = settings.Persistence,
                Lacunarity = settings.Lacunarity,

                NoiseSpaceOrigin = request.NoiseSpaceOrigin,
                SampleSpacing = request.CalculateSampleSpacing(),
                GlobalNoiseOffset = settings.GlobalNoiseOffset,

                Orientation = settings.Orientation,
                OutputRange = request.OutputRange,

                NormalizeByAmplitude = settings.NormalizeByAmplitude
            };

            JobHandle handle = job.ScheduleParallel(request.SampleCount, innerLoopBatchCount, dependency);
            outstandingJobs = JobHandle.CombineDependencies(outstandingJobs, handle);
            return handle;
        }

        public void CompleteOutstandingJobs()
        {
            outstandingJobs.Complete();
            outstandingJobs = default;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            outstandingJobs.Complete();

            if (gradients2D.IsCreated)
                gradients2D.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed || !gradients2D.IsCreated)
                throw new ObjectDisposedException(nameof(OpenSimplex2SFbmHeightFieldGenerator));
        }
    }
}
