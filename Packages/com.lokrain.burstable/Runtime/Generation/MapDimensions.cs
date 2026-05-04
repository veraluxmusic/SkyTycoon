using System;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Immutable dimensions for a rectangular tile map.
    /// </summary>
    /// <remarks>
    /// This value object owns the basic size invariants for generated maps.
    /// It keeps width, height, and tile-count validation centralized so generation
    /// specs, workspaces, stages, and tests do not duplicate dimension checks.
    ///
    /// The map uses row-major indexing. Valid tile coordinates are in the range
    /// <c>0..Width - 1</c> and <c>0..Height - 1</c>. The total tile count must fit
    /// in a signed 32-bit integer because Unity <c>NativeArray</c> lengths and job
    /// indices are <see cref="int"/> based.
    /// </remarks>
    public readonly struct MapDimensions : IEquatable<MapDimensions>
    {
        /// <summary>
        /// Default generated map width in tiles.
        /// </summary>
        public const int DefaultWidth = 256;

        /// <summary>
        /// Default generated map height in tiles.
        /// </summary>
        public const int DefaultHeight = 256;

        /// <summary>
        /// Minimum supported map width or height in tiles.
        /// </summary>
        public const int MinimumSize = 1;

        /// <summary>
        /// Creates immutable rectangular map dimensions.
        /// </summary>
        /// <param name="width">Map width in tiles.</param>
        /// <param name="height">Map height in tiles.</param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before
        /// using dimensions to allocate fields or schedule generation.
        /// </remarks>
        public MapDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the map width in tiles.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the map height in tiles.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the total number of tiles in the map.
        /// </summary>
        /// <remarks>
        /// This value is valid only after <see cref="Validate"/> has passed.
        /// </remarks>
        public int Length => Width * Height;

        /// <summary>
        /// Gets the default map dimensions used by samples, tests, and first-run previews.
        /// </summary>
        public static MapDimensions Default => new(
            DefaultWidth,
            DefaultHeight);

        /// <summary>
        /// Validates that these dimensions can be used for map generation and field allocation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when width or height is not positive, or when width multiplied by height
        /// does not fit in a signed 32-bit integer.
        /// </exception>
        public void Validate()
        {
            if (Width < MinimumSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    Width,
                    "Map width must be positive.");
            }

            if (Height < MinimumSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Height),
                    Height,
                    "Map height must be positive.");
            }

            if ((long)Width * Height > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    "Map width multiplied by height must fit in a signed 32-bit integer.");
            }
        }

        /// <summary>
        /// Determines whether these dimensions are equal to another dimensions value.
        /// </summary>
        public bool Equals(MapDimensions other)
        {
            return Width == other.Width &&
                   Height == other.Height;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapDimensions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        /// <summary>
        /// Determines whether two dimension values are equal.
        /// </summary>
        public static bool operator ==(MapDimensions left, MapDimensions right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two dimension values are not equal.
        /// </summary>
        public static bool operator !=(MapDimensions left, MapDimensions right)
        {
            return !left.Equals(right);
        }
    }
}