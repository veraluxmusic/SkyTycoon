using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Elevation
{
    /// <summary>
    /// Tests the public workspace field contract owned by <see cref="ElevationFields"/>.
    /// </summary>
    /// <remarks>
    /// These tests intentionally validate stable field metadata only. Field allocation,
    /// workspace lifetime, generation execution, and native storage behavior belong to
    /// workspace and stage execution tests.
    ///
    /// The elevation field identifier, symbolic name, and value type are package data-contract
    /// values. Changing them can break saved previews, downstream stage dependencies,
    /// generated-map consumers, exporters, editor tooling, and external package consumers.
    /// </remarks>
    public sealed class ElevationFieldsTests
    {
        /// <summary>
        /// Verifies that the primary elevation field keeps its stable raw identifier value.
        /// </summary>
        [Test]
        public void ElevationIdValue_ReturnsStablePrimaryElevationIdentifier()
        {
            Assert.AreEqual(1, ElevationFields.ElevationIdValue);
        }

        /// <summary>
        /// Verifies that the primary elevation field keeps its stable symbolic name.
        /// </summary>
        [Test]
        public void ElevationName_ReturnsStablePrimaryElevationName()
        {
            Assert.AreEqual("elevation", ElevationFields.ElevationName);
        }

        /// <summary>
        /// Verifies that the primary elevation field identifier is constructed from the stable
        /// raw identifier value.
        /// </summary>
        [Test]
        public void ElevationId_ReturnsIdentifierFromStableValue()
        {
            MapFieldId expected = new MapFieldId(ElevationFields.ElevationIdValue);

            Assert.AreEqual(expected, ElevationFields.ElevationId);
        }

        /// <summary>
        /// Verifies that the primary elevation field definition uses the stable identifier,
        /// signed 32-bit scalar value type, and stable symbolic name.
        /// </summary>
        [Test]
        public void Elevation_ReturnsStableInt32FieldDefinition()
        {
            MapFieldDefinition expected = new MapFieldDefinition(
                ElevationFields.ElevationId,
                MapFieldValueType.Int32,
                ElevationFields.ElevationName);

            Assert.AreEqual(expected, ElevationFields.Elevation);
        }
    }
}