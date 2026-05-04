using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Climate;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests deterministic scalar calculations owned by <see cref="ClimateAlgorithm"/>.
    /// </summary>
    public sealed class ClimateAlgorithmTests
    {
        /// <summary>
        /// Verifies that temperature resolves to the configured base temperature when elevation
        /// does not contribute a penalty.
        /// </summary>
        [Test]
        public void CalculateTemperature_WhenElevationIsZero_ReturnsBaseTemperature()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int temperature = ClimateAlgorithm.CalculateTemperature(
                elevation: 0,
                settings: settings);

            Assert.AreEqual(120, temperature);
        }

        /// <summary>
        /// Verifies that negative elevation does not increase temperature.
        /// </summary>
        [Test]
        public void CalculateTemperature_WhenElevationIsNegative_ReturnsBaseTemperature()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int temperature = ClimateAlgorithm.CalculateTemperature(
                elevation: -10,
                settings: settings);

            Assert.AreEqual(120, temperature);
        }

        /// <summary>
        /// Verifies that positive elevation reduces temperature by the configured per-unit penalty.
        /// </summary>
        [Test]
        public void CalculateTemperature_WhenElevationIsPositive_SubtractsElevationPenalty()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int temperature = ClimateAlgorithm.CalculateTemperature(
                elevation: 10,
                settings: settings);

            Assert.AreEqual(70, temperature);
        }

        /// <summary>
        /// Verifies that moisture currently resolves directly to the configured base moisture.
        /// </summary>
        [Test]
        public void CalculateMoisture_ReturnsBaseMoisture()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int moisture = ClimateAlgorithm.CalculateMoisture(settings);

            Assert.AreEqual(40, moisture);
        }

        /// <summary>
        /// Verifies that zero elevation produces no temperature penalty.
        /// </summary>
        [Test]
        public void CalculateElevationTemperaturePenalty_WhenElevationIsZero_ReturnsZero()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int penalty = ClimateAlgorithm.CalculateElevationTemperaturePenalty(
                elevation: 0,
                settings: settings);

            Assert.AreEqual(0, penalty);
        }

        /// <summary>
        /// Verifies that negative elevation produces no temperature penalty.
        /// </summary>
        [Test]
        public void CalculateElevationTemperaturePenalty_WhenElevationIsNegative_ReturnsZero()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int penalty = ClimateAlgorithm.CalculateElevationTemperaturePenalty(
                elevation: -10,
                settings: settings);

            Assert.AreEqual(0, penalty);
        }

        /// <summary>
        /// Verifies that zero configured elevation penalty disables elevation temperature reduction.
        /// </summary>
        [Test]
        public void CalculateElevationTemperaturePenalty_WhenPenaltyIsZero_ReturnsZero()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 0);

            int penalty = ClimateAlgorithm.CalculateElevationTemperaturePenalty(
                elevation: 10,
                settings: settings);

            Assert.AreEqual(0, penalty);
        }

        /// <summary>
        /// Verifies that positive elevation produces the configured per-unit temperature penalty.
        /// </summary>
        [Test]
        public void CalculateElevationTemperaturePenalty_WhenElevationAndPenaltyArePositive_ReturnsProduct()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: 5);

            int penalty = ClimateAlgorithm.CalculateElevationTemperaturePenalty(
                elevation: 10,
                settings: settings);

            Assert.AreEqual(50, penalty);
        }

        /// <summary>
        /// Verifies that elevation penalty multiplication is clamped to the signed 32-bit range.
        /// </summary>
        [Test]
        public void CalculateElevationTemperaturePenalty_WhenProductExceedsInt32MaxValue_ReturnsInt32MaxValue()
        {
            ClimateSettings settings = new(
                baseTemperature: 120,
                baseMoisture: 40,
                elevationTemperaturePenalty: int.MaxValue);

            int penalty = ClimateAlgorithm.CalculateElevationTemperaturePenalty(
                elevation: 2,
                settings: settings);

            Assert.AreEqual(int.MaxValue, penalty);
        }

        /// <summary>
        /// Verifies that temperature calculation clamps underflow to the signed 32-bit minimum.
        /// </summary>
        [Test]
        public void CalculateTemperature_WhenResultIsBelowInt32MinValue_ReturnsInt32MinValue()
        {
            ClimateSettings settings = new(
                baseTemperature: int.MinValue,
                baseMoisture: 40,
                elevationTemperaturePenalty: 1);

            int temperature = ClimateAlgorithm.CalculateTemperature(
                elevation: 1,
                settings: settings);

            Assert.AreEqual(int.MinValue, temperature);
        }
    }
}