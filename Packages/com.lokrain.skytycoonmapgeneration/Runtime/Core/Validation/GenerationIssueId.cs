#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Validation
{
    [Serializable]
    public readonly struct GenerationIssueId : IEquatable<GenerationIssueId>
    {
        public static readonly GenerationIssueId None = new(0u);

        public readonly uint Value;

        public GenerationIssueId(uint value)
        {
            Value = value;
        }

        public bool IsNone => Value == 0u;

        public static GenerationIssueId FromStableName(string stableName)
        {
            return new GenerationIssueId(StableHash128.StableNameToUInt32(stableName));
        }

        public bool Equals(GenerationIssueId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is GenerationIssueId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Value);
        }

        public override string ToString()
        {
            return "Issue:" + Value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool operator ==(GenerationIssueId left, GenerationIssueId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationIssueId left, GenerationIssueId right)
        {
            return !left.Equals(right);
        }
    }
}
