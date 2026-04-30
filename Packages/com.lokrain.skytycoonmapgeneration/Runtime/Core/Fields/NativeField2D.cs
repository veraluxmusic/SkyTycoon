#nullable enable

using System;
using Unity.Collections;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    /// <summary>
    /// Thin typed owner/view for a rectangular generated field.
    ///
    /// This type is intentionally small: algorithms should schedule jobs over the exposed
    /// NativeArray and dimensions instead of introducing virtual field abstractions in hot paths.
    /// </summary>
    public struct NativeField2D<T> : IDisposable where T : unmanaged
    {
        private NativeArray<T> _samples;

        public NativeField2D(
            MapFieldId fieldId,
            HeightFieldDimensions dimensions,
            Allocator allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            if (fieldId.IsNone)
                throw new ArgumentException("Native field id must not be None.", nameof(fieldId));

            dimensions.Validate();

            FieldId = fieldId;
            Dimensions = dimensions;
            _samples = new NativeArray<T>(dimensions.SampleCount, allocator, options);
        }

        public MapFieldId FieldId { get; }
        public HeightFieldDimensions Dimensions { get; }
        public bool IsCreated => _samples.IsCreated;
        public int Length => _samples.IsCreated ? _samples.Length : 0;
        public readonly NativeArray<T> Samples => _samples;

        public T this[int index]
        {
            get
            {
                EnsureCreated();
                Dimensions.ValidateIndex(index);
                return _samples[index];
            }
            set
            {
                EnsureCreated();
                Dimensions.ValidateIndex(index);
                _samples[index] = value;
            }
        }

        public T this[int x, int y]
        {
            get
            {
                EnsureCreated();
                return _samples[Dimensions.ToIndex(x, y)];
            }
            set
            {
                EnsureCreated();
                _samples[Dimensions.ToIndex(x, y)] = value;
            }
        }

        public void Fill(T value)
        {
            EnsureCreated();

            for (int i = 0; i < _samples.Length; i++)
                _samples[i] = value;
        }

        public void Dispose()
        {
            if (_samples.IsCreated)
                _samples.Dispose();
        }

        private void EnsureCreated()
        {
            if (!_samples.IsCreated)
                throw new ObjectDisposedException(nameof(NativeField2D<T>), "Native field has not been created or has already been disposed.");
        }
    }
}
