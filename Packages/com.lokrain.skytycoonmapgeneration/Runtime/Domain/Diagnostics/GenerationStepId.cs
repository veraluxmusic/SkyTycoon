#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.Diagnostics
{
    /// <summary>
    /// Stable identifier for one generation stage. Use ids as external contracts for reports, tests and benchmark output.
    /// </summary>
    public readonly struct GenerationStepId : IEquatable<GenerationStepId>
    {
        public readonly string Value;

        public GenerationStepId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Step id must not be null, empty or whitespace.", nameof(value));

            Value = value;
        }

        public bool Equals(GenerationStepId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is GenerationStepId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(GenerationStepId left, GenerationStepId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationStepId left, GenerationStepId right)
        {
            return !left.Equals(right);
        }
    }
}
