using System;
using System.Runtime.CompilerServices;

namespace Lokrain.Burstable.Math
{
    /// <summary>
    /// Provides row-major tile coordinate and index conversion helpers.
    /// </summary>
    /// <remarks>
    /// Tile index conversion is low-level scalar infrastructure. It is suitable for generation
    /// jobs, algorithms, previews, tests, exporters, and workspace consumers that need stable
    /// conversion between two-dimensional tile coordinates and one-dimensional field indices.
    ///
    /// This type does not own domain policy. It does not know about elevation, terrain,
    /// climate, biomes, shaping, field semantics, workspaces, or generation ordering.
    ///
    /// The package uses row-major indexing. For a map width of <c>width</c>, the linear index
    /// for coordinate <c>(x, y)</c> is <c>y * width + x</c>.
    /// </remarks>
    public static class TileIndexUtility
    {
        /// <summary>
        /// Converts a tile coordinate to a row-major linear index.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="width">Map width in tiles.</param>
        /// <returns>The row-major linear tile index.</returns>
        /// <remarks>
        /// This method is intended for hot paths and assumes the coordinate and width have
        /// already been validated by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(
            int tileX,
            int tileY,
            int width)
        {
            return tileY * width + tileX;
        }

        /// <summary>
        /// Converts a row-major linear index to a tile x-coordinate.
        /// </summary>
        /// <param name="index">Row-major linear tile index.</param>
        /// <param name="width">Map width in tiles.</param>
        /// <returns>The tile x-coordinate.</returns>
        /// <remarks>
        /// This method is intended for hot paths and assumes <paramref name="index"/> and
        /// <paramref name="width"/> have already been validated by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToX(
            int index,
            int width)
        {
            return index % width;
        }

        /// <summary>
        /// Converts a row-major linear index to a tile y-coordinate.
        /// </summary>
        /// <param name="index">Row-major linear tile index.</param>
        /// <param name="width">Map width in tiles.</param>
        /// <returns>The tile y-coordinate.</returns>
        /// <remarks>
        /// This method is intended for hot paths and assumes <paramref name="index"/> and
        /// <paramref name="width"/> have already been validated by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToY(
            int index,
            int width)
        {
            return index / width;
        }

        /// <summary>
        /// Determines whether a tile coordinate lies inside a rectangular tile map.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="width">Map width in tiles.</param>
        /// <param name="height">Map height in tiles.</param>
        /// <returns>
        /// <see langword="true"/> when the coordinate lies inside the rectangle; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInside(
            int tileX,
            int tileY,
            int width,
            int height)
        {
            return width > 0 &&
                   height > 0 &&
                   (uint)tileX < (uint)width &&
                   (uint)tileY < (uint)height;
        }

        /// <summary>
        /// Determines whether a row-major linear index lies inside a field of the specified
        /// length.
        /// </summary>
        /// <param name="index">Row-major linear tile index.</param>
        /// <param name="length">Total field length.</param>
        /// <returns>
        /// <see langword="true"/> when the index lies inside the field; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInside(
            int index,
            int length)
        {
            return length > 0 &&
                   (uint)index < (uint)length;
        }

        /// <summary>
        /// Validates that a tile coordinate lies inside a rectangular tile map.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="width">Map width in tiles.</param>
        /// <param name="height">Map height in tiles.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="width"/> or <paramref name="height"/> is not positive,
        /// or when the coordinate lies outside the rectangular tile map.
        /// </exception>
        public static void ValidateCoordinate(
            int tileX,
            int tileY,
            int width,
            int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    width,
                    "Map width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "Map height must be positive.");
            }

            if (!IsInside(tileX, tileY, width, height))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tileX),
                    "Tile coordinate is outside the rectangular map bounds.");
            }
        }

        /// <summary>
        /// Validates that a row-major linear index lies inside a field of the specified length.
        /// </summary>
        /// <param name="index">Row-major linear tile index.</param>
        /// <param name="length">Total field length.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="length"/> is negative, or when
        /// <paramref name="index"/> lies outside the field range.
        /// </exception>
        public static void ValidateIndex(
            int index,
            int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    length,
                    "Field length must not be negative.");
            }

            if ((uint)index >= (uint)length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Tile index is outside the field range.");
            }
        }
    }
}