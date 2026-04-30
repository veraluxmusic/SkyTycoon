#nullable enable

using System;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.MacroLandform
{
    /// <summary>
    /// Deterministic editor-only palette for macro-landform fields.
    /// Runtime generation deliberately knows nothing about these colors.
    /// </summary>
    public static class MacroLandformPreviewPalette
    {
        public static readonly Color32 Invalid = new(255, 0, 255, 255);
        public static readonly Color32 Boundary = new(12, 12, 16, 255);
        public static readonly Color32 Coast = new(242, 232, 170, 255);
        public static readonly Color32 OceanDeep = new(22, 54, 92, 255);
        public static readonly Color32 OceanShallow = new(62, 116, 152, 255);
        public static readonly Color32 LowLand = new(76, 136, 82, 255);
        public static readonly Color32 MidLand = new(124, 148, 84, 255);
        public static readonly Color32 HighLand = new(144, 116, 86, 255);
        public static readonly Color32 Mountain = new(190, 186, 174, 255);
        public static readonly Color32 MaskOff = new(18, 18, 22, 255);
        public static readonly Color32 MaskOn = new(224, 224, 224, 255);

        public static Color32 Grayscale(float value)
        {
            byte channel = ToByte(value);
            return new Color32(channel, channel, channel, 255);
        }

        public static Color32 Heat(float value)
        {
            value = Mathf.Clamp01(value);

            if (value < 0.5f)
            {
                float t = value * 2.0f;
                return Lerp(new Color32(28, 54, 130, 255), new Color32(74, 166, 118, 255), t);
            }

            return Lerp(new Color32(74, 166, 118, 255), new Color32(230, 210, 94, 255), (value - 0.5f) * 2.0f);
        }

        public static Color32 Buildability(float value)
        {
            return Lerp(new Color32(90, 40, 40, 255), new Color32(72, 174, 92, 255), Mathf.Clamp01(value));
        }

        public static Color32 CompositeLand(float height, float slope, float mountainInfluence)
        {
            height = Mathf.Clamp01(height);
            slope = Mathf.Clamp01(slope);
            mountainInfluence = Mathf.Clamp01(mountainInfluence);

            Color32 color;

            if (height < 0.40f)
                color = Lerp(LowLand, MidLand, height / 0.40f);
            else if (height < 0.72f)
                color = Lerp(MidLand, HighLand, (height - 0.40f) / 0.32f);
            else
                color = Lerp(HighLand, Mountain, (height - 0.72f) / 0.28f);

            float ridge = Mathf.Clamp01(mountainInfluence * 0.45f + slope * 0.35f);
            return Lerp(color, Mountain, ridge);
        }

        public static Color32 CompositeOcean(float coastDistance)
        {
            float shallow = Mathf.Clamp01(1.0f - coastDistance / 12.0f);
            return Lerp(OceanDeep, OceanShallow, shallow);
        }

        public static Color32 Overlay(Color32 baseColor, Color32 overlayColor, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            float inverse = 1.0f - alpha;

            return new Color32(
                (byte)Mathf.RoundToInt(baseColor.r * inverse + overlayColor.r * alpha),
                (byte)Mathf.RoundToInt(baseColor.g * inverse + overlayColor.g * alpha),
                (byte)Mathf.RoundToInt(baseColor.b * inverse + overlayColor.b * alpha),
                255);
        }

        public static Color32 Darken(Color32 color, byte amount)
        {
            return new Color32(
                (byte)Math.Max(0, color.r - amount),
                (byte)Math.Max(0, color.g - amount),
                (byte)Math.Max(0, color.b - amount),
                color.a);
        }

        private static Color32 Lerp(Color32 from, Color32 to, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.r, to.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.g, to.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(from.b, to.b, t)),
                255);
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255.0f);
        }
    }
}
