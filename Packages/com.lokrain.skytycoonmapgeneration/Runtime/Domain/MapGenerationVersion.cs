#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Domain
{
    /// <summary>
    /// Stable semantic version value used by generation reports.
    /// This intentionally does not use the name Version to avoid ambiguity with System.Version.
    /// </summary>
    public readonly struct MapGenerationVersion : IEquatable<MapGenerationVersion>, IComparable<MapGenerationVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        public MapGenerationVersion(int major, int minor, int patch)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), "Major version must be zero or greater.");
            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), "Minor version must be zero or greater.");
            if (patch < 0)
                throw new ArgumentOutOfRangeException(nameof(patch), "Patch version must be zero or greater.");

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int CompareTo(MapGenerationVersion other)
        {
            int major = Major.CompareTo(other.Major);
            if (major != 0)
                return major;

            int minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
                return minor;

            return Patch.CompareTo(other.Patch);
        }

        public bool Equals(MapGenerationVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object? obj)
        {
            return obj is MapGenerationVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Major, Minor, Patch);

        }

        public override string ToString()
        {
            return Major + "." + Minor + "." + Patch;
        }

        public static bool operator ==(MapGenerationVersion left, MapGenerationVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapGenerationVersion left, MapGenerationVersion right)
        {
            return !left.Equals(right);
        }
    }
}
