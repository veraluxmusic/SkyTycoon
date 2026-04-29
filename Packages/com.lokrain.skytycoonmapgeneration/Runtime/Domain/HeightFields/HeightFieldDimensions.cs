using System;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields
{
    public readonly struct HeightFieldDimensions : IEquatable<HeightFieldDimensions>
    {
        public readonly int Width;
        public readonly int Height;

        public HeightFieldDimensions(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

            Width = width;
            Height = height;
        }

        public int SampleCount => checked(Width * Height);

        public bool Equals(HeightFieldDimensions other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is HeightFieldDimensions other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string ToString() => $"{Width}x{Height}";

        public static bool operator ==(HeightFieldDimensions left, HeightFieldDimensions right) => left.Equals(right);
        public static bool operator !=(HeightFieldDimensions left, HeightFieldDimensions right) => !left.Equals(right);
    }
}
