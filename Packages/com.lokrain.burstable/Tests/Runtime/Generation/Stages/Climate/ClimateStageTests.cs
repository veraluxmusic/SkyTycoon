using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Climate;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests the managed climate generation stage contract owned by <see cref="ClimateStage"/>.
    /// </summary>
    public sealed class ClimateStageTests
    {
        /// <summary>
        /// Verifies that the default climate stage uses the default climate settings.
        /// </summary>
        [Test]
        public void Default_ReturnsDefaultStage()
        {
            ClimateStage stage = ClimateStage.Default;

            Assert.AreEqual(ClimateSettings.Default, stage.Settings);
        }

        /// <summary>
        /// Verifies that the constructor stores the supplied climate settings.
        /// </summary>
        [Test]
        public void Constructor_StoresSettings()
        {
            ClimateSettings settings = new(
                baseTemperature: 12,
                baseMoisture: 34,
                elevationTemperaturePenalty: 5);

            ClimateStage stage = new(settings);

            Assert.AreEqual(settings, stage.Settings);
        }

        /// <summary>
        /// Verifies that equal climate stages compare equal.
        /// </summary>
        [Test]
        public void Equals_WhenSettingsAreEqual_ReturnsTrue()
        {
            ClimateStage first = CreateStage();
            ClimateStage second = CreateStage();

            Assert.IsTrue(first.Equals(second));
        }

        /// <summary>
        /// Verifies that climate stages with different settings compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenSettingsDiffer_ReturnsFalse()
        {
            ClimateStage first = CreateStage();

            ClimateStage second = new(
                new ClimateSettings(
                    baseTemperature: 13,
                    baseMoisture: 34,
                    elevationTemperaturePenalty: 5));

            Assert.IsFalse(first.Equals(second));
        }

        /// <summary>
        /// Verifies that boxed equal climate stages compare equal.
        /// </summary>
        [Test]
        public void EqualsObject_WhenObjectIsEqualClimateStage_ReturnsTrue()
        {
            ClimateStage first = CreateStage();
            object second = CreateStage();

            Assert.IsTrue(first.Equals(second));
        }

        /// <summary>
        /// Verifies that a non-climate-stage object compares unequal.
        /// </summary>
        [Test]
        public void EqualsObject_WhenObjectIsNotClimateStage_ReturnsFalse()
        {
            ClimateStage stage = ClimateStage.Default;

            Assert.IsFalse(stage.Equals("not climate stage"));
        }

        /// <summary>
        /// Verifies that the equality operator returns true for equal climate stages.
        /// </summary>
        [Test]
        public void EqualityOperator_WhenSettingsAreEqual_ReturnsTrue()
        {
            ClimateStage first = CreateStage();
            ClimateStage second = CreateStage();

            Assert.IsTrue(first == second);
        }

        /// <summary>
        /// Verifies that the inequality operator returns false for equal climate stages.
        /// </summary>
        [Test]
        public void InequalityOperator_WhenSettingsAreEqual_ReturnsFalse()
        {
            ClimateStage first = CreateStage();
            ClimateStage second = CreateStage();

            Assert.IsFalse(first != second);
        }

        /// <summary>
        /// Verifies that equal climate stages produce equal hash codes.
        /// </summary>
        [Test]
        public void GetHashCode_WhenSettingsAreEqual_ReturnsSameHashCode()
        {
            ClimateStage first = CreateStage();
            ClimateStage second = CreateStage();

            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        private static ClimateStage CreateStage()
        {
            return new ClimateStage(
                new ClimateSettings(
                    baseTemperature: 12,
                    baseMoisture: 34,
                    elevationTemperaturePenalty: 5));
        }
    }
}