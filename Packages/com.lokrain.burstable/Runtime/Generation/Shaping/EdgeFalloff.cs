using System;
using System.Runtime.CompilerServices;
using Lokrain.Burstable.Generation;

namespace Lokrain.Burstable.Generation.Shaping
{
    /// <summary>
    /// Deterministic edge-falloff policy for rectangular tile maps.
    /// </summary>
    /// <remarks>
    /// Edge falloff converts a tile coordinate into a fixed-point multiplier that can be
    /// applied to generated scalar fields such as elevation.
    ///
    /// This type owns reusable shaping behavior. It does not allocate memory, sample noise,
    /// classify terrain, schedule jobs, mutate generated data, or own workspace lifetime.
    ///
    /// Multipliers use <see cref="MultiplierScale"/> as one. A multiplier of
    /// <see cref="MinimumMultiplier"/> means the generated contribution is fully suppressed at
    /// that tile. A multiplier of <see cref="MaximumMultiplier"/> means the generated
    /// contribution is unchanged.
    ///
    /// The public constructor stores values as provided so callers can build values cheaply.
    /// Call <see cref="Validate"/> at managed setup boundaries before using this policy in a
    /// generation stage or job.
    /// </remarks>
    public readonly struct EdgeFalloff : IEquatable<EdgeFalloff>
    {
        /// <summary>
        /// Fixed-point multiplier value representing one.
        /// </summary>
        public const int MultiplierScale = 65536;

        /// <summary>
        /// Fixed-point multiplier value representing zero.
        /// </summary>
        public const int MinimumMultiplier = 0;

        /// <summary>
        /// Maximum fixed-point multiplier value returned by this policy.
        /// </summary>
        public const int MaximumMultiplier = MultiplierScale;

        /// <summary>
        /// Minimum supported falloff distance in tiles when edge falloff is enabled.
        /// </summary>
        public const int MinimumEnabledDistanceInTiles = 1;

        /// <summary>
        /// Creates a deterministic edge-falloff policy.
        /// </summary>
        /// <param name="mode">Edge-falloff mode to evaluate.</param>
        /// <param name="distanceInTiles">
        /// Distance from each rectangular map border over which falloff is applied.
        /// </param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// the policy for generation.
        ///
        /// Prefer <see cref="FromSettings"/> when creating this value from
        /// <see cref="MapShapeSettings"/>.
        /// </remarks>
        public EdgeFalloff(
            EdgeFalloffMode mode,
            int distanceInTiles)
        {
            Mode = mode;
            DistanceInTiles = distanceInTiles;
        }

        /// <summary>
        /// Gets the edge-falloff mode evaluated by this policy.
        /// </summary>
        public EdgeFalloffMode Mode { get; }

        /// <summary>
        /// Gets the distance from each rectangular map border over which falloff is applied.
        /// </summary>
        /// <remarks>
        /// This value must be zero when <see cref="Mode"/> is <see cref="EdgeFalloffMode.None"/>.
        /// It must be positive when falloff is enabled.
        /// </remarks>
        public int DistanceInTiles { get; }

        /// <summary>
        /// Gets a value indicating whether this policy applies edge falloff.
        /// </summary>
        public bool IsEnabled => Mode != EdgeFalloffMode.None;

        /// <summary>
        /// Gets an edge-falloff policy that never reduces generated values.
        /// </summary>
        public static EdgeFalloff None => new(
            EdgeFalloffMode.None,
            0);

        /// <summary>
        /// Creates a linear edge-falloff policy.
        /// </summary>
        /// <param name="distanceInTiles">
        /// Distance from each rectangular map border over which falloff is applied.
        /// </param>
        /// <returns>A linear edge-falloff policy.</returns>
        /// <remarks>
        /// The returned value stores the supplied distance as provided. Call
        /// <see cref="Validate"/> before using it for generation.
        /// </remarks>
        public static EdgeFalloff Linear(int distanceInTiles)
        {
            return new EdgeFalloff(
                EdgeFalloffMode.Linear,
                distanceInTiles);
        }

        /// <summary>
        /// Creates an edge-falloff policy from validated map-shaping settings.
        /// </summary>
        /// <param name="settings">Map-shaping settings.</param>
        /// <returns>An edge-falloff policy representing the supplied settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the settings contain an invalid edge-falloff distance.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the settings contain an unsupported edge-falloff mode.
        /// </exception>
        public static EdgeFalloff FromSettings(MapShapeSettings settings)
        {
            settings.Validate();

            EdgeFalloff edgeFalloff = new(
                settings.EdgeFalloffMode,
                settings.EdgeFalloffDistanceInTiles);

            edgeFalloff.Validate();
            return edgeFalloff;
        }

        /// <summary>
        /// Validates that this policy can be used for deterministic edge-falloff evaluation.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="Mode"/> is not a supported edge-falloff mode.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="DistanceInTiles"/> is invalid for the selected
        /// <see cref="Mode"/>.
        /// </exception>
        public void Validate()
        {
            if (!IsSupportedMode(Mode))
            {
                throw new ArgumentException(
                    "Unsupported edge falloff mode.",
                    nameof(Mode));
            }

            if (Mode == EdgeFalloffMode.None)
            {
                if (DistanceInTiles != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(DistanceInTiles),
                        DistanceInTiles,
                        "Edge falloff distance must be zero when edge falloff is disabled.");
                }

                return;
            }

            if (DistanceInTiles < MinimumEnabledDistanceInTiles)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistanceInTiles),
                    DistanceInTiles,
                    "Edge falloff distance must be positive when edge falloff is enabled.");
            }
        }

        /// <summary>
        /// Evaluates the fixed-point falloff multiplier for a tile coordinate.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="dimensions">Map dimensions.</param>
        /// <returns>
        /// A fixed-point multiplier in the inclusive range
        /// <see cref="MinimumMultiplier"/> to <see cref="MaximumMultiplier"/>.
        /// </returns>
        /// <remarks>
        /// This method is intended for hot generation paths. It assumes this policy,
        /// <paramref name="dimensions"/>, and the supplied tile coordinate have already been
        /// validated by the caller.
        ///
        /// Unsupported modes return <see cref="MinimumMultiplier"/> instead of throwing so the
        /// method remains safe for Burst/job paths. Managed setup code should call
        /// <see cref="Validate"/> before scheduling work.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EvaluateMultiplier(
            int tileX,
            int tileY,
            MapDimensions dimensions)
        {
            switch (Mode)
            {
                case EdgeFalloffMode.None:
                    return MaximumMultiplier;

                case EdgeFalloffMode.Linear:
                    return EvaluateLinearMultiplier(
                        tileX,
                        tileY,
                        dimensions);

                default:
                    return MinimumMultiplier;
            }
        }

        /// <summary>
        /// Determines whether this edge-falloff policy is equal to another policy.
        /// </summary>
        /// <param name="other">Other edge-falloff policy.</param>
        /// <returns>
        /// <see langword="true"/> when both policies are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(EdgeFalloff other)
        {
            return Mode == other.Mode &&
                   DistanceInTiles == other.DistanceInTiles;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is EdgeFalloff other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Mode * 397) ^ DistanceInTiles;
            }
        }

        /// <summary>
        /// Determines whether two edge-falloff policies are equal.
        /// </summary>
        /// <param name="left">Left edge-falloff policy.</param>
        /// <param name="right">Right edge-falloff policy.</param>
        /// <returns>
        /// <see langword="true"/> when both policies are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(EdgeFalloff left, EdgeFalloff right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two edge-falloff policies are not equal.
        /// </summary>
        /// <param name="left">Left edge-falloff policy.</param>
        /// <param name="right">Right edge-falloff policy.</param>
        /// <returns>
        /// <see langword="true"/> when both policies are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(EdgeFalloff left, EdgeFalloff right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Evaluates the linear fixed-point falloff multiplier for a tile coordinate.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="dimensions">Map dimensions.</param>
        /// <returns>The fixed-point linear edge-falloff multiplier.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EvaluateLinearMultiplier(
            int tileX,
            int tileY,
            MapDimensions dimensions)
        {
            if (DistanceInTiles <= 0)
            {
                return MinimumMultiplier;
            }

            int nearestEdgeDistance = GetNearestEdgeDistance(
                tileX,
                tileY,
                dimensions);

            if (nearestEdgeDistance <= 0)
            {
                return MinimumMultiplier;
            }

            if (nearestEdgeDistance >= DistanceInTiles)
            {
                return MaximumMultiplier;
            }

            return (int)((long)nearestEdgeDistance * MultiplierScale / DistanceInTiles);
        }

        /// <summary>
        /// Gets the tile distance to the nearest rectangular map edge.
        /// </summary>
        /// <param name="tileX">Tile x-coordinate.</param>
        /// <param name="tileY">Tile y-coordinate.</param>
        /// <param name="dimensions">Map dimensions.</param>
        /// <returns>The distance to the nearest rectangular map edge, in tiles.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNearestEdgeDistance(
            int tileX,
            int tileY,
            MapDimensions dimensions)
        {
            int distanceToLeft = tileX;
            int distanceToRight = dimensions.Width - 1 - tileX;
            int distanceToBottom = tileY;
            int distanceToTop = dimensions.Height - 1 - tileY;

            int horizontalDistance = Min(distanceToLeft, distanceToRight);
            int verticalDistance = Min(distanceToBottom, distanceToTop);

            return Min(horizontalDistance, verticalDistance);
        }

        /// <summary>
        /// Determines whether an edge-falloff mode is supported by this policy.
        /// </summary>
        /// <param name="mode">Edge-falloff mode to test.</param>
        /// <returns>
        /// <see langword="true"/> when the mode is supported; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupportedMode(EdgeFalloffMode mode)
        {
            return mode == EdgeFalloffMode.None ||
                   mode == EdgeFalloffMode.Linear;
        }

        /// <summary>
        /// Returns the smaller of two signed integer values.
        /// </summary>
        /// <param name="left">Left value.</param>
        /// <param name="right">Right value.</param>
        /// <returns>The smaller value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Min(
            int left,
            int right)
        {
            return left < right ? left : right;
        }
    }
}