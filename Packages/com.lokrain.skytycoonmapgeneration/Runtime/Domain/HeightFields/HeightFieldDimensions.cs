#nullable enable

using System;
using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    /// <summary>
    /// Immutable dimensions for a rectangular height-field sample grid.
    ///
    /// Coordinates are addressed in row-major order:
    /// index = y * Width + x
    ///
    /// This type intentionally stores only integer grid dimensions. It does not know
    /// anything about world-space size, noise-space size, tile size, texture size,
    /// terrain scale, or map semantics.
    /// </summary>
    [Serializable]
    public readonly struct HeightFieldDimensions : IEquatable<HeightFieldDimensions>
    {
        public const int MinWidth = 1;
        public const int MinHeight = 1;

        /// <summary>
        /// Conservative hard limit used to catch invalid authoring values before NativeArray
        /// allocation or texture creation. This is not a design recommendation; it is a guardrail.
        /// </summary>
        public const int MaxWidth = 16384;

        /// <summary>
        /// Conservative hard limit used to catch invalid authoring values before NativeArray
        /// allocation or texture creation. This is not a design recommendation; it is a guardrail.
        /// </summary>
        public const int MaxHeight = 16384;

        public readonly int Width;
        public readonly int Height;

        public HeightFieldDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int SampleCount
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

        public bool IsValid
        {
            get
            {
                if (Width < MinWidth || Height < MinHeight)
                    return false;

                if (Width > MaxWidth || Height > MaxHeight)
                    return false;

                long sampleCount = (long)Width * Height;
                return sampleCount <= int.MaxValue;
            }
        }

        public int LastX => Width - 1;
        public int LastY => Height - 1;

        public static HeightFieldDimensions Square(int size)
        {
            return new HeightFieldDimensions(size, size);
        }

        public void Validate()
        {
            if (Width < MinWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    Width,
                    "Height-field width must be greater than or equal to 1.");
            }

            if (Height < MinHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Height),
                    Height,
                    "Height-field height must be greater than or equal to 1.");
            }

            if (Width > MaxWidth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Width),
                    Width,
                    "Height-field width exceeds the supported maximum.");
            }

            if (Height > MaxHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Height),
                    Height,
                    "Height-field height exceeds the supported maximum.");
            }

            long sampleCount = (long)Width * Height;

            if (sampleCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(SampleCount),
                    sampleCount,
                    "Height-field sample count exceeds Int32.MaxValue.");
            }
        }

        public bool Contains(int x, int y)
        {
            return (uint)x < (uint)Width && (uint)y < (uint)Height;
        }

        public bool ContainsIndex(int index)
        {
            return (uint)index < (uint)SampleCount;
        }

        public int ToIndex(int x, int y)
        {
            Validate();

            if (!Contains(x, y))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    "Coordinates are outside the height-field dimensions.");
            }

            return y * Width + x;
        }

        public bool TryToIndex(int x, int y, out int index)
        {
            if (!Contains(x, y))
            {
                index = -1;
                return false;
            }

            index = y * Width + x;
            return true;
        }

        public int GetX(int index)
        {
            ValidateIndex(index);
            return index % Width;
        }

        public int GetY(int index)
        {
            ValidateIndex(index);
            return index / Width;
        }

        public void ToCoordinates(int index, out int x, out int y)
        {
            ValidateIndex(index);

            y = index / Width;
            x = index - y * Width;
        }

        public bool IsBorder(int x, int y)
        {
            if (!Contains(x, y))
                return false;

            return x == 0 || y == 0 || x == LastX || y == LastY;
        }

        public bool IsBorderIndex(int index)
        {
            ToCoordinates(index, out int x, out int y);
            return IsBorder(x, y);
        }

        public void ValidateIndex(int index)
        {
            Validate();

            if ((uint)index >= (uint)SampleCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Index is outside the height-field dimensions.");
            }
        }

        public bool Equals(HeightFieldDimensions other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object? obj)
        {
            return obj is HeightFieldDimensions other && Equals(other);
        }

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

        public override string ToString()
        {
            return Width.ToString(CultureInfo.InvariantCulture)
                + "x"
                + Height.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(HeightFieldDimensions left, HeightFieldDimensions right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeightFieldDimensions left, HeightFieldDimensions right)
        {
            return !left.Equals(right);
        }
    }
}