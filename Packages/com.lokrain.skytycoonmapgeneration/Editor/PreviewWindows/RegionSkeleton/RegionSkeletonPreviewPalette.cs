#nullable enable

using System;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton
{
    /// <summary>
    /// Deterministic editor-only color palette for skeleton fields.
    /// This class deliberately does not live in Runtime because colors are preview metadata, not generation data.
    /// </summary>
    public static class RegionSkeletonPreviewPalette
    {
        private static readonly Color32[] RegionColors =
        {
            new(214, 083, 083, 255),
            new(083, 143, 214, 255),
            new(093, 177, 104, 255),
            new(221, 170, 073, 255),
            new(157, 103, 213, 255),
            new(067, 177, 178, 255),
            new(205, 104, 162, 255),
            new(155, 139, 089, 255)
        };

        public static readonly Color32 Invalid = new(255, 000, 255, 255);
        public static readonly Color32 Empty = new(028, 028, 032, 255);
        public static readonly Color32 Neutral = new(220, 220, 224, 255);
        public static readonly Color32 NeutralAccent = new(180, 180, 188, 255);
        public static readonly Color32 Boundary = new(018, 018, 022, 255);
        public static readonly Color32 GridLine = new(000, 000, 000, 255);

        public static Color32 GetRegionColor(int regionId)
        {
            if (regionId <= 0)
                return Empty;

            int index = (regionId - 1) % RegionColors.Length;
            return RegionColors[index];
        }

        public static Color32 GetNeutralZoneColor(int neutralZoneId)
        {
            if (neutralZoneId <= 0)
                return Empty;

            if (neutralZoneId == 1)
                return Neutral;

            unchecked
            {
                byte value = (byte)Math.Max(72, 220 - neutralZoneId * 28);
                return new Color32(value, value, (byte)Math.Min(255, value + 16), 255);
            }
        }

        public static Color32 Darken(Color32 color, byte amount)
        {
            return new Color32(
                (byte)Math.Max(0, color.r - amount),
                (byte)Math.Max(0, color.g - amount),
                (byte)Math.Max(0, color.b - amount),
                color.a);
        }
    }
}
