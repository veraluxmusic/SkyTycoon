#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    /// <summary>
    /// Stable generated-field identifier. IDs are semantic, not storage indices.
    /// </summary>
    [Serializable]
    public readonly struct MapFieldId : IEquatable<MapFieldId>
    {
        public static readonly MapFieldId None = new(0u);

        public readonly uint Value;

        public MapFieldId(uint value)
        {
            Value = value;
        }

        public bool IsNone => Value == 0u;

        public static MapFieldId FromStableName(string stableName)
        {
            return new MapFieldId(StableHash128.StableNameToUInt32(stableName));
        }

        public bool Equals(MapFieldId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is MapFieldId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return "Field:" + Value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(MapFieldId left, MapFieldId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapFieldId left, MapFieldId right)
        {
            return !left.Equals(right);
        }
    }
}
