#nullable enable

using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    internal static class OpenSimplex2SGradientTable2D
    {
        public static void Build(NativeArray<float2> destination)
        {
            if (!destination.IsCreated)
                throw new ArgumentException("Destination gradient table must be created.", nameof(destination));
            if (destination.Length != OpenSimplex2SFbmConstants.GradientTableSize2D)
                throw new ArgumentException("Destination gradient table has an unexpected length.", nameof(destination));

            const float normalizer2D = 0.05481866495625118f;

            ReadOnlySpan<float> source = stackalloc float[]
            {
                 0.38268343236509f,   0.923879532511287f,
                 0.923879532511287f,  0.38268343236509f,
                 0.923879532511287f, -0.38268343236509f,
                 0.38268343236509f,  -0.923879532511287f,
                -0.38268343236509f,  -0.923879532511287f,
                -0.923879532511287f, -0.38268343236509f,
                -0.923879532511287f,  0.38268343236509f,
                -0.38268343236509f,   0.923879532511287f,
                 0.130526192220052f,  0.99144486137381f,
                 0.608761429008721f,  0.793353340291235f,
                 0.793353340291235f,  0.608761429008721f,
                 0.99144486137381f,   0.130526192220051f,
                 0.99144486137381f,  -0.130526192220051f,
                 0.793353340291235f, -0.60876142900872f,
                 0.608761429008721f, -0.793353340291235f,
                 0.130526192220052f, -0.99144486137381f,
                -0.130526192220052f, -0.99144486137381f,
                -0.608761429008721f, -0.793353340291235f,
                -0.793353340291235f, -0.608761429008721f,
                -0.99144486137381f,  -0.130526192220052f,
                -0.99144486137381f,   0.130526192220051f,
                -0.793353340291235f,  0.608761429008721f,
                -0.608761429008721f,  0.793353340291235f,
                -0.130526192220052f,  0.99144486137381f
            };

            int sourceVectorCount = source.Length / 2;

            for (int i = 0; i < destination.Length; i++)
            {
                int sourceIndex = (i % sourceVectorCount) * 2;
                destination[i] = new float2(source[sourceIndex] / normalizer2D, source[sourceIndex + 1] / normalizer2D);
            }
        }
    }
}
