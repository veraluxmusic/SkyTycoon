using System;
using SkyTycoonMapGenerationVersion = Lokrain.SkyTycoon.MapGeneration.Domain.Version;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm 
{
    public sealed class Generator : IDisposable
    {
        private readonly FbmSettings settings;
        private NativeArray<float2> gradients2D;
        private JobHandle lastScheduledHandle;
        private bool disposed;

        public Generator(in FbmSettings settings, Allocator allocator = Allocator.Persistent)
        {
            this.settings = settings;
            gradients2D = new NativeArray<float2>(Constants.GradientTableSize2D, allocator, NativeArrayOptions.UninitializedMemory);
            GradientTable2D.Build(gradients2D);
        }

        public static SkyTycoonMapGenerationVersion AlgorithmVersion => new SkyTycoonMapGenerationVersion(
            Constants.AlgorithmVersionMajor,
            Constants.AlgorithmVersionMinor,
            Constants.AlgorithmVersionPatch);

        public bool IsCreated => gradients2D.IsCreated && !disposed;

        public NativeArray<float> Generate(in HeightFieldRequest request, Allocator allocator)
        {
            ThrowIfDisposed();
            request.Validate();

            var destination = new NativeArray<float>(request.SampleCount, allocator, NativeArrayOptions.UninitializedMemory);
            Schedule(request, destination).Complete();
            return destination;
        }

        public JobHandle Schedule(
            in HeightFieldRequest request,
            NativeArray<float> destination,
            JobHandle dependency = default)
        {
            ThrowIfDisposed();
            request.Validate();

            if (!destination.IsCreated || destination.Length != request.SampleCount)
            {
                throw new ArgumentException(
                    $"Destination buffer must be created and contain exactly {request.SampleCount} samples.",
                    nameof(destination));
            }

            int tileSize = request.GetEffectiveJobTileSize(settings.SuggestedJobTileSize);
            int tilesPerRow = (request.Width + tileSize - 1) / tileSize;
            int tileRows = (request.Height + tileSize - 1) / tileSize;
            int totalTileCount = tilesPerRow * tileRows;

            var job = new GenerateHeightFieldJob
            {
                Gradients2D = gradients2D,
                Destination = destination,
                Seed = settings.Seed,
                Width = request.Width,
                Height = request.Height,
                TileSize = tileSize,
                TilesPerRow = tilesPerRow,
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

            JobHandle handle = job.ScheduleParallel(totalTileCount, 1, dependency);
            lastScheduledHandle = JobHandle.CombineDependencies(lastScheduledHandle, handle);
            return handle;
        }

        public void CompleteOutstandingJobs()
        {
            lastScheduledHandle.Complete();
            lastScheduledHandle = default;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            lastScheduledHandle.Complete();

            if (gradients2D.IsCreated)
                gradients2D.Dispose();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed || !gradients2D.IsCreated)
                throw new ObjectDisposedException(nameof(Generator));
        }
    }
}
