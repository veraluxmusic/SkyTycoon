using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Generation.Algorithms.OpenSimplex2SFbm
{
    public readonly struct Evaluator2D
    {
        private const long PrimeX = 0x5205402B9270C86FL;
        private const long PrimeY = 0x598CD327003817B5L;
        private const long HashMultiplier = 0x53A3F72DEEC546F5L;

        private const int GradientCount2DExponent = 7;
        private const int GradientCount2D = 1 << GradientCount2DExponent;

        private const float Root2Over2 = 0.7071067811865476f;
        private const float Skew2D = 0.366025403784439f;
        private const float Unskew2D = -0.21132486540518713f;
        private const float RSquared2D = 2f / 3f;

        [ReadOnly]
        private readonly NativeArray<float2> gradients2D;

        public Evaluator2D(NativeArray<float2> gradients2D)
        {
            this.gradients2D = gradients2D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(long seed, float2 point, Orientation orientation)
        {
            return orientation == Orientation.ImproveX
                ? EvaluateImproveX(seed, point.x, point.y)
                : EvaluateStandard(seed, point.x, point.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EvaluateStandard(long seed, float x, float y)
        {
            float s = Skew2D * (x + y);
            return EvaluateUnskewedBase(seed, x + s, y + s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EvaluateImproveX(long seed, float x, float y)
        {
            float xx = x * Root2Over2;
            float yy = y * (Root2Over2 * (1f + 2f * Skew2D));
            return EvaluateUnskewedBase(seed, yy + xx, yy - xx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EvaluateUnskewedBase(long seed, float xs, float ys)
        {
            int xsb = FastFloor(xs);
            int ysb = FastFloor(ys);
            float xi = xs - xsb;
            float yi = ys - ysb;

            long xsbp;
            long ysbp;
            unchecked
            {
                xsbp = xsb * PrimeX;
                ysbp = ysb * PrimeY;
            }

            float t = (xi + yi) * Unskew2D;
            float dx0 = xi + t;
            float dy0 = yi + t;

            float a0 = RSquared2D - dx0 * dx0 - dy0 * dy0;
            float value = Kernel(a0) * Gradient(seed, xsbp, ysbp, dx0, dy0);

            float a1 = (2f * (1f + 2f * Unskew2D) * (1f / Unskew2D + 2f)) * t
                     + ((-2f * (1f + 2f * Unskew2D) * (1f + 2f * Unskew2D)) + a0);
            float dx1 = dx0 - (1f + 2f * Unskew2D);
            float dy1 = dy0 - (1f + 2f * Unskew2D);
            value += Kernel(a1) * Gradient(seed, unchecked(xsbp + PrimeX), unchecked(ysbp + PrimeY), dx1, dy1);

            float xmyi = xi - yi;
            if (t < Unskew2D)
            {
                if (xi + xmyi > 1f)
                {
                    Add(seed, unchecked(xsbp + (PrimeX << 1)), unchecked(ysbp + PrimeY),
                        dx0 - (3f * Unskew2D + 2f), dy0 - (3f * Unskew2D + 1f), ref value);
                }
                else
                {
                    Add(seed, xsbp, unchecked(ysbp + PrimeY),
                        dx0 - Unskew2D, dy0 - (Unskew2D + 1f), ref value);
                }

                if (yi - xmyi > 1f)
                {
                    Add(seed, unchecked(xsbp + PrimeX), unchecked(ysbp + (PrimeY << 1)),
                        dx0 - (3f * Unskew2D + 1f), dy0 - (3f * Unskew2D + 2f), ref value);
                }
                else
                {
                    Add(seed, unchecked(xsbp + PrimeX), ysbp,
                        dx0 - (Unskew2D + 1f), dy0 - Unskew2D, ref value);
                }
            }
            else
            {
                if (xi + xmyi < 0f)
                {
                    Add(seed, unchecked(xsbp - PrimeX), ysbp,
                        dx0 + (1f + Unskew2D), dy0 + Unskew2D, ref value);
                }
                else
                {
                    Add(seed, unchecked(xsbp + PrimeX), ysbp,
                        dx0 - (Unskew2D + 1f), dy0 - Unskew2D, ref value);
                }

                if (yi < xmyi)
                {
                    Add(seed, xsbp, unchecked(ysbp - PrimeY),
                        dx0 + Unskew2D, dy0 + (Unskew2D + 1f), ref value);
                }
                else
                {
                    Add(seed, xsbp, unchecked(ysbp + PrimeY),
                        dx0 - Unskew2D, dy0 - (Unskew2D + 1f), ref value);
                }
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(long seed, long xsvp, long ysvp, float dx, float dy, ref float value)
        {
            float a = RSquared2D - dx * dx - dy * dy;
            if (a > 0f)
                value += Kernel(a) * Gradient(seed, xsvp, ysvp, dx, dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Kernel(float attenuation)
        {
            float a2 = attenuation * attenuation;
            return a2 * a2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Gradient(long seed, long xsvp, long ysvp, float dx, float dy)
        {
            long hash;
            unchecked
            {
                hash = seed ^ xsvp ^ ysvp;
                hash *= HashMultiplier;
                hash ^= (long)((ulong)hash >> (64 - GradientCount2DExponent + 1));
            }

            int index = (int)(((ulong)hash & ((ulong)(GradientCount2D - 1) << 1)) >> 1);
            float2 gradient = gradients2D[index];
            return gradient.x * dx + gradient.y * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(float value)
        {
            int integer = (int)value;
            return value < integer ? integer - 1 : integer;
        }
    }
}
