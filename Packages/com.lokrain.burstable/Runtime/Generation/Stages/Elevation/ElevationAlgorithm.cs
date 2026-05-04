using System;
using Lokrain.Burstable.Generation.Shaping;
using Lokrain.Burstable.Math;

namespace Lokrain.Burstable.Generation.Stages.Elevation
{
    /// <summary>
    /// Internal deterministic evaluator for scalar elevation values.
    /// </summary>
    /// <remarks>
    /// The elevation evaluator converts an explicit generation seed and tile coordinate into a
    /// deterministic signed 32-bit elevation value. The value is scaled into the numeric range
    /// defined by <see cref="ElevationSettings"/> and then shaped by an
    /// <see cref="EdgeFalloff"/> policy.
    ///
    /// This type is intentionally internal because the current implementation is a primitive
    /// coordinate-stable hash evaluator. It is suitable as a deterministic first elevation
    /// stage, but it should not be treated as the package's final public terrain-generation
    /// algorithm contract.
    ///
    /// This type does not allocate field storage, own workspace lifetime, classify terrain,
    /// schedule jobs, build previews, or depend on managed engine state.
    /// </remarks>
    internal readonly struct ElevationAlgorithm : IEquatable<ElevationAlgorithm>
    {
        /// <summary>
        /// Creates a deterministic elevation evaluator.
        /// </summary>
        /// <param name="seed">Explicit deterministic seed consumed by the evaluator.</param>
        /// <param name="settings">Elevation output range settings.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="settings"/> contains an invalid elevation range.
        /// </exception>
        public ElevationAlgorithm(
            MapGenerationSeed seed,
            ElevationSettings settings)
        {
            settings.Validate();

            Seed = seed;
            Settings = settings;
        }

        /// <summary>
        /// Gets the explicit deterministic seed consumed by this evaluator.
        /// </summary>
        public MapGenerationSeed Seed { get; }

        /// <summary>
        /// Gets the elevation output range settings.
        /// </summary>
        public ElevationSettings Settings { get; }

        /// <summary>
        /// Evaluates the deterministic elevation value for a tile coordinate without validating
        /// the supplied coordinate, dimensions, or falloff policy.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="dimensions">Generated map dimensions.</param>
        /// <param name="edgeFalloff">Edge-falloff policy applied to the generated value.</param>
        /// <returns>
        /// A deterministic elevation value in the inclusive configured elevation range.
        /// </returns>
        /// <remarks>
        /// This method is intended for hot generation paths. Callers must validate dimensions,
        /// coordinates, settings, and falloff policy before invoking it.
        ///
        /// Edge falloff is applied to the generated contribution above
        /// <see cref="ElevationSettings.MinimumElevation"/>. Border suppression therefore moves
        /// values toward the configured minimum elevation rather than toward absolute zero,
        /// which keeps negative or non-zero minimum ranges well-defined.
        /// </remarks>
        public int EvaluateUnchecked(
            int tileX,
            int tileY,
            MapDimensions dimensions,
            EdgeFalloff edgeFalloff)
        {
            uint hash = DeterministicHash.HashCoordinate(
                Seed.Value,
                tileX,
                tileY);

            int elevation = ScaleHashToElevation(hash);
            int multiplier = edgeFalloff.EvaluateMultiplier(
                tileX,
                tileY,
                dimensions);

            return ApplyMultiplierToElevationContribution(
                elevation,
                multiplier);
        }

        /// <summary>
        /// Determines whether this evaluator is equal to another evaluator.
        /// </summary>
        /// <param name="other">Other elevation evaluator.</param>
        /// <returns>
        /// <see langword="true"/> when both evaluators contain the same seed and settings;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(ElevationAlgorithm other)
        {
            return Seed == other.Seed &&
                   Settings == other.Settings;
        }

        /// <summary>
        /// Determines whether this evaluator is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this evaluator.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is an
        /// <see cref="ElevationAlgorithm"/> value equal to this value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ElevationAlgorithm other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this evaluator.
        /// </summary>
        /// <returns>A hash code derived from the seed and elevation settings.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Seed.GetHashCode() * 397) ^ Settings.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether two elevation evaluators are equal.
        /// </summary>
        /// <param name="left">Left elevation evaluator.</param>
        /// <param name="right">Right elevation evaluator.</param>
        /// <returns>
        /// <see langword="true"/> when both values are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(ElevationAlgorithm left, ElevationAlgorithm right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two elevation evaluators are not equal.
        /// </summary>
        /// <param name="left">Left elevation evaluator.</param>
        /// <param name="right">Right elevation evaluator.</param>
        /// <returns>
        /// <see langword="true"/> when the values are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(ElevationAlgorithm left, ElevationAlgorithm right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Scales a deterministic hash value into the configured inclusive elevation range.
        /// </summary>
        /// <param name="hash">Hash value to scale.</param>
        /// <returns>A deterministic elevation value in the configured inclusive range.</returns>
        private int ScaleHashToElevation(uint hash)
        {
            long range = Settings.ElevationRange;
            ulong possibleValues = (ulong)range + 1UL;
            long offset = (long)((hash * possibleValues) >> 32);

            return Settings.MinimumElevation + (int)offset;
        }

        /// <summary>
        /// Applies an edge-falloff multiplier to the elevation contribution above the configured
        /// minimum elevation.
        /// </summary>
        /// <param name="elevation">Unshaped elevation value.</param>
        /// <param name="multiplier">Fixed-point edge-falloff multiplier.</param>
        /// <returns>The shaped elevation value.</returns>
        private int ApplyMultiplierToElevationContribution(
            int elevation,
            int multiplier)
        {
            long contribution = (long)elevation - Settings.MinimumElevation;
            long shapedContribution = contribution * multiplier / EdgeFalloff.MultiplierScale;

            return Settings.MinimumElevation + (int)shapedContribution;
        }
    }
}