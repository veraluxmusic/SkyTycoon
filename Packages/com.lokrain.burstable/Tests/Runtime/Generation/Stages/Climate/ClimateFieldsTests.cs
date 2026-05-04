using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Climate;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests the generated workspace field contract owned by <see cref="ClimateFields"/>.
    /// </summary>
    public sealed class ClimateFieldsTests
    {
        /// <summary>
        /// Verifies that the temperature field identifier exposes the documented stable raw value.
        /// </summary>
        [Test]
        public void TemperatureId_ReturnsStableIdentifier()
        {
            Assert.AreEqual(
                new MapFieldId(ClimateFields.TemperatureIdValue),
                ClimateFields.TemperatureId);
        }

        /// <summary>
        /// Verifies that the moisture field identifier exposes the documented stable raw value.
        /// </summary>
        [Test]
        public void MoistureId_ReturnsStableIdentifier()
        {
            Assert.AreEqual(
                new MapFieldId(ClimateFields.MoistureIdValue),
                ClimateFields.MoistureId);
        }

        /// <summary>
        /// Verifies that the temperature field definition uses the stable identifier, scalar value
        /// type, and symbolic name owned by the climate stage.
        /// </summary>
        [Test]
        public void Temperature_ReturnsTemperatureFieldDefinition()
        {
            MapFieldDefinition definition = ClimateFields.Temperature;

            Assert.AreEqual(ClimateFields.TemperatureId, definition.Id);
            Assert.AreEqual(MapFieldValueType.Int32, definition.ValueType);
            Assert.AreEqual(ClimateFields.TemperatureName, definition.Name);
        }

        /// <summary>
        /// Verifies that the moisture field definition uses the stable identifier, scalar value
        /// type, and symbolic name owned by the climate stage.
        /// </summary>
        [Test]
        public void Moisture_ReturnsMoistureFieldDefinition()
        {
            MapFieldDefinition definition = ClimateFields.Moisture;

            Assert.AreEqual(ClimateFields.MoistureId, definition.Id);
            Assert.AreEqual(MapFieldValueType.Int32, definition.ValueType);
            Assert.AreEqual(ClimateFields.MoistureName, definition.Name);
        }

        /// <summary>
        /// Verifies that the temperature and moisture fields do not reuse the same identifier.
        /// </summary>
        [Test]
        public void TemperatureIdAndMoistureId_AreDifferent()
        {
            Assert.AreNotEqual(ClimateFields.TemperatureId, ClimateFields.MoistureId);
        }

        /// <summary>
        /// Verifies that the temperature and moisture fields do not reuse the same symbolic name.
        /// </summary>
        [Test]
        public void TemperatureNameAndMoistureName_AreDifferent()
        {
            Assert.AreNotEqual(ClimateFields.TemperatureName, ClimateFields.MoistureName);
        }
    }
}