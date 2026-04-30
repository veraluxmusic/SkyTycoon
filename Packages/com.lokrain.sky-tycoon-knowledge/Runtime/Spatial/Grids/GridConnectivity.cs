#nullable enable

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Grids
{
    /// <summary>
    /// Defines the connectivity topology used by grid algorithms.
    /// </summary>
    public enum GridConnectivity
    {
        /// <summary>
        /// Orthogonal connectivity: west, east, south, and north.
        /// </summary>
        Four = 4,

        /// <summary>
        /// Orthogonal plus diagonal connectivity.
        /// </summary>
        Eight = 8
    }
}
