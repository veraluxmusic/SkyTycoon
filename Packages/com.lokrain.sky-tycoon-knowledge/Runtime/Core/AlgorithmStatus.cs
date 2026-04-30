namespace Lokrain.SkyTycoon.Knowledge.Core
{
    /// <summary>
    /// Deterministic status values returned by low-level algorithm facades.
    /// </summary>
    public enum AlgorithmStatus : byte
    {
        Success = 0,
        InvalidDimensions = 1,
        InputTooSmall = 2,
        OutputTooSmall = 3,
        WorkspaceTooSmall = 4,
        UnsupportedConnectivity = 5,
        ComponentCapacityExceeded = 6
    }
}
