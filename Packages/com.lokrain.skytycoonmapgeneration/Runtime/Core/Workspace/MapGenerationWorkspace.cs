#nullable enable

using System;
using Unity.Collections;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Workspace
{
    /// <summary>
    /// Reusable temporary storage for map generation.
    ///
    /// The workspace is managed by orchestration code and reused across runs to avoid repeated
    /// NativeArray allocations. Jobs receive the arrays, never this workspace object.
    /// </summary>
    public sealed class MapGenerationWorkspace : IDisposable
    {
        private readonly Allocator _allocator;

        private NativeArray<float> _floatA;
        private NativeArray<float> _floatB;
        private NativeArray<int> _intA;
        private NativeArray<int> _intB;
        private NativeArray<byte> _byteA;
        private NativeArray<byte> _byteB;

        public MapGenerationWorkspace(HeightFieldDimensions dimensions, Allocator allocator = Allocator.Persistent)
        {
            dimensions.Validate();

            if (allocator == Allocator.Invalid || allocator == Allocator.None)
                throw new ArgumentOutOfRangeException(nameof(allocator), allocator, "Workspace allocator must be a persistent or temporary-job allocator.");

            Dimensions = dimensions;
            _allocator = allocator;
        }

        public HeightFieldDimensions Dimensions { get; private set; }
        public int Capacity => Dimensions.SampleCount;

        public void Resize(HeightFieldDimensions dimensions)
        {
            dimensions.Validate();

            if (Dimensions == dimensions)
                return;

            DisposeBuffers();
            Dimensions = dimensions;
        }

        public NativeArray<float> GetFloatA()
        {
            EnsureCapacity(ref _floatA);
            return _floatA;
        }

        public NativeArray<float> GetFloatB()
        {
            EnsureCapacity(ref _floatB);
            return _floatB;
        }

        public NativeArray<int> GetIntA()
        {
            EnsureCapacity(ref _intA);
            return _intA;
        }

        public NativeArray<int> GetIntB()
        {
            EnsureCapacity(ref _intB);
            return _intB;
        }

        public NativeArray<byte> GetByteA()
        {
            EnsureCapacity(ref _byteA);
            return _byteA;
        }

        public NativeArray<byte> GetByteB()
        {
            EnsureCapacity(ref _byteB);
            return _byteB;
        }

        public void Dispose()
        {
            DisposeBuffers();
        }

        private void EnsureCapacity<T>(ref NativeArray<T> buffer) where T : unmanaged
        {
            if (buffer.IsCreated && buffer.Length == Capacity)
                return;

            if (buffer.IsCreated)
                buffer.Dispose();

            buffer = new NativeArray<T>(Capacity, _allocator, NativeArrayOptions.UninitializedMemory);
        }

        private void DisposeBuffers()
        {
            DisposeIfCreated(ref _floatA);
            DisposeIfCreated(ref _floatB);
            DisposeIfCreated(ref _intA);
            DisposeIfCreated(ref _intB);
            DisposeIfCreated(ref _byteA);
            DisposeIfCreated(ref _byteB);
        }

        private static void DisposeIfCreated<T>(ref NativeArray<T> buffer) where T : unmanaged
        {
            if (buffer.IsCreated)
                buffer.Dispose();
        }
    }
}
