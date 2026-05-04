using System;
using Unity.Collections;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Provides a typed non-owning view over generated map field storage.
    /// </summary>
    /// <typeparam name="T">Unmanaged element type stored by the field.</typeparam>
    /// <remarks>
    /// A map field view wraps workspace-owned native storage. It does not allocate memory and
    /// does not dispose memory. The owning <see cref="MapWorkspace"/> controls the lifetime of
    /// the underlying array.
    ///
    /// This type intentionally stores field identity and storage data only. It does not store
    /// <see cref="MapFieldDefinition"/> because definitions contain managed metadata and are not
    /// suitable for Burst or job capture.
    ///
    /// The default value is an invalid non-created field view. It is useful as a failed
    /// <c>out</c> parameter result, but it must not be read, written, or scheduled.
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
        /// Thrown when <paramref name="valueType"/> is incompatible with
        /// <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="values"/> has not been created.
        /// </exception>
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
        public int Length => values.IsCreated ? values.Length : 0;

        /// <summary>
        /// Gets or sets a field value by linear tile index.
        /// </summary>
        /// <param name="index">Linear tile index.</param>
        /// <returns>The field value at the specified index.</returns>
        /// <remarks>
        /// Bounds checking and disposed-array checking follow Unity's active safety
        /// configuration for <see cref="NativeArray{T}"/>.
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
        /// <exception cref="InvalidOperationException">
        /// Thrown when the field storage has not been created.
        /// </exception>
        /// <remarks>
        /// The returned array is a non-owning struct copy. Consumers must not dispose it unless
        /// they own the workspace storage.
        /// </remarks>
        public NativeArray<T> AsNativeArray()
        {
            ValidateCreated();

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
        /// Thrown when <paramref name="valueType"/> is incompatible with
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
        /// Gets the declared field value type expected for <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The expected field value type.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when <typeparamref name="T"/> is not supported by the workspace layer.
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