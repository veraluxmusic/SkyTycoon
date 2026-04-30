#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    internal static class OpenSimplex2SFbmConstants
    {
        public const int AlgorithmVersionMajor = 0;
        public const int AlgorithmVersionMinor = 0;
        public const int AlgorithmVersionPatch = 1;

        public const string StepId = "F001.OpenSimplex2SFbm.HeightField";
        public const int GradientTableSize2D = 128;
        public const long OctaveSeedStep = unchecked((long)0x9E3779B97F4A7C15UL);
    }
}
