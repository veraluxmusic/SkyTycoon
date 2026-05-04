using Lokrain.Burstable.Tiles;
using Lokrain.Burstable.Workspace;
using Unity.Collections;
using UnityEngine;

namespace Lokrain.Burstable.Editor.Preview
{
    /// <summary>
    /// Builds editor preview textures from generated map workspace fields.
    /// </summary>
    /// <remarks>
    /// This type is editor-only visualization code. It must not affect generation,
    /// determinism, serialized map data, or runtime field values.
    ///
    /// The caller owns the returned <see cref="Texture2D"/> and must destroy it when
    /// the preview window or owning editor tool is disposed. If a supplied texture has
    /// incompatible dimensions, this builder destroys it and returns a replacement.
    /// </remarks>
    public static class MapPreviewTextureBuilder
    {
        private const string TextureName = "Burstable Map Preview";

        /// <summary>
        /// Builds or updates a preview texture for the selected map preview mode.
        /// </summary>
        /// <param name="texture">
        /// Existing preview texture to reuse when dimensions match the workspace.
        /// If dimensions do not match, the texture is destroyed and replaced.
        /// </param>
        /// <param name="workspace">Generated map workspace containing the source preview fields.</param>
        /// <param name="mode">Preview mode selecting which workspace field to visualize.</param>
        /// <returns>
        /// A texture containing the requested preview visualization, or the original texture
        /// when <paramref name="workspace"/> is null.
        /// </returns>
        public static Texture2D BuildOrUpdate(
            Texture2D texture,
            MapWorkspace workspace,
            MapPreviewMode mode)
        {
            if (workspace == null)
            {
                return texture;
            }

            texture = EnsureTexture(texture, workspace.Width, workspace.Height);

            using var colors = new NativeArray<Color32>(
                workspace.Length,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);

            switch (mode)
            {
                case MapPreviewMode.Elevation:
                    WriteUInt16Gradient(colors, workspace.Elevation.Values, 255);
                    break;

                case MapPreviewMode.Moisture:
                    WriteUInt16Gradient(colors, workspace.Moisture.Values, ushort.MaxValue);
                    break;

                case MapPreviewMode.Temperature:
                    WriteUInt16Gradient(colors, workspace.Temperature.Values, ushort.MaxValue);
                    break;

                case MapPreviewMode.Slope:
                    WriteUInt8Gradient(colors, workspace.Slope.Values, byte.MaxValue);
                    break;

                case MapPreviewMode.Biome:
                    WriteBiome(colors, workspace.BiomeKind.Values);
                    break;

                case MapPreviewMode.Terrain:
                default:
                    WriteTerrain(colors, workspace.TerrainKind.Values);
                    break;
            }

            texture.SetPixelData(colors, 0);
            texture.Apply(false, false);

            return texture;
        }

        /// <summary>
        /// Creates a compatible preview texture or reuses the existing one.
        /// </summary>
        private static Texture2D EnsureTexture(Texture2D texture, int width, int height)
        {
            if (texture != null &&
                texture.width == width &&
                texture.height == height)
            {
                return texture;
            }

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }

            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = TextureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        /// <summary>
        /// Writes a grayscale visualization for an unsigned 16-bit field.
        /// </summary>
        /// <remarks>
        /// The field value is normalized by <paramref name="max"/> and written into all RGB channels.
        /// </remarks>
        private static void WriteUInt16Gradient(
            NativeArray<Color32> colors,
            NativeArray<ushort> values,
            int max)
        {
            if (max <= 0)
            {
                WriteSolid(colors, new Color32(0, 0, 0, 255));
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                var channel = ToByteChannel(values[i], max);
                colors[i] = new Color32(channel, channel, channel, 255);
            }
        }

        /// <summary>
        /// Writes a grayscale visualization for an unsigned 8-bit field.
        /// </summary>
        /// <remarks>
        /// The field value is normalized by <paramref name="max"/> and written into all RGB channels.
        /// </remarks>
        private static void WriteUInt8Gradient(
            NativeArray<Color32> colors,
            NativeArray<byte> values,
            int max)
        {
            if (max <= 0)
            {
                WriteSolid(colors, new Color32(0, 0, 0, 255));
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                var channel = ToByteChannel(values[i], max);
                colors[i] = new Color32(channel, channel, channel, 255);
            }
        }

        /// <summary>
        /// Writes a categorical terrain visualization.
        /// </summary>
        private static void WriteTerrain(
            NativeArray<Color32> colors,
            NativeArray<byte> values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                colors[i] = GetTerrainColor((TerrainKind)values[i]);
            }
        }

        /// <summary>
        /// Writes a categorical biome visualization.
        /// </summary>
        private static void WriteBiome(
            NativeArray<Color32> colors,
            NativeArray<byte> values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                colors[i] = GetBiomeColor((BiomeKind)values[i]);
            }
        }

        /// <summary>
        /// Fills the preview with a single color.
        /// </summary>
        private static void WriteSolid(NativeArray<Color32> colors, Color32 color)
        {
            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
        }

        /// <summary>
        /// Converts an integer field value into a normalized 8-bit color channel.
        /// </summary>
        private static byte ToByteChannel(int value, int max)
        {
            var normalized = value * 255 / max;

            if (normalized <= 0)
            {
                return 0;
            }

            if (normalized >= 255)
            {
                return 255;
            }

            return (byte)normalized;
        }

        /// <summary>
        /// Gets the editor preview color for a terrain category.
        /// </summary>
        private static Color32 GetTerrainColor(TerrainKind terrain)
        {
            switch (terrain)
            {
                case TerrainKind.Sea:
                    return new Color32(35, 83, 150, 255);

                case TerrainKind.Coast:
                    return new Color32(216, 198, 124, 255);

                case TerrainKind.Grass:
                    return new Color32(78, 151, 70, 255);

                case TerrainKind.Rough:
                    return new Color32(110, 125, 82, 255);

                case TerrainKind.Forest:
                    return new Color32(39, 106, 48, 255);

                case TerrainKind.Desert:
                    return new Color32(216, 185, 91, 255);

                case TerrainKind.Snow:
                    return new Color32(232, 235, 235, 255);

                case TerrainKind.Mountain:
                    return new Color32(110, 110, 110, 255);

                case TerrainKind.River:
                    return new Color32(48, 118, 190, 255);

                case TerrainKind.Lake:
                    return new Color32(42, 100, 170, 255);

                case TerrainKind.Void:
                default:
                    return new Color32(0, 0, 0, 255);
            }
        }

        /// <summary>
        /// Gets the editor preview color for a biome category.
        /// </summary>
        private static Color32 GetBiomeColor(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Ocean:
                    return new Color32(35, 83, 150, 255);

                case BiomeKind.Coast:
                    return new Color32(216, 198, 124, 255);

                case BiomeKind.Grassland:
                    return new Color32(78, 151, 70, 255);

                case BiomeKind.Forest:
                    return new Color32(39, 106, 48, 255);

                case BiomeKind.Desert:
                    return new Color32(216, 185, 91, 255);

                case BiomeKind.Tundra:
                    return new Color32(148, 164, 152, 255);

                case BiomeKind.Snow:
                    return new Color32(232, 235, 235, 255);

                case BiomeKind.Mountain:
                    return new Color32(110, 110, 110, 255);

                case BiomeKind.None:
                default:
                    return new Color32(0, 0, 0, 255);
            }
        }
    }
}