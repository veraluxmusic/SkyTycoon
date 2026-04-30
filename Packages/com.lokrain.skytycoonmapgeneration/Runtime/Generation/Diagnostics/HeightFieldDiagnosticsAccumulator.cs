// Runtime/Generation/Diagnostics/HeightFieldDiagnosticsAccumulator.cs
#nullable enable

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Diagnostics
{
    internal struct HeightFieldDiagnosticsAccumulator
    {
        public int SampleCount;
        public int FiniteCount;
        public int NonFiniteCount;
        public int ClampedLowCount;
        public int ClampedHighCount;

        public float Min;
        public float Max;

        public double Mean;
        public double M2;

        public ulong QuantizedHash64;
    }
}