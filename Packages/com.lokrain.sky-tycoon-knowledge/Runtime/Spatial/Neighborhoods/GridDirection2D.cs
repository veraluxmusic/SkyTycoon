#nullable enable

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Neighborhoods
{
    /// <summary>
    /// Deterministic two-dimensional neighbour directions.
    ///
    /// The first four values are the canonical 4-neighbour traversal order.
    /// Eight-neighbour traversal appends diagonals after cardinals.
    ///
    /// Direction names are grid-space names only. Rendering code may choose a different
    /// visual interpretation for the y-axis.
    /// </summary>
    public enum GridDirection2D
    {
        /// <summary>
        /// Offset (-1, 0).
        /// </summary>
        West = 0,

        /// <summary>
        /// Offset (+1, 0).
        /// </summary>
        East = 1,

        /// <summary>
        /// Offset (0, -1).
        /// </summary>
        South = 2,

        /// <summary>
        /// Offset (0, +1).
        /// </summary>
        North = 3,

        /// <summary>
        /// Offset (-1, -1).
        /// </summary>
        SouthWest = 4,

        /// <summary>
        /// Offset (+1, -1).
        /// </summary>
        SouthEast = 5,

        /// <summary>
        /// Offset (-1, +1).
        /// </summary>
        NorthWest = 6,

        /// <summary>
        /// Offset (+1, +1).
        /// </summary>
        NorthEast = 7
    }
}