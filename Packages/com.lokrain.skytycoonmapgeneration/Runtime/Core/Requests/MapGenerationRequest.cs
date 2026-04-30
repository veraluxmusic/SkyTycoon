#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Requests
{
    /// <summary>
    /// Pure runtime request. This is the boundary between authoring/compilation and generation.
    /// It must stay free of UnityEngine objects and ScriptableObject references.
    /// </summary>
    [Serializable]
    public readonly struct MapGenerationRequest : IEquatable<MapGenerationRequest>
    {
        public const int MinPlayerCount = 1;
        public const int MaxPlayerCount = 8;

        public readonly HeightFieldDimensions Dimensions;
        public readonly DeterministicSeed Seed;
        public readonly MapArchetype Archetype;
        public readonly MapGenerationMode Mode;
        public readonly int PlayerCount;

        public MapGenerationRequest(
            HeightFieldDimensions dimensions,
            DeterministicSeed seed,
            MapArchetype archetype,
            MapGenerationMode mode,
            int playerCount)
        {
            Dimensions = dimensions;
            Seed = seed;
            Archetype = archetype;
            Mode = mode;
            PlayerCount = playerCount;
        }

        public static MapGenerationRequest CreateDefaultPreview(int width, int height, uint seed)
        {
            return new MapGenerationRequest(
                new HeightFieldDimensions(width, height),
                new DeterministicSeed(seed),
                MapArchetype.SingleContinentEightRegionNeutralCore,
                MapGenerationMode.Preview,
                MaxPlayerCount);
        }

        public void Validate()
        {
            Dimensions.Validate();

            if (Seed.Value == 0u)
                throw new ArgumentOutOfRangeException(nameof(Seed), Seed.Value, "Map generation seed must not be zero.");

            if (Archetype == MapArchetype.Unknown)
                throw new ArgumentOutOfRangeException(nameof(Archetype), Archetype, "Map archetype must be explicit.");

            if (PlayerCount < MinPlayerCount || PlayerCount > MaxPlayerCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PlayerCount),
                    PlayerCount,
                    "Player count must be between 1 and 8 for the v0.0.1 generation contract.");
            }
        }

        public bool Equals(MapGenerationRequest other)
        {
            return Dimensions == other.Dimensions
                && Seed == other.Seed
                && Archetype == other.Archetype
                && Mode == other.Mode
                && PlayerCount == other.PlayerCount;
        }

        public override bool Equals(object? obj)
        {
            return obj is MapGenerationRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Dimensions.GetHashCode();
                hash = hash * 31 + Seed.GetHashCode();
                hash = hash * 31 + Archetype.GetHashCode();
                hash = hash * 31 + Mode.GetHashCode();
                hash = hash * 31 + PlayerCount;
                return hash;
            }
        }

        public static bool operator ==(MapGenerationRequest left, MapGenerationRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapGenerationRequest left, MapGenerationRequest right)
        {
            return !left.Equals(right);
        }
    }
}
