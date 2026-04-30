#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Grids
{
    /// <summary>
    /// Immutable dimensions for a rectangular two-dimensional grid.
    ///
    /// Coordinates are converted to row-major linear indices:
    ///
    /// <code>
    /// index = y * Width + x
    /// </code>
    ///
    /// This type is intentionally generic. It does not know about height fields,
    /// masks, regions, biomes, textures, tiles, world-space size, or rendering.
    /// </summary>
    public readonly struct GridDimensions : IEquatable<GridDimensions>
    {
        /// <summary>
        /// Minimum supported grid width.
        /// </summary>
        public const int MinWidth = 1;

        /// <summary>
        /// Minimum supported grid height.
        /// </summary>
        public const int MinHeight = 1;

        /// <summary>
        /// Conservative per-axis guardrail.
        ///
        /// This is not a design recommendation. It exists to catch invalid dimensions
        /// before allocation, indexing, or algorithm execution.
        /// </summary>
        public const int MaxWidth = 65536;

        /// <summary>
        /// Conservative per-axis guardrail.
        ///
        /// This is not a design recommendation. It exists to catch invalid dimensions
        /// before allocation, indexing, or algorithm execution.
        /// </summary>
        public const int MaxHeight = 65536;

        /// <summary>
        /// Grid width in cells.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Grid height in cells.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Creates grid dimensions.
        ///
        /// Construction does not throw. Call <see cref="Validate"/> when rejecting
        /// invalid dimensions is required. This keeps the type cheap to pass around
        /// and compatible with serialized/default-created values.
        /// </summary>
        public GridDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Total number of cells.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when dimensions are invalid or when the cell count exceeds
        /// <see cref="int.MaxValue"/>.
        /// </exception>
        public int CellCount
        {
            get
            {
                Validate();

                checked
                {
                    return Width * Height;
                }
            }
        }

        /// <summary>
        /// Last valid x-coordinate.
        /// </summary>
        public int LastX => Width - 1;

        /// <summary>
        /// Last valid y-coordinate.
        /// </summary>
        public int LastY => Height - 1;

        /// <summary>
        /// Returns true when the dimensions are within supported bounds and the total
        /// cell count fits in a signed 32-bit integer.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (Width < MinWidth || Height < MinHeight)
                    return false;

                if (Width > MaxWidth || Height > MaxHeight)
                    return false;

                long cellCount = (long)Width * Height;
                return cellCount <= int.MaxValue;
            }
        }

        /// <summary>
        /// Creates square grid dimensions.
        /// </summary>
        public static GridDimensions Square(int size)
        {
            return new GridDimensions(size, size);
        }

        /// <summary>
        /// Throws when dimensions are outside the supported range.
        /// </summary>
        public void Validate()
        {
            if (Width < MinWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    Width,
                    "Grid width must be greater than or equal to 1.");
            }

            if (Height < MinHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Height),
                    Height,
                    "Grid height must be greater than or equal to 1.");
            }

            if (Width > MaxWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    Width,
                    "Grid width exceeds the supported maximum.");
            }

            if (Height > MaxHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Height),
                    Height,
                    "Grid height exceeds the supported maximum.");
            }

            long cellCount = (long)Width * Height;

            if (cellCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CellCount),
                    cellCount,
                    "Grid cell count exceeds Int32.MaxValue.");
            }
        }

        /// <summary>
        /// Returns true when the coordinate is inside the grid.
        /// </summary>
        public bool Contains(int x, int y)
        {
            return (uint)x < (uint)Width && (uint)y < (uint)Height;
        }

        /// <summary>
        /// Returns true when the coordinate is inside the grid.
        /// </summary>
        public bool Contains(GridCoordinate coordinate)
        {
            return Contains(coordinate.X, coordinate.Y);
        }

        /// <summary>
        /// Returns true when the linear index is inside the grid.
        /// </summary>
        public bool ContainsLinearIndex(int linearIndex)
        {
            if (!IsValid)
                return false;

            long cellCount = (long)Width * Height;
            return linearIndex >= 0 && linearIndex < cellCount;
        }

        /// <summary>
        /// Returns true when the linear index is inside the grid.
        /// </summary>
        public bool ContainsIndex(GridIndex index)
        {
            return ContainsLinearIndex(index.Value);
        }

        /// <summary>
        /// Converts a coordinate to a row-major linear index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when dimensions are invalid or the coordinate is outside the grid.
        /// </exception>
        public int ToLinearIndex(int x, int y)
        {
            Validate();

            if (!Contains(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    "Coordinates are outside the grid dimensions.");
            }

            return ToLinearIndexUnchecked(x, y);
        }

        /// <summary>
        /// Converts a coordinate to a row-major linear index.
        /// </summary>
        public int ToLinearIndex(GridCoordinate coordinate)
        {
            return ToLinearIndex(coordinate.X, coordinate.Y);
        }

        /// <summary>
        /// Converts a coordinate to a row-major grid index.
        /// </summary>
        public GridIndex ToIndex(int x, int y)
        {
            return new GridIndex(ToLinearIndex(x, y));
        }

        /// <summary>
        /// Converts a coordinate to a row-major grid index.
        /// </summary>
        public GridIndex ToIndex(GridCoordinate coordinate)
        {
            return ToIndex(coordinate.X, coordinate.Y);
        }

        /// <summary>
        /// Converts a coordinate to a row-major linear index without validation.
        ///
        /// Use only after validating dimensions and coordinate bounds externally.
        /// </summary>
        public int ToLinearIndexUnchecked(int x, int y)
        {
            return y * Width + x;
        }

        /// <summary>
        /// Attempts to convert a coordinate to a row-major grid index.
        /// </summary>
        public bool TryToIndex(int x, int y, out GridIndex index)
        {
            if (!IsValid || !Contains(x, y))
            {
                index = default;
                return false;
            }

            index = new GridIndex(ToLinearIndexUnchecked(x, y));
            return true;
        }

        /// <summary>
        /// Attempts to convert a coordinate to a row-major grid index.
        /// </summary>
        public bool TryToIndex(GridCoordinate coordinate, out GridIndex index)
        {
            return TryToIndex(coordinate.X, coordinate.Y, out index);
        }

        /// <summary>
        /// Converts a row-major linear index to a grid coordinate.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when dimensions are invalid or the index is outside the grid.
        /// </exception>
        public GridCoordinate ToCoordinate(int linearIndex)
        {
            ValidateLinearIndex(linearIndex);
            return ToCoordinateUnchecked(linearIndex);
        }

        /// <summary>
        /// Converts a row-major grid index to a grid coordinate.
        /// </summary>
        public GridCoordinate ToCoordinate(GridIndex index)
        {
            return ToCoordinate(index.Value);
        }

        /// <summary>
        /// Converts a row-major linear index to a grid coordinate without validation.
        ///
        /// Use only after validating dimensions and index bounds externally.
        /// </summary>
        public GridCoordinate ToCoordinateUnchecked(int linearIndex)
        {
            int y = linearIndex / Width;
            int x = linearIndex - y * Width;

            return new GridCoordinate(x, y);
        }

        /// <summary>
        /// Converts a row-major grid index to a grid coordinate without validation.
        ///
        /// Use only after validating dimensions and index bounds externally.
        /// </summary>
        public GridCoordinate ToCoordinateUnchecked(GridIndex index)
        {
            return ToCoordinateUnchecked(index.Value);
        }

        /// <summary>
        /// Attempts to convert a row-major linear index to a grid coordinate.
        /// </summary>
        public bool TryToCoordinate(int linearIndex, out GridCoordinate coordinate)
        {
            if (!ContainsLinearIndex(linearIndex))
            {
                coordinate = default;
                return false;
            }

            coordinate = ToCoordinateUnchecked(linearIndex);
            return true;
        }

        /// <summary>
        /// Attempts to convert a row-major grid index to a grid coordinate.
        /// </summary>
        public bool TryToCoordinate(GridIndex index, out GridCoordinate coordinate)
        {
            return TryToCoordinate(index.Value, out coordinate);
        }

        /// <summary>
        /// Returns true when the coordinate is on the outer border of the grid.
        /// </summary>
        public bool IsBorder(int x, int y)
        {
            if (!Contains(x, y))
                return false;

            return x == 0 || y == 0 || x == LastX || y == LastY;
        }

        /// <summary>
        /// Returns true when the coordinate is on the outer border of the grid.
        /// </summary>
        public bool IsBorder(GridCoordinate coordinate)
        {
            return IsBorder(coordinate.X, coordinate.Y);
        }

        /// <summary>
        /// Returns true when the index is on the outer border of the grid.
        /// </summary>
        public bool IsBorder(GridIndex index)
        {
            GridCoordinate coordinate = ToCoordinate(index);
            return IsBorder(coordinate);
        }

        /// <summary>
        /// Throws when the row-major linear index is outside the grid.
        /// </summary>
        public void ValidateLinearIndex(int linearIndex)
        {
            Validate();

            long cellCount = (long)Width * Height;

            if (linearIndex < 0 || linearIndex >= cellCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(linearIndex),
                    linearIndex,
                    "Linear index is outside the grid dimensions.");
            }
        }

        /// <summary>
        /// Throws when the row-major grid index is outside the grid.
        /// </summary>
        public void ValidateIndex(GridIndex index)
        {
            ValidateLinearIndex(index.Value);
        }

        /// <inheritdoc />
        public bool Equals(GridDimensions other)
        {
            return Width == other.Width && Height == other.Height;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is GridDimensions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Width;
                hash = hash * 31 + Height;
                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Width.ToString(CultureInfo.InvariantCulture)
                + "x"
                + Height.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(GridDimensions left, GridDimensions right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridDimensions left, GridDimensions right)
        {
            return !left.Equals(right);
        }
    }
}