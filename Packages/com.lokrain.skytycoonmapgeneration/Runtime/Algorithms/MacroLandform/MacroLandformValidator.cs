#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform
{
    /// <summary>
    /// Deterministic validator for Stage 1: Macro Landform.
    /// This validator checks construction invariants, not subjective beauty.
    /// </summary>
    public sealed class MacroLandformValidator
    {
        private const byte MaskOff = 0;
        private const byte MaskOn = 1;

        public MapValidationReport Validate(MacroLandformResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            MapValidationReportBuilder builder = new();

            ValidateDimensions(result, builder);
            ValidateLandAndOceanMasks(result, builder);
            ValidateHardWaterBorder(result, builder);
            ValidateSingleConnectedLandmass(result, builder);
            ValidateFloatFieldRange(result.BaseHeightField.Samples, MapFieldIds.Height, 0f, 1f, builder);
            ValidateFloatFieldRange(result.BuildabilityField.Samples, MapFieldIds.Buildability, 0f, 1f, builder);
            ValidateFloatFieldRange(result.MountainInfluenceField.Samples, MapFieldIds.MountainInfluence, 0f, 1f, builder);
            ValidateFloatFieldRange(result.BasinInfluenceField.Samples, MapFieldIds.BasinInfluence, 0f, 1f, builder);
            ValidateFloatFieldRange(result.PlainInfluenceField.Samples, MapFieldIds.PlainInfluence, 0f, 1f, builder);
            ValidateFloatFieldRange(result.ContinentDistanceField.Samples, MapFieldIds.ContinentDistance, 0f, 1f, builder);
            ValidateBuildability(result, builder);

            StableHash128 reportHash = StableHash128.FromStableName("MacroLandformValidationReport.v0.0.1")
                .Append(result.ArtifactHash)
                .Append(builder.IssueCount);

            return builder.Build(reportHash);
        }

        private static void ValidateDimensions(
            MacroLandformResult result,
            MapValidationReportBuilder builder)
        {
            HeightFieldDimensions dimensions = result.Settings.Dimensions;

            if (result.Dimensions != dimensions)
            {
                builder.AddFatal(
                    MacroLandformIssueIds.DimensionMismatch,
                    MapGenerationStageIds.MacroLandform,
                    MapFieldId.None,
                    "Macro landform result dimensions do not match settings.");
            }
        }

        private static void ValidateLandAndOceanMasks(
            MacroLandformResult result,
            MapValidationReportBuilder builder)
        {
            NativeArray<byte> landMask = result.LandMaskField.Samples;
            NativeArray<byte> oceanMask = result.OceanMaskField.Samples;
            int landCount = 0;

            for (int i = 0; i < landMask.Length; i++)
            {
                byte land = landMask[i];
                byte ocean = oceanMask[i];

                if (!IsBinaryMaskValue(land) || !IsBinaryMaskValue(ocean))
                {
                    builder.AddError(
                        MacroLandformIssueIds.FieldRangeFailure,
                        MapGenerationStageIds.MacroLandform,
                        MapFieldIds.LandMask,
                        "Land/ocean masks must contain only binary values.");
                    return;
                }

                if ((land == MaskOn) == (ocean == MaskOn))
                {
                    builder.AddError(
                        MacroLandformIssueIds.FieldRangeFailure,
                        MapGenerationStageIds.MacroLandform,
                        MapFieldIds.LandMask,
                        "Each cell must be exactly one of land or ocean.");
                    return;
                }

                if (land == MaskOn)
                    landCount++;
            }

            if (landCount != result.Settings.TargetLandSampleCount || landCount != result.LandSampleCount)
            {
                builder.AddError(
                    MacroLandformIssueIds.LandCountMismatch,
                    MapGenerationStageIds.MacroLandform,
                    MapFieldIds.LandMask,
                    "Macro landform land count does not match the target land count.");
            }
        }

        private static void ValidateHardWaterBorder(
            MacroLandformResult result,
            MapValidationReportBuilder builder)
        {
            HeightFieldDimensions dimensions = result.Dimensions;
            int border = result.Settings.HardWaterBorderThickness;
            NativeArray<byte> landMask = result.LandMaskField.Samples;

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    bool isBorder = x < border
                        || y < border
                        || x >= dimensions.Width - border
                        || y >= dimensions.Height - border;

                    if (!isBorder)
                        continue;

                    int index = y * dimensions.Width + x;

                    if (landMask[index] != MaskOff)
                    {
                        builder.AddError(
                            MacroLandformIssueIds.HardWaterBorderFailure,
                            MapGenerationStageIds.MacroLandform,
                            MapFieldIds.LandMask,
                            "Macro landform violates hard water border.");
                        return;
                    }
                }
            }
        }

        private static void ValidateSingleConnectedLandmass(
            MacroLandformResult result,
            MapValidationReportBuilder builder)
        {
            HeightFieldDimensions dimensions = result.Dimensions;
            NativeArray<byte> landMask = result.LandMaskField.Samples;
            NativeArray<byte> visited = new(landMask.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            NativeArray<int> queue = new(landMask.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            try
            {
                int startIndex = -1;

                for (int i = 0; i < landMask.Length; i++)
                {
                    if (landMask[i] == MaskOn)
                    {
                        startIndex = i;
                        break;
                    }
                }

                if (startIndex < 0)
                {
                    builder.AddError(
                        MacroLandformIssueIds.LandConnectivityFailure,
                        MapGenerationStageIds.MacroLandform,
                        MapFieldIds.LandMask,
                        "Macro landform contains no land.");
                    return;
                }

                int connectedLandCount = FloodFillLand(dimensions, landMask, visited, queue, startIndex);

                if (connectedLandCount != result.LandSampleCount)
                {
                    builder.AddError(
                        MacroLandformIssueIds.LandConnectivityFailure,
                        MapGenerationStageIds.MacroLandform,
                        MapFieldIds.LandMask,
                        "Macro landform land is not one 4-connected continent.");
                }
            }
            finally
            {
                visited.Dispose();
                queue.Dispose();
            }
        }

        private static int FloodFillLand(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<byte> visited,
            NativeArray<int> queue,
            int startIndex)
        {
            int head = 0;
            int tail = 0;
            int count = 0;

            queue[tail++] = startIndex;
            visited[startIndex] = MaskOn;

            while (head < tail)
            {
                int index = queue[head++];
                count++;

                dimensions.ToCoordinates(index, out int x, out int y);

                TryVisit(dimensions, landMask, visited, queue, ref tail, x - 1, y);
                TryVisit(dimensions, landMask, visited, queue, ref tail, x + 1, y);
                TryVisit(dimensions, landMask, visited, queue, ref tail, x, y - 1);
                TryVisit(dimensions, landMask, visited, queue, ref tail, x, y + 1);
            }

            return count;
        }

        private static void TryVisit(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<byte> visited,
            NativeArray<int> queue,
            ref int tail,
            int x,
            int y)
        {
            if (!dimensions.Contains(x, y))
                return;

            int index = y * dimensions.Width + x;

            if (visited[index] == MaskOn || landMask[index] == MaskOff)
                return;

            visited[index] = MaskOn;
            queue[tail++] = index;
        }

        private static void ValidateFloatFieldRange(
            NativeArray<float> samples,
            MapFieldId fieldId,
            float minimumInclusive,
            float maximumInclusive,
            MapValidationReportBuilder builder)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                float value = samples[i];

                if (float.IsNaN(value) || value < minimumInclusive || value > maximumInclusive)
                {
                    builder.AddError(
                        MacroLandformIssueIds.FieldRangeFailure,
                        MapGenerationStageIds.MacroLandform,
                        fieldId,
                        "Macro landform field contains an out-of-range or NaN value.");
                    return;
                }
            }
        }

        private static void ValidateBuildability(
            MacroLandformResult result,
            MapValidationReportBuilder builder)
        {
            NativeArray<byte> landMask = result.LandMaskField.Samples;
            NativeArray<float> buildability = result.BuildabilityField.Samples;

            for (int i = 0; i < landMask.Length; i++)
            {
                if (landMask[i] == MaskOff && buildability[i] != 0f)
                {
                    builder.AddError(
                        MacroLandformIssueIds.BuildabilityFailure,
                        MapGenerationStageIds.MacroLandform,
                        MapFieldIds.Buildability,
                        "Ocean cells must have zero buildability.");
                    return;
                }
            }
        }

        private static bool IsBinaryMaskValue(byte value)
        {
            return value == MaskOff || value == MaskOn;
        }
    }
}
