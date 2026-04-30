#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Grids
{
    /// <summary>
    /// Immutable integer coordinate in a two-dimensional grid.
    ///
    /// This type has no world-space meaning. It only represents grid-space coordinates.
    /// The owning grid decides whether <see cref="Y"/> increasing means north, south,
    /// top, bottom, up, or down in a rendered view.
    /// </summary>
    public readonly struct GridCoordinate : IEquatable<GridCoordinate>
    {
        /// <summary>
        /// Horizontal grid coordinate.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// Vertical grid coordinate.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Creates a grid coordinate.
        /// </summary>
        /// <param name="x">Horizontal coordinate.</param>
        /// <param name="y">Vertical coordinate.</param>
        public GridCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns a coordinate translated by the supplied delta.
        /// </summary>
        public GridCoordinate Translate(int deltaX, int deltaY)
        {
            return new GridCoordinate(X + deltaX, Y + deltaY);
        }

        /// <inheritdoc />
        public bool Equals(GridCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GridCoordinate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "("
                + X.ToString(CultureInfo.InvariantCulture)
                + ", "
                + Y.ToString(CultureInfo.InvariantCulture)
                + ")";
        }

        public static bool operator ==(GridCoordinate left, GridCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCoordinate left, GridCoordinate right)
        {
            return !left.Equals(right);
        }
    }
}