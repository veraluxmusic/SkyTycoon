using System;
using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Climate;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests the deterministic scalar settings contract owned by <see cref="ClimateSettings"/>.
    /// </summary>
    public sealed class ClimateSettingsTests
    {
        /// <summary>
        /// Verifies that the default settings expose the documented neutral scalar values.
        /// </summary>
        [Test]
        public void Default_ReturnsNeutralSettings()
        {
            ClimateSettings settings = ClimateSettings.Default;

            Assert.AreEqual(ClimateSettings.DefaultBaseTemperature, settings.BaseTemperature);
            Assert.AreEqual(ClimateSettings.DefaultBaseMoisture, settings.BaseMoisture);
            Assert.AreEqual(
                ClimateSettings.DefaultElevationTemperaturePenalty,
                settings.ElevationTemperaturePenalty);
        }

        /// <summary>
        /// Verifies that the constructor stores the provided scalar values.
        /// </summary>
        [Test]
        public void Constructor_WhenValuesAreValid_StoresValues()
        {
            ClimateSettings settings = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.AreEqual(12, settings.BaseTemperature);
            Assert.AreEqual(34, settings.BaseMoisture);
            Assert.AreEqual(5, settings.ElevationTemperaturePenalty);
        }

        /// <summary>
        /// Verifies that zero elevation temperature penalty is accepted.
        /// </summary>
        [Test]
        public void Constructor_WhenElevationTemperaturePenaltyIsZero_StoresValue()
        {
            ClimateSettings settings = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 0);

            Assert.AreEqual(0, settings.ElevationTemperaturePenalty);
        }

        /// <summary>
        /// Verifies that negative elevation temperature penalty is rejected by the constructor.
        /// </summary>
        [Test]
        public void Constructor_WhenElevationTemperaturePenaltyIsNegative_ThrowsArgumentOutOfRangeException()
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new ClimateSettings(
                    baseTemperature: 12,
                    baseMoisture: 34,
                    elevationTemperaturePenalty: -1));

            Assert.AreEqual("elevationTemperaturePenalty", exception.ParamName);
        }

        /// <summary>
        /// Verifies that valid climate settings pass explicit validation.
        /// </summary>
        [Test]
        public void Validate_WhenSettingsAreValid_DoesNotThrow()
        {
            ClimateSettings settings = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.DoesNotThrow(() => settings.Validate());
        }

        /// <summary>
        /// Verifies that equal settings values compare equal.
        /// </summary>
        [Test]
        public void Equals_WhenValuesAreEqual_ReturnsTrue()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsTrue(first.Equals(second));
        }

        /// <summary>
        /// Verifies that settings values with different base temperature compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenBaseTemperatureDiffers_ReturnsFalse()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 13,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsFalse(first.Equals(second));
        }

        /// <summary>
        /// Verifies that settings values with different base moisture compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenBaseMoistureDiffers_ReturnsFalse()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 35,
                elevationTemperaturePenalty: 5);

            Assert.IsFalse(first.Equals(second));
        }

        /// <summary>
        /// Verifies that settings values with different elevation temperature penalty compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenElevationTemperaturePenaltyDiffers_ReturnsFalse()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 6);

            Assert.IsFalse(first.Equals(second));
        }

        /// <summary>
        /// Verifies that boxed equal settings values compare equal.
        /// </summary>
        [Test]
        public void EqualsObject_WhenObjectIsEqualClimateSettings_ReturnsTrue()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            object second = new ClimateSettings(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsTrue(first.Equals(second));
        }

        /// <summary>
        /// Verifies that a non-settings object compares unequal.
        /// </summary>
        [Test]
        public void EqualsObject_WhenObjectIsNotClimateSettings_ReturnsFalse()
        {
            ClimateSettings settings = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsFalse(settings.Equals("not climate settings"));
        }

        /// <summary>
        /// Verifies that the equality operator returns true for equal settings values.
        /// </summary>
        [Test]
        public void EqualityOperator_WhenValuesAreEqual_ReturnsTrue()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsTrue(first == second);
        }

        /// <summary>
        /// Verifies that the inequality operator returns false for equal settings values.
        /// </summary>
        [Test]
        public void InequalityOperator_WhenValuesAreEqual_ReturnsFalse()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.IsFalse(first != second);
        }

        /// <summary>
        /// Verifies that equal settings values produce equal hash codes.
        /// </summary>
        [Test]
        public void GetHashCode_WhenValuesAreEqual_ReturnsSameHashCode()
        {
            ClimateSettings first = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateSettings second = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }
    }
}