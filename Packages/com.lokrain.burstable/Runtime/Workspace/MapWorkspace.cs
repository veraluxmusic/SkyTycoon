using System;
using System.Collections.Generic;
using Lokrain.Burstable.Generation;
using Unity.Collections;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Owns native storage for generated map fields.
    /// </summary>
    /// <remarks>
    /// A map workspace owns generated field arrays for one rectangular map. Field storage is
    /// allocated from a validated <see cref="MapFieldRegistry"/> and has one element per tile.
    ///
    /// Field views returned by this workspace are non-owning handles and must not be used after
    /// the workspace is disposed.
    ///
    /// The workspace is managed setup/runtime infrastructure. It should not be captured by
    /// Burst-compiled jobs. Resolve required fields on the managed side and pass their native
    /// arrays to jobs.
    ///
    /// Workspace allocation supports <see cref="Allocator.Persistent"/> and
    /// <see cref="Allocator.TempJob"/>. <see cref="Allocator.Temp"/> is intentionally rejected.
    /// </remarks>
    public sealed class MapWorkspace : IDisposable
    {
        private readonly Dictionary<int, NativeArray<int>> int32Fields;
        private readonly Dictionary<int, NativeArray<byte>> uint8Fields;

        private bool isDisposed;

        /// <summary>
        /// Initializes a new map workspace and allocates storage for every registered field.
        /// </summary>
        /// <param name="dimensions">Dimensions of the generated rectangular tile map.</param>
        /// <param name="fieldRegistry">Registry containing field definitions to allocate.</param>
        /// <param name="allocator">Allocator used for native field storage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fieldRegistry"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="dimensions"/> are invalid.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is unsupported or when the registry
        /// contains an unsupported field value type.
        /// </exception>
        public MapWorkspace(
            MapDimensions dimensions,
            MapFieldRegistry fieldRegistry,
            Allocator allocator)
        {
            dimensions.Validate();

            FieldRegistry = fieldRegistry ?? throw new ArgumentNullException(nameof(fieldRegistry));
            ValidateAllocator(allocator);

            Dimensions = dimensions;
            Allocator = allocator;

            int32Fields = new Dictionary<int, NativeArray<int>>(FieldRegistry.Count);
            uint8Fields = new Dictionary<int, NativeArray<byte>>(FieldRegistry.Count);

            try
            {
                AllocateFields();
            }
            catch
            {
                DisposeAllocatedFields();
                throw;
            }
        }

        /// <summary>
        /// Gets the dimensions of the generated rectangular tile map.
        /// </summary>
        public MapDimensions Dimensions { get; }

        /// <summary>
        /// Gets the field registry used to allocate this workspace.
        /// </summary>
        public MapFieldRegistry FieldRegistry { get; }

        /// <summary>
        /// Gets the allocator used for native field storage.
        /// </summary>
        public Allocator Allocator { get; }

        /// <summary>
        /// Gets the number of tiles represented by each map field.
        /// </summary>
        public int Length => Dimensions.Length;

        /// <summary>
        /// Gets a value indicating whether this workspace has been disposed.
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// Determines whether this workspace contains storage for the specified field.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when the workspace contains the field; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        public bool Contains(MapFieldId id)
        {
            ThrowIfDisposed();

            return FieldRegistry.Contains(id);
        }

        /// <summary>
        /// Determines whether this workspace contains storage for the specified field name.
        /// </summary>
        /// <param name="name">Stable symbolic field name.</param>
        /// <returns>
        /// <see langword="true"/> when the workspace contains the field; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        public bool ContainsName(string name)
        {
            ThrowIfDisposed();

            return FieldRegistry.ContainsName(name);
        }

        /// <summary>
        /// Gets a signed 32-bit integer field view by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>A typed non-owning field view.</returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the workspace does not contain the requested field.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested field is not an <see cref="MapFieldValueType.Int32"/>
        /// field.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when registry metadata and allocated storage are inconsistent.
        /// </exception>
        public MapField<int> GetInt32Field(MapFieldId id)
        {
            ThrowIfDisposed();

            MapFieldDefinition definition = FieldRegistry.Get(id);
            ValidateRequestedValueType(definition, MapFieldValueType.Int32);

            if (!int32Fields.TryGetValue(definition.Id.Value, out NativeArray<int> values))
            {
                throw new InvalidOperationException(
                    "Map workspace does not contain native storage for the requested Int32 field.");
            }

            return new MapField<int>(
                definition.Id,
                definition.ValueType,
                values);
        }

        /// <summary>
        /// Gets an unsigned 8-bit integer field view by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>A typed non-owning field view.</returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the workspace does not contain the requested field.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the requested field is not an <see cref="MapFieldValueType.UInt8"/>
        /// field.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when registry metadata and allocated storage are inconsistent.
        /// </exception>
        public MapField<byte> GetUInt8Field(MapFieldId id)
        {
            ThrowIfDisposed();

            MapFieldDefinition definition = FieldRegistry.Get(id);
            ValidateRequestedValueType(definition, MapFieldValueType.UInt8);

            if (!uint8Fields.TryGetValue(definition.Id.Value, out NativeArray<byte> values))
            {
                throw new InvalidOperationException(
                    "Map workspace does not contain native storage for the requested UInt8 field.");
            }

            return new MapField<byte>(
                definition.Id,
                definition.ValueType,
                values);
        }

        /// <summary>
        /// Attempts to get a signed 32-bit integer field view by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <param name="field">
        /// Typed non-owning field view when found; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching signed 32-bit integer field exists;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        public bool TryGetInt32Field(
            MapFieldId id,
            out MapField<int> field)
        {
            ThrowIfDisposed();

            if (!FieldRegistry.TryGet(id, out MapFieldDefinition definition) ||
                definition.ValueType != MapFieldValueType.Int32 ||
                !int32Fields.TryGetValue(definition.Id.Value, out NativeArray<int> values))
            {
                field = default;
                return false;
            }

            field = new MapField<int>(
                definition.Id,
                definition.ValueType,
                values);

            return true;
        }

        /// <summary>
        /// Attempts to get an unsigned 8-bit integer field view by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <param name="field">
        /// Typed non-owning field view when found; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching unsigned 8-bit integer field exists;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        public bool TryGetUInt8Field(
            MapFieldId id,
            out MapField<byte> field)
        {
            ThrowIfDisposed();

            if (!FieldRegistry.TryGet(id, out MapFieldDefinition definition) ||
                definition.ValueType != MapFieldValueType.UInt8 ||
                !uint8Fields.TryGetValue(definition.Id.Value, out NativeArray<byte> values))
            {
                field = default;
                return false;
            }

            field = new MapField<byte>(
                definition.Id,
                definition.ValueType,
                values);

            return true;
        }

        /// <summary>
        /// Disposes all native field storage owned by this workspace.
        /// </summary>
        /// <remarks>
        /// All jobs using arrays from this workspace must be completed before disposal.
        /// Existing field views become invalid after disposal.
        /// </remarks>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeAllocatedFields();
            isDisposed = true;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allocates native storage for every registered field definition.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when a registered field uses an unsupported value type.
        /// </exception>
        private void AllocateFields()
        {
            for (int i = 0; i < FieldRegistry.Count; i++)
            {
                MapFieldDefinition definition = FieldRegistry.GetAt(i);

                switch (definition.ValueType)
                {
                    case MapFieldValueType.Int32:
                        int32Fields.Add(
                            definition.Id.Value,
                            new NativeArray<int>(
                                Length,
                                Allocator,
                                NativeArrayOptions.ClearMemory));
                        break;

                    case MapFieldValueType.UInt8:
                        uint8Fields.Add(
                            definition.Id.Value,
                            new NativeArray<byte>(
                                Length,
                                Allocator,
                                NativeArrayOptions.ClearMemory));
                        break;

                    default:
                        throw new ArgumentException(
                            "Map workspace cannot allocate unsupported field value type.");
                }
            }
        }

        /// <summary>
        /// Disposes all native arrays allocated by this workspace.
        /// </summary>
        private void DisposeAllocatedFields()
        {
            foreach (NativeArray<int> field in int32Fields.Values)
            {
                if (field.IsCreated)
                {
                    field.Dispose();
                }
            }

            foreach (NativeArray<byte> field in uint8Fields.Values)
            {
                if (field.IsCreated)
                {
                    field.Dispose();
                }
            }

            int32Fields.Clear();
            uint8Fields.Clear();
        }

        /// <summary>
        /// Validates that an allocator can be used for workspace-owned native field storage.
        /// </summary>
        /// <param name="allocator">Allocator to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="allocator"/> is not <see cref="Allocator.Persistent"/>
        /// or <see cref="Allocator.TempJob"/>.
        /// </exception>
        private static void ValidateAllocator(Allocator allocator)
        {
            if (allocator != Allocator.Persistent &&
                allocator != Allocator.TempJob)
            {
                throw new ArgumentException(
                    "Map workspace allocator must be Persistent or TempJob.",
                    nameof(allocator));
            }
        }

        /// <summary>
        /// Validates that a field definition has the requested value type.
        /// </summary>
        /// <param name="definition">Field definition to validate.</param>
        /// <param name="expectedValueType">Expected value type.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the definition value type does not match
        /// <paramref name="expectedValueType"/>.
        /// </exception>
        private static void ValidateRequestedValueType(
            MapFieldDefinition definition,
            MapFieldValueType expectedValueType)
        {
            if (definition.ValueType != expectedValueType)
            {
                throw new ArgumentException(
                    "Requested map field value type does not match the field definition.",
                    nameof(definition));
            }
        }

        /// <summary>
        /// Throws when this workspace has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this workspace has been disposed.
        /// </exception>
        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(MapWorkspace));
            }
        }
    }
}