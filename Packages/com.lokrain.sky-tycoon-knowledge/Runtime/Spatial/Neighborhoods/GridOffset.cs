#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Neighborhoods
{
    /// <summary>
    /// Immutable integer offset between two grid coordinates.
    /// </summary>
    public readonly struct GridOffset : IEquatable<GridOffset>
    {
        /// <summary>
        /// Horizontal coordinate delta.
        /// </summary>
        public readonly int DeltaX;

        /// <summary>
        /// Vertical coordinate delta.
        /// </summary>
        public readonly int DeltaY;

        /// <summary>
        /// Creates a grid offset.
        /// </summary>
        public GridOffset(int deltaX, int deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        /// <summary>
        /// Returns true when this offset is exactly zero.
        /// </summary>
        public bool IsZero => DeltaX == 0 && DeltaY == 0;

        /// <summary>
        /// Returns true when this offset represents a cardinal 4-neighbour step.
        /// </summary>
        public bool IsCardinal => Abs(DeltaX) + Abs(DeltaY) == 1;

        /// <summary>
        /// Returns true when this offset represents a diagonal 8-neighbour step.
        /// </summary>
        public bool IsDiagonal => Abs(DeltaX) == 1 && Abs(DeltaY) == 1;

        /// <summary>
        /// Creates an offset from a deterministic grid direction.
        /// </summary>
        public static GridOffset FromDirection(GridDirection2D direction)
        {
            switch (direction)
            {
                case GridDirection2D.West:
                    return new GridOffset(-1, 0);

                case GridDirection2D.East:
                    return new GridOffset(1, 0);

                case GridDirection2D.South:
                    return new GridOffset(0, -1);

                case GridDirection2D.North:
                    return new GridOffset(0, 1);

                case GridDirection2D.SouthWest:
                    return new GridOffset(-1, -1);

                case GridDirection2D.SouthEast:
                    return new GridOffset(1, -1);

                case GridDirection2D.NorthWest:
                    return new GridOffset(-1, 1);

                case GridDirection2D.NorthEast:
                    return new GridOffset(1, 1);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unsupported grid direction.");
            }
        }

        /// <inheritdoc />
        public bool Equals(GridOffset other)
        {
            return DeltaX == other.DeltaX && DeltaY == other.DeltaY;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GridOffset other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + DeltaX;
                hash = hash * 31 + DeltaY;
                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "("
                + DeltaX.ToString(CultureInfo.InvariantCulture)
                + ", "
                + DeltaY.ToString(CultureInfo.InvariantCulture)
                + ")";
        }

        public static bool operator ==(GridOffset left, GridOffset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridOffset left, GridOffset right)
        {
            return !left.Equals(right);
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }
    }
}