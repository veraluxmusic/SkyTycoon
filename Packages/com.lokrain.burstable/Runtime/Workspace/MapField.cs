using System;
using Unity.Collections;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Provides a typed non-owning view over generated map field storage.
    /// </summary>
    /// <typeparam name="T">Unmanaged element type stored by the field.</typeparam>
    /// <remarks>
    /// A map field provides typed access to a contiguous field array owned by a
    /// <see cref="MapWorkspace"/>. This type does not allocate memory and does not dispose
    /// memory. Field lifetime is owned by the workspace that created the view.
    ///
    /// This type intentionally stores only unmanaged field identity and storage data. It does
    /// not store <see cref="MapFieldDefinition"/> because definitions include managed metadata
    /// such as symbolic names and are not suitable for Burst or job capture.
    ///
    /// The underlying <see cref="NativeArray{T}"/> may be passed directly to jobs when the
    /// caller owns the required lifetime and dependency safety.
    /// </remarks>
    public struct MapField<T>
        where T : unmanaged
    {
        private NativeArray<T> values;

        /// <summary>
        /// Initializes a typed non-owning field view.
        /// </summary>
        /// <param name="id">Stable field identifier.</param>
        /// <param name="valueType">Declared field value type.</param>
        /// <param name="values">Workspace-owned field values.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="valueType"/> is not compatible with
        /// <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="values"/> has not been created.
        /// </exception>
        /// <remarks>
        /// This constructor is internal because workspaces own field storage and should be the
        /// only runtime source of field views.
        /// </remarks>
        internal MapField(
            MapFieldId id,
            MapFieldValueType valueType,
            NativeArray<T> values)
        {
            id.Validate();
            ValidateValueType(valueType);

            if (!values.IsCreated)
            {
                throw new InvalidOperationException(
                    "Map field values must be created.");
            }

            Id = id;
            ValueType = valueType;
            this.values = values;
        }

        /// <summary>
        /// Gets the stable field identifier.
        /// </summary>
        public MapFieldId Id { get; }

        /// <summary>
        /// Gets the declared field value type.
        /// </summary>
        public MapFieldValueType ValueType { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying native storage has been created.
        /// </summary>
        public bool IsCreated => values.IsCreated;

        /// <summary>
        /// Gets the number of elements in the field.
        /// </summary>
        /// <remarks>
        /// For map-tile fields, this value should match the owning workspace tile count.
        /// </remarks>
        public int Length => values.IsCreated ? values.Length : 0;

        /// <summary>
        /// Gets or sets a field value by linear tile index.
        /// </summary>
        /// <param name="index">Linear tile index.</param>
        /// <returns>The field value at the specified index.</returns>
        /// <remarks>
        /// Bounds checking follows the active Unity safety configuration for
        /// <see cref="NativeArray{T}"/>.
        /// </remarks>
        public T this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        /// <summary>
        /// Gets the underlying workspace-owned native array.
        /// </summary>
        /// <returns>The underlying native array.</returns>
        /// <remarks>
        /// The returned array is a non-owning struct copy. Disposing it is equivalent to
        /// disposing the workspace-owned storage and must be avoided by consumers that do not
        /// own the workspace lifetime.
        /// </remarks>
        public NativeArray<T> AsNativeArray()
        {
            return values;
        }

        /// <summary>
        /// Validates that the field has created native storage.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the field storage has not been created.
        /// </exception>
        public void ValidateCreated()
        {
            if (!values.IsCreated)
            {
                throw new InvalidOperationException(
                    "Map field values have not been created.");
            }
        }

        /// <summary>
        /// Validates that the field length matches an expected length.
        /// </summary>
        /// <param name="expectedLength">Expected field length.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the field storage has not been created.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="expectedLength"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the field length does not match <paramref name="expectedLength"/>.
        /// </exception>
        public void ValidateLength(int expectedLength)
        {
            ValidateCreated();

            if (expectedLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedLength),
                    expectedLength,
                    "Expected field length must not be negative.");
            }

            if (values.Length != expectedLength)
            {
                throw new ArgumentException(
                    "Map field length does not match the expected length.",
                    nameof(expectedLength));
            }
        }

        /// <summary>
        /// Validates that a declared field value type is compatible with
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <param name="valueType">Declared field value type.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="valueType"/> is not compatible with
        /// <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when <typeparamref name="T"/> is not supported as a map field element type.
        /// </exception>
        private static void ValidateValueType(MapFieldValueType valueType)
        {
            MapFieldValueType expectedValueType = GetExpectedValueType();

            if (valueType != expectedValueType)
            {
                throw new ArgumentException(
                    "Map field value type is not compatible with the typed field view.",
                    nameof(valueType));
            }
        }

        /// <summary>
        /// Gets the declared map field value type expected for <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The expected map field value type.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when <typeparamref name="T"/> is not supported as a map field element type.
        /// </exception>
        private static MapFieldValueType GetExpectedValueType()
        {
            Type type = typeof(T);

            if (type == typeof(int))
            {
                return MapFieldValueType.Int32;
            }

            if (type == typeof(byte))
            {
                return MapFieldValueType.UInt8;
            }

            throw new NotSupportedException(
                "The requested map field element type is not supported.");
        }
    }
}