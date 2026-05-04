using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lokrain.Burstable.Generation;
using Lokrain.Burstable.Workspace;
using Unity.Collections;

namespace Lokrain.Burstable.Tests.Runtime.Workspace
{
    /// <summary>
    /// Tests native storage ownership and typed field access behavior owned by
    /// <see cref="MapWorkspace"/>.
    /// </summary>
    /// <remarks>
    /// These tests validate the workspace layer independently from generation stages. The
    /// workspace owns native field allocation, field lookup, typed field view creation, and
    /// disposal. It does not own stage ordering, generated value semantics, terrain
    /// classification, preview behavior, or job scheduling policy.
    ///
    /// Field definitions are supplied through <see cref="MapFieldRegistry"/> because workspace
    /// storage is intentionally registry-backed and allocated up front.
    /// </remarks>
    public sealed class MapWorkspaceTests
    {
        private static readonly MapFieldDefinition Int32Definition = new MapFieldDefinition(
            new MapFieldId(101),
            MapFieldValueType.Int32,
            "test-int32");

        private static readonly MapFieldDefinition UInt8Definition = new MapFieldDefinition(
            new MapFieldId(102),
            MapFieldValueType.UInt8,
            "test-uint8");

        /// <summary>
        /// Verifies that constructing a workspace with registered fields allocates typed native
        /// storage with one element per map tile.
        /// </summary>
        [Test]
        public void Constructor_WhenRegistryContainsFields_AllocatesOneElementPerTile()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition, UInt8Definition),
                Allocator.Persistent);

            try
            {
                MapField<int> int32Field = workspace.GetInt32Field(Int32Definition.Id);
                MapField<byte> uint8Field = workspace.GetUInt8Field(UInt8Definition.Id);

                Assert.IsTrue(int32Field.IsCreated);
                Assert.IsTrue(uint8Field.IsCreated);
                Assert.AreEqual(workspace.Length, int32Field.Length);
                Assert.AreEqual(workspace.Length, uint8Field.Length);
                Assert.AreEqual(CreateDimensions().Length, int32Field.Length);
                Assert.AreEqual(CreateDimensions().Length, uint8Field.Length);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that workspace-owned field storage is clear-initialized.
        /// </summary>
        /// <remarks>
        /// Generation stages should still write every value they own. Clear initialization is a
        /// safety baseline for deterministic setup and diagnostics, not a substitute for stage
        /// ownership.
        /// </remarks>
        [Test]
        public void Constructor_WhenFieldsAreAllocated_ClearInitializesFieldStorage()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition, UInt8Definition),
                Allocator.Persistent);

            try
            {
                NativeArray<int> int32Values = workspace
                    .GetInt32Field(Int32Definition.Id)
                    .AsNativeArray();

                NativeArray<byte> uint8Values = workspace
                    .GetUInt8Field(UInt8Definition.Id)
                    .AsNativeArray();

                for (int i = 0; i < workspace.Length; i++)
                {
                    Assert.AreEqual(0, int32Values[i]);
                    Assert.AreEqual(0, uint8Values[i]);
                }
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that <see cref="Allocator.Persistent"/> is accepted for workspace storage.
        /// </summary>
        [Test]
        public void Constructor_WhenAllocatorIsPersistent_DoesNotThrow()
        {
            MapWorkspace workspace = CreateWorkspace(
                MapFieldRegistry.Empty,
                Allocator.Persistent);

            try
            {
                Assert.AreEqual(Allocator.Persistent, workspace.Allocator);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that <see cref="Allocator.TempJob"/> is accepted for short-lived workspace
        /// storage.
        /// </summary>
        [Test]
        public void Constructor_WhenAllocatorIsTempJob_DoesNotThrow()
        {
            MapWorkspace workspace = CreateWorkspace(
                MapFieldRegistry.Empty,
                Allocator.TempJob);

            try
            {
                Assert.AreEqual(Allocator.TempJob, workspace.Allocator);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that <see cref="Allocator.Temp"/> is rejected for workspace-owned storage.
        /// </summary>
        /// <remarks>
        /// Workspaces are expected to back scheduled generation jobs. Temp allocations are not a
        /// safe policy for that ownership model.
        /// </remarks>
        [Test]
        public void Constructor_WhenAllocatorIsTemp_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MapWorkspace(
                CreateDimensions(),
                MapFieldRegistry.Empty,
                Allocator.Temp));
        }

        /// <summary>
        /// Verifies that <see cref="Allocator.None"/> is rejected for workspace-owned storage.
        /// </summary>
        [Test]
        public void Constructor_WhenAllocatorIsNone_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MapWorkspace(
                CreateDimensions(),
                MapFieldRegistry.Empty,
                Allocator.None));
        }

        /// <summary>
        /// Verifies that <see cref="Allocator.Invalid"/> is rejected for workspace-owned storage.
        /// </summary>
        [Test]
        public void Constructor_WhenAllocatorIsInvalid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MapWorkspace(
                CreateDimensions(),
                MapFieldRegistry.Empty,
                Allocator.Invalid));
        }

        /// <summary>
        /// Verifies that constructing a workspace with a null registry is rejected.
        /// </summary>
        [Test]
        public void Constructor_WhenRegistryIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MapWorkspace(
                CreateDimensions(),
                null,
                Allocator.Persistent));
        }

        /// <summary>
        /// Verifies that <see cref="MapWorkspace.Contains"/> returns true for registered fields.
        /// </summary>
        [Test]
        public void Contains_WhenFieldIsRegistered_ReturnsTrue()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                Assert.IsTrue(workspace.Contains(Int32Definition.Id));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that <see cref="MapWorkspace.Contains"/> returns false for missing fields.
        /// </summary>
        [Test]
        public void Contains_WhenFieldIsMissing_ReturnsFalse()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                Assert.IsFalse(workspace.Contains(new MapFieldId(999)));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that signed 32-bit fields can be resolved through the matching typed access
        /// method.
        /// </summary>
        [Test]
        public void GetInt32Field_WhenFieldIsInt32_ReturnsTypedField()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                MapField<int> field = workspace.GetInt32Field(Int32Definition.Id);

                Assert.AreEqual(Int32Definition.Id, field.Id);
                Assert.AreEqual(MapFieldValueType.Int32, field.ValueType);
                Assert.AreEqual(workspace.Length, field.Length);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that unsigned 8-bit fields can be resolved through the matching typed access
        /// method.
        /// </summary>
        [Test]
        public void GetUInt8Field_WhenFieldIsUInt8_ReturnsTypedField()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(UInt8Definition),
                Allocator.Persistent);

            try
            {
                MapField<byte> field = workspace.GetUInt8Field(UInt8Definition.Id);

                Assert.AreEqual(UInt8Definition.Id, field.Id);
                Assert.AreEqual(MapFieldValueType.UInt8, field.ValueType);
                Assert.AreEqual(workspace.Length, field.Length);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that requesting a signed 32-bit view for an unsigned 8-bit field is rejected.
        /// </summary>
        [Test]
        public void GetInt32Field_WhenFieldIsUInt8_ThrowsArgumentException()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(UInt8Definition),
                Allocator.Persistent);

            try
            {
                Assert.Throws<ArgumentException>(() => workspace.GetInt32Field(UInt8Definition.Id));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that requesting an unsigned 8-bit view for a signed 32-bit field is rejected.
        /// </summary>
        [Test]
        public void GetUInt8Field_WhenFieldIsInt32_ThrowsArgumentException()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                Assert.Throws<ArgumentException>(() => workspace.GetUInt8Field(Int32Definition.Id));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that requesting a missing field through the throwing signed 32-bit accessor
        /// throws <see cref="KeyNotFoundException"/>.
        /// </summary>
        [Test]
        public void GetInt32Field_WhenFieldIsMissing_ThrowsKeyNotFoundException()
        {
            MapWorkspace workspace = CreateWorkspace(
                MapFieldRegistry.Empty,
                Allocator.Persistent);

            try
            {
                Assert.Throws<KeyNotFoundException>(() => workspace.GetInt32Field(Int32Definition.Id));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that requesting a missing field through the throwing unsigned 8-bit accessor
        /// throws <see cref="KeyNotFoundException"/>.
        /// </summary>
        [Test]
        public void GetUInt8Field_WhenFieldIsMissing_ThrowsKeyNotFoundException()
        {
            MapWorkspace workspace = CreateWorkspace(
                MapFieldRegistry.Empty,
                Allocator.Persistent);

            try
            {
                Assert.Throws<KeyNotFoundException>(() => workspace.GetUInt8Field(UInt8Definition.Id));
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that non-throwing signed 32-bit lookup succeeds for a matching field.
        /// </summary>
        [Test]
        public void TryGetInt32Field_WhenFieldIsInt32_ReturnsTrue()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                bool result = workspace.TryGetInt32Field(
                    Int32Definition.Id,
                    out MapField<int> field);

                Assert.IsTrue(result);
                Assert.IsTrue(field.IsCreated);
                Assert.AreEqual(Int32Definition.Id, field.Id);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that non-throwing unsigned 8-bit lookup succeeds for a matching field.
        /// </summary>
        [Test]
        public void TryGetUInt8Field_WhenFieldIsUInt8_ReturnsTrue()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(UInt8Definition),
                Allocator.Persistent);

            try
            {
                bool result = workspace.TryGetUInt8Field(
                    UInt8Definition.Id,
                    out MapField<byte> field);

                Assert.IsTrue(result);
                Assert.IsTrue(field.IsCreated);
                Assert.AreEqual(UInt8Definition.Id, field.Id);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that non-throwing signed 32-bit lookup returns false for invalid identifiers,
        /// missing fields, and mismatched value types.
        /// </summary>
        [Test]
        public void TryGetInt32Field_WhenFieldCannotBeReturned_ReturnsFalse()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(UInt8Definition),
                Allocator.Persistent);

            try
            {
                Assert.IsFalse(workspace.TryGetInt32Field(
                    MapFieldId.Invalid,
                    out MapField<int> invalidField));

                Assert.IsFalse(invalidField.IsCreated);

                Assert.IsFalse(workspace.TryGetInt32Field(
                    Int32Definition.Id,
                    out MapField<int> missingField));

                Assert.IsFalse(missingField.IsCreated);

                Assert.IsFalse(workspace.TryGetInt32Field(
                    UInt8Definition.Id,
                    out MapField<int> wrongTypeField));

                Assert.IsFalse(wrongTypeField.IsCreated);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that non-throwing unsigned 8-bit lookup returns false for invalid
        /// identifiers, missing fields, and mismatched value types.
        /// </summary>
        [Test]
        public void TryGetUInt8Field_WhenFieldCannotBeReturned_ReturnsFalse()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                Assert.IsFalse(workspace.TryGetUInt8Field(
                    MapFieldId.Invalid,
                    out MapField<byte> invalidField));

                Assert.IsFalse(invalidField.IsCreated);

                Assert.IsFalse(workspace.TryGetUInt8Field(
                    UInt8Definition.Id,
                    out MapField<byte> missingField));

                Assert.IsFalse(missingField.IsCreated);

                Assert.IsFalse(workspace.TryGetUInt8Field(
                    Int32Definition.Id,
                    out MapField<byte> wrongTypeField));

                Assert.IsFalse(wrongTypeField.IsCreated);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that writing through a field view updates workspace-owned storage.
        /// </summary>
        [Test]
        public void FieldView_WhenWritten_UpdatesWorkspaceStorage()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            try
            {
                MapField<int> field = workspace.GetInt32Field(Int32Definition.Id);

                field[0] = 17;
                field[workspace.Length - 1] = 42;

                NativeArray<int> values = field.AsNativeArray();

                Assert.AreEqual(17, values[0]);
                Assert.AreEqual(42, values[workspace.Length - 1]);
            }
            finally
            {
                workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that disposing a workspace marks it as disposed.
        /// </summary>
        [Test]
        public void Dispose_WhenCalled_MarksWorkspaceDisposed()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            workspace.Dispose();

            Assert.IsTrue(workspace.IsDisposed);
        }

        /// <summary>
        /// Verifies that disposing a workspace more than once is safe.
        /// </summary>
        [Test]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            workspace.Dispose();

            Assert.DoesNotThrow(workspace.Dispose);
            Assert.IsTrue(workspace.IsDisposed);
        }

        /// <summary>
        /// Verifies that field lookup after workspace disposal is rejected.
        /// </summary>
        [Test]
        public void GetInt32Field_WhenWorkspaceIsDisposed_ThrowsObjectDisposedException()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            workspace.Dispose();

            Assert.Throws<ObjectDisposedException>(() => workspace.GetInt32Field(Int32Definition.Id));
        }

        /// <summary>
        /// Verifies that non-throwing field lookup after workspace disposal is rejected.
        /// </summary>
        [Test]
        public void TryGetInt32Field_WhenWorkspaceIsDisposed_ThrowsObjectDisposedException()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            workspace.Dispose();

            Assert.Throws<ObjectDisposedException>(() => workspace.TryGetInt32Field(
                Int32Definition.Id,
                out _));
        }

        /// <summary>
        /// Verifies that containment checks after workspace disposal are rejected.
        /// </summary>
        [Test]
        public void Contains_WhenWorkspaceIsDisposed_ThrowsObjectDisposedException()
        {
            MapWorkspace workspace = CreateWorkspace(
                CreateRegistry(Int32Definition),
                Allocator.Persistent);

            workspace.Dispose();

            Assert.Throws<ObjectDisposedException>(() => workspace.Contains(Int32Definition.Id));
        }

        /// <summary>
        /// Creates standard dimensions for workspace tests.
        /// </summary>
        /// <returns>Valid map dimensions.</returns>
        private static MapDimensions CreateDimensions()
        {
            return new MapDimensions(
                width: 4,
                height: 3);
        }

        /// <summary>
        /// Creates a field registry containing the supplied definitions.
        /// </summary>
        /// <param name="definitions">Definitions to include.</param>
        /// <returns>A validated field registry.</returns>
        private static MapFieldRegistry CreateRegistry(
            params MapFieldDefinition[] definitions)
        {
            return new MapFieldRegistry(definitions);
        }

        /// <summary>
        /// Creates a workspace using standard test dimensions.
        /// </summary>
        /// <param name="registry">Field registry used to allocate storage.</param>
        /// <param name="allocator">Allocator used for native storage.</param>
        /// <returns>A newly allocated workspace.</returns>
        private static MapWorkspace CreateWorkspace(
            MapFieldRegistry registry,
            Allocator allocator)
        {
            return new MapWorkspace(
                CreateDimensions(),
                registry,
                allocator);
        }
    }
}