#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation
{
    /// <summary>
    /// Immutable context used while compiling authoring assets into a pure runtime generation plan.
    /// This is intentionally small: stage-specific options belong in profiles, not in orchestration state.
    /// </summary>
    [Serializable]
    public readonly struct GenerationCompileContext : IEquatable<GenerationCompileContext>
    {
        public readonly MapGenerationRequest Request;
        public readonly bool StrictValidation;

        public GenerationCompileContext(
            MapGenerationRequest request,
            bool strictValidation = true)
        {
            Request = request;
            StrictValidation = strictValidation;
        }

        public static GenerationCompileContext CreateDefaultPreview(
            int width,
            int height,
            uint seed)
        {
            return new GenerationCompileContext(
                MapGenerationRequest.CreateDefaultPreview(width, height, seed),
                strictValidation: true);
        }

        public void Validate()
        {
            Request.Validate();
        }

        public bool Equals(GenerationCompileContext other)
        {
            return Request == other.Request
                && StrictValidation == other.StrictValidation;
        }

        public override bool Equals(object? obj)
        {
            return obj is GenerationCompileContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Request.GetHashCode();
                hash = hash * 31 + StrictValidation.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(GenerationCompileContext left, GenerationCompileContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationCompileContext left, GenerationCompileContext right)
        {
            return !left.Equals(right);
        }
    }
}
