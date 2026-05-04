using System;

namespace Lokrain.Burstable.Generation
{
    /// <summary>
    /// Runtime execution settings for map generation scheduling.
    /// </summary>
    /// <remarks>
    /// This value controls how generation work is scheduled and executed, not what data is
    /// generated. Changing these settings must not change deterministic map output.
    ///
    /// Keep execution concerns separate from <see cref="MapGenerationSpec"/>. The generation spec
    /// describes the world to generate; this type describes how the work should be scheduled.
    /// </remarks>
    public readonly struct MapGenerationExecutionSettings : IEquatable<MapGenerationExecutionSettings>
    {
        /// <summary>
        /// Default inner-loop batch count for per-tile parallel jobs.
        /// </summary>
        public const int DefaultInnerLoopBatchCount = 128;

        /// <summary>
        /// Minimum supported inner-loop batch count.
        /// </summary>
        public const int MinimumInnerLoopBatchCount = 1;

        /// <summary>
        /// Creates execution settings for map generation scheduling.
        /// </summary>
        /// <param name="innerLoopBatchCount">
        /// Number of iterations assigned to each batch for parallel-for jobs.
        /// </param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// these settings to schedule generation work.
        /// </remarks>
        public MapGenerationExecutionSettings(int innerLoopBatchCount)
        {
            InnerLoopBatchCount = innerLoopBatchCount;
        }

        /// <summary>
        /// Gets the number of iterations assigned to each batch for parallel-for jobs.
        /// </summary>
        /// <remarks>
        /// This value affects scheduling overhead and work distribution. It must not affect generated
        /// field values.
        /// </remarks>
        public int InnerLoopBatchCount { get; }

        /// <summary>
        /// Gets default execution settings suitable for general generation and editor previews.
        /// </summary>
        public static MapGenerationExecutionSettings Default => new(
            DefaultInnerLoopBatchCount);

        /// <summary>
        /// Validates that these execution settings can be used for job scheduling.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="InnerLoopBatchCount"/> is less than one.
        /// </exception>
        public void Validate()
        {
            if (InnerLoopBatchCount < MinimumInnerLoopBatchCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(InnerLoopBatchCount),
                    InnerLoopBatchCount,
                    "Inner loop batch count must be positive.");
            }
        }

        /// <summary>
        /// Determines whether these execution settings are equal to another value.
        /// </summary>
        public bool Equals(MapGenerationExecutionSettings other)
        {
            return InnerLoopBatchCount == other.InnerLoopBatchCount;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapGenerationExecutionSettings other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return InnerLoopBatchCount;
        }

        /// <summary>
        /// Determines whether two execution settings values are equal.
        /// </summary>
        public static bool operator ==(
            MapGenerationExecutionSettings left,
            MapGenerationExecutionSettings right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two execution settings values are not equal.
        /// </summary>
        public static bool operator !=(
            MapGenerationExecutionSettings left,
            MapGenerationExecutionSettings right)
        {
            return !left.Equals(right);
        }
    }
}