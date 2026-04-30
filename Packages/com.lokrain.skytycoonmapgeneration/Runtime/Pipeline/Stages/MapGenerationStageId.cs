#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages
{
    /// <summary>
    /// Stable stage identifier. Keep this compact and value-based; stage names belong in descriptors/logs.
    /// </summary>
    [Serializable]
    public readonly struct MapGenerationStageId : IEquatable<MapGenerationStageId>
    {
        public static readonly MapGenerationStageId None = new(0u);

        public readonly uint Value;

        public MapGenerationStageId(uint value)
        {
            Value = value;
        }

        public bool IsNone => Value == 0u;

        public static MapGenerationStageId FromStableName(string stableName)
        {
            return new MapGenerationStageId(StableHash128.StableNameToUInt32(stableName));
        }

        public bool Equals(MapGenerationStageId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is MapGenerationStageId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return "Stage:" + Value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(MapGenerationStageId left, MapGenerationStageId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapGenerationStageId left, MapGenerationStageId right)
        {
            return !left.Equals(right);
        }
    }
}
