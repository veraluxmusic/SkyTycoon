using System;
using Unity.Mathematics;
using UnityEngine;

namespace Lokrain.SkyTycoon.Map
{
    [CreateAssetMenu(
        fileName = "Math Noise Preview Profile",
        menuName = "Sky Tycoon/Map/Math Noise Preview Profile")]
    public sealed class MathNoisePreviewProfile : ScriptableObject
    {
        public enum NoiseKind
        {
            Simplex,
            ClassicPerlin,
            CellularF1,
            CellularF2,
            CellularF2MinusF1
        }

        [Header("Texture")]
        [SerializeField, Min(16)] private int resolution = 256;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;

        [Header("Noise")]
        [SerializeField] private NoiseKind noiseKind = NoiseKind.Simplex;
        [SerializeField] private uint seed = 1;
        [SerializeField, Min(0.0001f)] private float scale = 64f;

        [Header("Fractal")]
        [SerializeField, Range(1, 12)] private int octaves = 4;
        [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;
        [SerializeField, Min(1f)] private float lacunarity = 2f;

        [Header("Output")]
        [SerializeField] private bool normalizeByAmplitude = true;
        [SerializeField] private bool invert;
        [SerializeField, Range(-1f, 1f)] private float bias;
        [SerializeField, Min(0.01f)] private float contrast = 1f;
        [SerializeField] private Gradient colorRamp = CreateDefaultRamp();

        public int Resolution => Mathf.Max(16, resolution);
        public FilterMode FilterMode => filterMode;

        public void GenerateInto(Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            int size = Resolution;

            if (texture.width != size || texture.height != size)
                throw new ArgumentException("Texture dimensions must match profile resolution.", nameof(texture));

            var pixels = new Color32[size * size];

            float safeScale = Mathf.Max(0.0001f, scale);
            float2 seedOffset = CreateSeedOffset(seed);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float2 uv = new float2(x, y) / math.max(1, size - 1);
                    float2 p = uv * safeScale + seedOffset;

                    float value = SampleFbm(p);
                    value = ApplyOutputMapping(value);

                    Color color = colorRamp.Evaluate(value);
                    pixels[x + y * size] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.name = name + " Preview";
        }

        private float SampleFbm(float2 p)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float amplitudeSum = 0f;

            int octaveCount = Mathf.Max(1, octaves);

            for (int i = 0; i < octaveCount; i++)
            {
                float sample = SampleNoise(p * frequency);

                sum += sample * amplitude;
                amplitudeSum += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (normalizeByAmplitude && amplitudeSum > 0f)
                sum /= amplitudeSum;

            return sum;
        }

        private float SampleNoise(float2 p)
        {
            switch (noiseKind)
            {
                case NoiseKind.Simplex:
                    // Unity.Mathematics noise.snoise(float2) returns approximately [-1, 1].
                    return noise.snoise(p);

                case NoiseKind.ClassicPerlin:
                    // Unity.Mathematics noise.cnoise(float2) returns approximately [-1, 1].
                    return noise.cnoise(p);

                case NoiseKind.CellularF1:
                {
                    float2 cell = noise.cellular(p);
                    return 1f - math.saturate(cell.x);
                }

                case NoiseKind.CellularF2:
                {
                    float2 cell = noise.cellular(p);
                    return 1f - math.saturate(cell.y);
                }

                case NoiseKind.CellularF2MinusF1:
                {
                    float2 cell = noise.cellular(p);
                    return math.saturate(cell.y - cell.x) * 2f - 1f;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float ApplyOutputMapping(float value)
        {
            // Convert common [-1, 1] noise into [0, 1].
            value = value * 0.5f + 0.5f;

            value += bias;
            value = (value - 0.5f) * contrast + 0.5f;
            value = math.saturate(value);

            return invert ? 1f - value : value;
        }

        private static float2 CreateSeedOffset(uint seedValue)
        {
            uint h0 = math.hash(new uint2(seedValue, 0xA2C2A3u));
            uint h1 = math.hash(new uint2(seedValue, 0x7B1D5Fu));

            return new float2(
                (h0 & 0xFFFFu) / 65535f,
                (h1 & 0xFFFFu) / 65535f) * 10_000f;
        }

        private static Gradient CreateDefaultRamp()
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            };
        }
    }
}