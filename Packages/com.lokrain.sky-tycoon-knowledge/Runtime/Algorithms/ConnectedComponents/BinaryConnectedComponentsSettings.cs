using Lokrain.SkyTycoon.Knowledge.Core;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;

namespace Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents
{
    /// <summary>
    /// Immutable configuration for binary connected-component labeling.
    /// </summary>
    public readonly struct BinaryConnectedComponentsSettings
    {
        public BinaryConnectedComponentsSettings(GridConnectivity connectivity, byte foregroundValue)
        {
            Connectivity = connectivity;
            ForegroundValue = foregroundValue;
        }

        public GridConnectivity Connectivity { get; }

        public byte ForegroundValue { get; }

        public static BinaryConnectedComponentsSettings FourConnected(byte foregroundValue = 1)
        {
            return new BinaryConnectedComponentsSettings(GridConnectivity.Four, foregroundValue);
        }

        public static BinaryConnectedComponentsSettings EightConnected(byte foregroundValue = 1)
        {
            return new BinaryConnectedComponentsSettings(GridConnectivity.Eight, foregroundValue);
        }

        public AlgorithmStatus Validate()
        {
            return Connectivity == GridConnectivity.Four || Connectivity == GridConnectivity.Eight
                ? AlgorithmStatus.Success
                : AlgorithmStatus.UnsupportedConnectivity;
        }
    }
}
