#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Grids
{
    /// <summary>
    /// Immutable row-major linear grid index.
    ///
    /// The index is interpreted relative to a <see cref="GridDimensions"/> instance:
    ///
    /// <code>
    /// index = y * width + x
    /// </code>
    ///
    /// This value object intentionally does not store dimensions. Validity against a
    /// concrete grid must be checked by <see cref="GridDimensions"/>.
    /// </summary>
    public readonly struct GridIndex : IEquatable<GridIndex>, IComparable<GridIndex>
    {
        /// <summary>
        /// Row-major linear index value.
        /// </summary>
        public readonly int Value;

        /// <summary>
        /// Creates a non-negative row-major grid index.
        /// </summary>
        /// <param name="value">Linear index value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="value"/> is negative.
        /// </exception>
        public GridIndex(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Grid index must be non-negative.");
            }

            Value = value;
        }

        /// <inheritdoc />
        public int CompareTo(GridIndex other)
        {
            return Value.CompareTo(other.Value);
        }

        /// <inheritdoc />
        public bool Equals(GridIndex other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GridIndex other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(GridIndex left, GridIndex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridIndex left, GridIndex right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(GridIndex left, GridIndex right)
        {
            return left.Value < right.Value;
        }

        public static bool operator >(GridIndex left, GridIndex right)
        {
            return left.Value > right.Value;
        }

        public static bool operator <=(GridIndex left, GridIndex right)
        {
            return left.Value <= right.Value;
        }

        public static bool operator >=(GridIndex left, GridIndex right)
        {
            return left.Value >= right.Value;
        }
    }
}