#nullable enable

using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;

namespace Lokrain.SkyTycoon.Knowledge.Spatial.Neighborhoods
{
    /// <summary>
    /// Allocation-free neighbour enumerator for rectangular grids.
    ///
    /// This is intentionally not <see cref="System.Collections.IEnumerator"/>.
    /// Interface-based enumeration would introduce unnecessary managed contracts for
    /// Burst-facing algorithm code. Use it manually:
    ///
    /// <code>
    /// GridNeighborEnumerator neighbours = GridNeighborhood.Enumerate(dimensions, origin, GridConnectivity.Four);
    ///
    /// while (neighbours.MoveNext())
    /// {
    ///     GridNeighbor neighbour = neighbours.Current;
    /// }
    /// </code>
    /// </summary>
    public struct GridNeighborEnumerator
    {
        private readonly GridDimensions _dimensions;
        private readonly GridCoordinate _origin;
        private readonly GridConnectivity _connectivity;

        private int _nextOrdinal;
        private GridNeighbor _current;

        internal GridNeighborEnumerator(
            GridDimensions dimensions,
            GridIndex origin,
            GridConnectivity connectivity,
            bool validate)
        {
            if (validate)
            {
                dimensions.Validate();
                dimensions.ValidateIndex(origin);
                GridNeighborhood.ValidateConnectivity(connectivity);
            }

            _dimensions = dimensions;
            _origin = validate
                ? dimensions.ToCoordinate(origin)
                : dimensions.ToCoordinateUnchecked(origin);

            _connectivity = connectivity;
            _nextOrdinal = 0;
            _current = default;
        }

        /// <summary>
        /// Current valid neighbour.
        /// </summary>
        public GridNeighbor Current => _current;

        /// <summary>
        /// Advances to the next in-bounds neighbour.
        /// </summary>
        /// <returns>
        /// True when <see cref="Current"/> contains a valid neighbour; otherwise false.
        /// </returns>
        public bool MoveNext()
        {
            int candidateCount = GridNeighborhood.GetPotentialNeighborCount(_connectivity);

            while (_nextOrdinal < candidateCount)
            {
                GridOffset offset = GridNeighborhood.GetOffsetUnchecked(_connectivity, _nextOrdinal);
                _nextOrdinal++;

                int x = _origin.X + offset.DeltaX;
                int y = _origin.Y + offset.DeltaY;

                if (!_dimensions.Contains(x, y))
                    continue;

                int linearIndex = _dimensions.ToLinearIndexUnchecked(x, y);

                _current = new GridNeighbor(
                    new GridIndex(linearIndex),
                    new GridCoordinate(x, y),
                    offset);

                return true;
            }

            return false;
        }
    }
}