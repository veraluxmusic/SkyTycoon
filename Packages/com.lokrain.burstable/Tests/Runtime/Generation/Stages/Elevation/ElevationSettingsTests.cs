using System;
using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Elevation;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Elevation
{
    /// <summary>
    /// Tests the public configuration contract owned by <see cref="ElevationSettings"/>.
    /// </summary>
    /// <remarks>
    /// These tests intentionally validate settings invariants without scheduling generation
    /// jobs or allocating workspace fields. Elevation settings own the numeric output contract
    /// only; stage execution, workspace ownership, field registration, and job behavior belong
    /// to separate test fixtures.
    /// </remarks>
    public sealed class ElevationSettingsTests
    {
        /// <summary>
        /// Verifies that the default settings expose the expected inclusive 16-bit scalar
        /// elevation range.
        /// </summary>
        [Test]
        public void Default_ReturnsInclusiveSixteenBitScalarRange()
        {
            ElevationSettings settings = ElevationSettings.Default;

            Assert.AreEqual(0, settings.MinimumElevation);
            Assert.AreEqual(65535, settings.MaximumElevation);
            Assert.AreEqual(65535L, settings.ElevationRange);
            Assert.AreEqual(65536L, settings.ElevationRange + 1L);
        }

        /// <summary>
        /// Verifies that default settings are valid for generation.
        /// </summary>
        [Test]
        public void Validate_WhenDefaultSettings_DoesNotThrow()
        {
            ElevationSettings settings = ElevationSettings.Default;

            Assert.DoesNotThrow(settings.Validate);
        }

        /// <summary>
        /// Verifies that the minimum supported elevation range is valid.
        /// </summary>
        [Test]
        public void Validate_WhenRangeIsMinimumSupportedRange_DoesNotThrow()
        {
            ElevationSettings settings = new(
                minimumElevation: 10,
                maximumElevation: 11);

            Assert.AreEqual(ElevationSettings.MinimumElevationRange, settings.ElevationRange);
            Assert.DoesNotThrow(settings.Validate);
        }

        /// <summary>
        /// Verifies that equal minimum and maximum elevation values are rejected.
        /// </summary>
        [Test]
        public void Validate_WhenMaximumEqualsMinimum_ThrowsArgumentOutOfRangeException()
        {
            ElevationSettings settings = new(
                minimumElevation: 10,
                maximumElevation: 10);

            Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        }

        /// <summary>
        /// Verifies that a maximum elevation below the minimum elevation is rejected.
        /// </summary>
        [Test]
        public void Validate_WhenMaximumIsLessThanMinimum_ThrowsArgumentOutOfRangeException()
        {
            ElevationSettings settings = new(
                minimumElevation: 10,
                maximumElevation: 9);

            Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        }

        /// <summary>
        /// Verifies that ranges larger than a signed 32-bit integer are rejected.
        /// </summary>
        /// <remarks>
        /// Generation uses signed 32-bit scalar output values, but range calculation must still
        /// be safe for invalid input. This test protects the explicit validation boundary.
        /// </remarks>
        [Test]
        public void Validate_WhenRangeExceedsInt32_ThrowsArgumentOutOfRangeException()
        {
            ElevationSettings settings = new(
                minimumElevation: int.MinValue,
                maximumElevation: int.MaxValue);

            Assert.Greater(settings.ElevationRange, int.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        }

        /// <summary>
        /// Verifies that <see cref="ElevationSettings.ElevationRange"/> does not overflow for
        /// invalid extreme input values.
        /// </summary>
        [Test]
        public void ElevationRange_WhenInputWouldOverflowInt32_ReturnsInt64Difference()
        {
            ElevationSettings settings = new(
                minimumElevation: int.MaxValue,
                maximumElevation: int.MinValue);

            long expectedRange = (long)int.MinValue - int.MaxValue;

            Assert.AreEqual(expectedRange, settings.ElevationRange);
        }

        /// <summary>
        /// Verifies that settings with the same values compare equal.
        /// </summary>
        [Test]
        public void Equals_WhenValuesMatch_ReturnsTrue()
        {
            ElevationSettings left = new(
                minimumElevation: -10,
                maximumElevation: 20);

            ElevationSettings right = new(
                minimumElevation: -10,
                maximumElevation: 20);

            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left.Equals((object)right));
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        /// <summary>
        /// Verifies that settings with different minimum elevation values compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenMinimumElevationDiffers_ReturnsFalse()
        {
            ElevationSettings left = new(
                minimumElevation: -10,
                maximumElevation: 20);

            ElevationSettings right = new(
                minimumElevation: -9,
                maximumElevation: 20);

            Assert.IsFalse(left.Equals(right));
            Assert.IsFalse(left.Equals((object)right));
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        /// <summary>
        /// Verifies that settings with different maximum elevation values compare unequal.
        /// </summary>
        [Test]
        public void Equals_WhenMaximumElevationDiffers_ReturnsFalse()
        {
            ElevationSettings left = new(
                minimumElevation: -10,
                maximumElevation: 20);

            ElevationSettings right = new(
                minimumElevation: -10,
                maximumElevation: 21);

            Assert.IsFalse(left.Equals(right));
            Assert.IsFalse(left.Equals((object)right));
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        /// <summary>
        /// Verifies that settings do not compare equal to unrelated object types.
        /// </summary>
        [Test]
        public void Equals_WhenObjectHasDifferentType_ReturnsFalse()
        {
            ElevationSettings settings = ElevationSettings.Default;

            Assert.IsFalse(settings.Equals("elevation"));
        }
    }
}