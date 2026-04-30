using Lokrain.SkyTycoon.Knowledge.Core;

namespace Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents
{
    /// <summary>
    /// Deterministic metadata produced by binary connected-component labeling.
    /// </summary>
    public readonly struct BinaryConnectedComponentsResult
    {
        public BinaryConnectedComponentsResult(
            AlgorithmStatus status,
            int componentCount,
            int largestComponentLabel,
            int largestComponentArea,
            int foregroundCellCount)
        {
            Status = status;
            ComponentCount = componentCount;
            LargestComponentLabel = largestComponentLabel;
            LargestComponentArea = largestComponentArea;
            ForegroundCellCount = foregroundCellCount;
        }

        public AlgorithmStatus Status { get; }

        public int ComponentCount { get; }

        public int LargestComponentLabel { get; }

        public int LargestComponentArea { get; }

        public int ForegroundCellCount { get; }

        public static BinaryConnectedComponentsResult Failed(AlgorithmStatus status)
        {
            return new BinaryConnectedComponentsResult(status, 0, 0, 0, 0);
        }
    }
}
