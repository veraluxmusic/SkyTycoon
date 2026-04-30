#nullable enable

using System;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Neighborhoods
{
    /// <summary>
    /// Deterministic, allocation-free grid-neighbour traversal utilities.
    ///
    /// Traversal order is stable and intentionally documented because later algorithms
    /// such as connected components, region growth, morphology, and cost-distance
    /// propagation may use traversal order as a deterministic tie-breaker.
    ///
    /// 4-neighbour order:
    ///
    /// <code>
    /// West, East, South, North
    /// </code>
    ///
    /// 8-neighbour order:
    ///
    /// <code>
    /// West, East, South, North, SouthWest, SouthEast, NorthWest, NorthEast
    /// </code>
    /// </summary>
    public static class GridNeighborhood
    {
        /// <summary>
        /// Returns the number of candidate offsets for the requested connectivity before
        /// bounds clipping.
        /// </summary>
        public static int GetPotentialNeighborCount(GridConnectivity connectivity)
        {
            switch (connectivity)
            {
                case GridConnectivity.Four:
                    return 4;

                case GridConnectivity.Eight:
                    return 8;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(connectivity),
                        connectivity,
                        "Unsupported grid connectivity.");
            }
        }

        /// <summary>
        /// Throws when the connectivity mode is unsupported.
        /// </summary>
        public static void ValidateConnectivity(GridConnectivity connectivity)
        {
            GetPotentialNeighborCount(connectivity);
        }

        /// <summary>
        /// Returns the deterministic direction for a connectivity-local neighbour ordinal.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the connectivity mode is invalid or the ordinal is outside the
        /// candidate neighbour range.
        /// </exception>
        public static GridDirection2D GetDirection(GridConnectivity connectivity, int ordinal)
        {
            int count = GetPotentialNeighborCount(connectivity);

            if ((uint)ordinal >= (uint)count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ordinal),
                    ordinal,
                    "Neighbour ordinal is outside the connectivity range.");
            }

            return GetDirectionUnchecked(ordinal);
        }

        /// <summary>
        /// Returns the deterministic offset for a connectivity-local neighbour ordinal.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the connectivity mode is invalid or the ordinal is outside the
        /// candidate neighbour range.
        /// </exception>
        public static GridOffset GetOffset(GridConnectivity connectivity, int ordinal)
        {
            GridDirection2D direction = GetDirection(connectivity, ordinal);
            return GridOffset.FromDirection(direction);
        }

        /// <summary>
        /// Attempts to get an in-bounds neighbour in the supplied direction.
        ///
        /// Invalid dimensions or invalid origin indices are treated as programming errors
        /// and throw. A direction that points outside the grid returns false.
        /// </summary>
        public static bool TryGetNeighbor(
            GridDimensions dimensions,
            GridIndex origin,
            GridDirection2D direction,
            out GridNeighbor neighbor)
        {
            dimensions.Validate();
            dimensions.ValidateIndex(origin);

            GridCoordinate originCoordinate = dimensions.ToCoordinate(origin);
            GridOffset offset = GridOffset.FromDirection(direction);

            int x = originCoordinate.X + offset.DeltaX;
            int y = originCoordinate.Y + offset.DeltaY;

            if (!dimensions.Contains(x, y))
            {
                neighbor = default;
                return false;
            }

            int linearIndex = dimensions.ToLinearIndexUnchecked(x, y);

            neighbor = new GridNeighbor(
                new GridIndex(linearIndex),
                new GridCoordinate(x, y),
                offset);

            return true;
        }

        /// <summary>
        /// Creates an allocation-free neighbour enumerator.
        ///
        /// This validates dimensions, origin, and connectivity before enumeration.
        /// </summary>
        public static GridNeighborEnumerator Enumerate(
            GridDimensions dimensions,
            GridIndex origin,
            GridConnectivity connectivity)
        {
            return new GridNeighborEnumerator(dimensions, origin, connectivity, validate: true);
        }

        /// <summary>
        /// Creates an allocation-free neighbour enumerator without validating dimensions
        /// or origin index.
        ///
        /// Use only when the caller already proved that dimensions and origin are valid.
        /// Connectivity is still validated during enumeration.
        /// </summary>
        public static GridNeighborEnumerator EnumerateUnchecked(
            GridDimensions dimensions,
            GridIndex origin,
            GridConnectivity connectivity)
        {
            return new GridNeighborEnumerator(dimensions, origin, connectivity, validate: false);
        }

        internal static GridOffset GetOffsetUnchecked(GridConnectivity connectivity, int ordinal)
        {
            GridDirection2D direction = GetDirection(connectivity, ordinal);
            return GridOffset.FromDirection(direction);
        }

        private static GridDirection2D GetDirectionUnchecked(int ordinal)
        {
            switch (ordinal)
            {
                case 0:
                    return GridDirection2D.West;

                case 1:
                    return GridDirection2D.East;

                case 2:
                    return GridDirection2D.South;

                case 3:
                    return GridDirection2D.North;

                case 4:
                    return GridDirection2D.SouthWest;

                case 5:
                    return GridDirection2D.SouthEast;

                case 6:
                    return GridDirection2D.NorthWest;

                case 7:
                    return GridDirection2D.NorthEast;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        ordinal,
                        "Neighbour ordinal is outside the supported direction range.");
            }
        }
    }
}