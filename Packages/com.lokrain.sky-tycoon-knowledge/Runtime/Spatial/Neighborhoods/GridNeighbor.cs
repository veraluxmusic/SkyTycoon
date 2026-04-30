#nullable enable

using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Neighborhoods
{
    /// <summary>
    /// A valid neighbour cell produced by grid-neighbour traversal.
    /// </summary>
    public readonly struct GridNeighbor
    {
        /// <summary>
        /// Row-major index of the neighbour.
        /// </summary>
        public readonly GridIndex Index;

        /// <summary>
        /// Coordinate of the neighbour.
        /// </summary>
        public readonly GridCoordinate Coordinate;

        /// <summary>
        /// Offset from the traversal origin to the neighbour.
        /// </summary>
        public readonly GridOffset Offset;

        /// <summary>
        /// Creates a neighbour value.
        /// </summary>
        public GridNeighbor(GridIndex index, GridCoordinate coordinate, GridOffset offset)
        {
            Index = index;
            Coordinate = coordinate;
            Offset = offset;
        }
    }
}