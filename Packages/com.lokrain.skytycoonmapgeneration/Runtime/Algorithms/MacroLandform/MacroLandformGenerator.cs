#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Collections;
using Unity.Mathematics;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform
{
    /// <summary>
    /// Deterministic Stage 1 macro-landform generator.
    ///
    /// Production contract:
    /// - keeps the hard ocean boundary;
    /// - generates exactly the requested land count;
    /// - keeps one 4-connected continent;
    /// - derives terrain character from strategic region roles;
    /// - emits all major fields needed for preview, validation, and later stages.
    ///
    /// This implementation is intentionally single-threaded for v0.0.1 correctness and debuggability.
    /// The generated fields and settings are structured so the hot loops can be moved to Burst jobs
    /// without changing the public stage contract.
    /// </summary>
    public sealed class MacroLandformGenerator
    {
        private const byte MaskOff = 0;
        private const byte MaskOn = 1;

        public MacroLandformResult Generate(
            MacroLandformSettings settings,
            RegionSkeletonResult skeleton,
            RegionRoleCatalog roleCatalog,
            Allocator allocator = Allocator.Persistent)
        {
            settings.Validate();

            if (skeleton == null)
                throw new ArgumentNullException(nameof(skeleton));

            if (roleCatalog == null)
                throw new ArgumentNullException(nameof(roleCatalog));

            if (skeleton.Dimensions != settings.Dimensions)
                throw new ArgumentException("Macro landform settings dimensions must match the region skeleton result.", nameof(skeleton));

            roleCatalog.ValidateForRegionCount(skeleton.Settings.PlayerRegionCount);

            if (allocator == Allocator.Invalid || allocator == Allocator.None)
                throw new ArgumentOutOfRangeException(nameof(allocator), allocator, "Macro landform output allocator must create native storage.");

            NativeField2D<float> baseHeight = new(MapFieldIds.Height, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<byte> landMask = new(MapFieldIds.LandMask, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<byte> oceanMask = new(MapFieldIds.OceanMask, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> coastDistance = new(MapFieldIds.CoastDistance, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> continentDistance = new(MapFieldIds.ContinentDistance, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> slope = new(MapFieldIds.Slope, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> buildability = new(MapFieldIds.Buildability, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> mountainInfluence = new(MapFieldIds.MountainInfluence, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> basinInfluence = new(MapFieldIds.BasinInfluence, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);
            NativeField2D<float> plainInfluence = new(MapFieldIds.PlainInfluence, settings.Dimensions, allocator, NativeArrayOptions.UninitializedMemory);

            NativeArray<float> landScores = default;
            NativeArray<int> componentIds = default;
            NativeArray<int> floodQueue = default;

            try
            {
                int sampleCount = settings.Dimensions.SampleCount;
                landScores = new NativeArray<float>(sampleCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                componentIds = new NativeArray<int>(sampleCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                floodQueue = new NativeArray<int>(sampleCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                RegionPrimaryIdentity[] identityByRegionValue = BuildRegionIdentityLookup(skeleton, roleCatalog);

                GenerateScoresAndRoleInfluence(
                    settings,
                    skeleton,
                    identityByRegionValue,
                    landScores,
                    mountainInfluence.Samples,
                    basinInfluence.Samples,
                    plainInfluence.Samples);

                int landCount = CutToTargetLandCount(settings, landScores, landMask.Samples, oceanMask.Samples);
                landCount = KeepLargestLandComponent(settings.Dimensions, landMask.Samples, oceanMask.Samples, componentIds, floodQueue);
                landCount = GrowLandBackToTarget(settings, landScores, landMask.Samples, oceanMask.Samples, landCount);

                BuildHeight(
                    settings,
                    landScores,
                    landMask.Samples,
                    baseHeight.Samples,
                    mountainInfluence.Samples,
                    basinInfluence.Samples,
                    plainInfluence.Samples);

                BuildDistanceFields(settings.Dimensions, landMask.Samples, coastDistance.Samples, continentDistance.Samples, floodQueue);
                BuildSlope(settings.Dimensions, baseHeight.Samples, slope.Samples);
                BuildBuildability(settings, landMask.Samples, slope.Samples, mountainInfluence.Samples, buildability.Samples);

                StableHash128 artifactHash = ComputeArtifactHash(
                    settings,
                    baseHeight.Samples,
                    landMask.Samples,
                    oceanMask.Samples,
                    coastDistance.Samples,
                    continentDistance.Samples,
                    slope.Samples,
                    buildability.Samples,
                    mountainInfluence.Samples,
                    basinInfluence.Samples,
                    plainInfluence.Samples,
                    landCount);

                return new MacroLandformResult(
                    settings,
                    baseHeight,
                    landMask,
                    oceanMask,
                    coastDistance,
                    continentDistance,
                    slope,
                    buildability,
                    mountainInfluence,
                    basinInfluence,
                    plainInfluence,
                    landCount,
                    artifactHash);
            }
            catch
            {
                baseHeight.Dispose();
                landMask.Dispose();
                oceanMask.Dispose();
                coastDistance.Dispose();
                continentDistance.Dispose();
                slope.Dispose();
                buildability.Dispose();
                mountainInfluence.Dispose();
                basinInfluence.Dispose();
                plainInfluence.Dispose();
                throw;
            }
            finally
            {
                if (landScores.IsCreated)
                    landScores.Dispose();

                if (componentIds.IsCreated)
                    componentIds.Dispose();

                if (floodQueue.IsCreated)
                    floodQueue.Dispose();
            }
        }

        private static RegionPrimaryIdentity[] BuildRegionIdentityLookup(
            RegionSkeletonResult skeleton,
            RegionRoleCatalog roleCatalog)
        {
            RegionPrimaryIdentity[] identityByRegionValue = new RegionPrimaryIdentity[skeleton.Settings.PlayerRegionCount + 1];

            for (int i = 0; i < identityByRegionValue.Length; i++)
                identityByRegionValue[i] = RegionPrimaryIdentity.BalancedFrontier;

            for (int i = 0; i < skeleton.RoleAssignments.Count; i++)
            {
                RegionRoleAssignment assignment = skeleton.RoleAssignments[i];
                RegionRoleDefinition definition = roleCatalog.GetRequired(assignment.RoleId);
                identityByRegionValue[assignment.RegionId.Value] = definition.PrimaryIdentity;
            }

            return identityByRegionValue;
        }

        private static void GenerateScoresAndRoleInfluence(
            MacroLandformSettings settings,
            RegionSkeletonResult skeleton,
            RegionPrimaryIdentity[] identityByRegionValue,
            NativeArray<float> landScores,
            NativeArray<float> mountainInfluence,
            NativeArray<float> basinInfluence,
            NativeArray<float> plainInfluence)
        {
            HeightFieldDimensions dimensions = settings.Dimensions;
            float centerX = (dimensions.Width - 1) * 0.5f;
            float centerY = (dimensions.Height - 1) * 0.5f;
            float maxRadius = math.max(1f, math.min(dimensions.Width, dimensions.Height) * 0.5f - settings.HardWaterBorderThickness);
            uint seed = settings.Seed.Value;

            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int index = y * dimensions.Width + x;

                    if (IsHardWaterBorder(settings, x, y))
                    {
                        landScores[index] = float.NegativeInfinity;
                        mountainInfluence[index] = 0f;
                        basinInfluence[index] = 0f;
                        plainInfluence[index] = 0f;
                        continue;
                    }

                    float2 p = new(x, y);
                    float warpX = FbmValueNoise(p, seed ^ 0xA2C2A9B5u, 2, settings.DomainWarpWavelengthTiles, 0.55f, 2.0f);
                    float warpY = FbmValueNoise(p, seed ^ 0x27D4EB2Fu, 2, settings.DomainWarpWavelengthTiles, 0.55f, 2.0f);
                    float2 warped = p + new float2(warpX, warpY) * settings.DomainWarpAmplitudeTiles;

                    float dx = (warped.x - centerX) / maxRadius;
                    float dy = (warped.y - centerY) / maxRadius;
                    float radial01 = math.saturate(math.sqrt(dx * dx + dy * dy));
                    float radialScore = 1.0f - math.pow(radial01, settings.ContinentFalloffExponent);

                    float largeNoise = FbmValueNoise(warped, seed ^ 0x85EBCA6Bu, settings.FbmOctaves, settings.FbmBaseWavelengthTiles, settings.FbmPersistence, settings.FbmLacunarity);
                    float fineNoise = FbmValueNoise(warped, seed ^ 0xC2B2AE35u, 2, math.max(8f, settings.FbmBaseWavelengthTiles * 0.25f), 0.50f, 2.15f);

                    int regionValue = skeleton.RegionIdField.Samples[index];
                    RegionPrimaryIdentity identity = regionValue > 0 && regionValue < identityByRegionValue.Length
                        ? identityByRegionValue[regionValue]
                        : RegionPrimaryIdentity.BalancedFrontier;

                    GetRoleInfluence(identity, out float mountainRole, out float basinRole, out float plainRole);

                    float mountainNoise = math.saturate(0.5f + 0.5f * FbmValueNoise(warped, seed ^ 0x165667B1u, 3, settings.FbmBaseWavelengthTiles * 0.45f, 0.58f, 2.1f));
                    float basinNoise = math.saturate(0.5f + 0.5f * FbmValueNoise(warped, seed ^ 0xD3A2646Cu, 3, settings.FbmBaseWavelengthTiles * 0.60f, 0.55f, 2.0f));
                    float plainNoise = math.saturate(0.5f + 0.5f * FbmValueNoise(warped, seed ^ 0xFD7046C5u, 2, settings.FbmBaseWavelengthTiles * 0.80f, 0.50f, 2.0f));

                    mountainInfluence[index] = math.saturate(mountainRole * (0.45f + mountainNoise * 0.55f));
                    basinInfluence[index] = math.saturate(basinRole * (0.50f + basinNoise * 0.50f));
                    plainInfluence[index] = math.saturate(plainRole * (0.55f + plainNoise * 0.45f));

                    float roleLandBias = plainInfluence[index] * 0.050f + basinInfluence[index] * 0.025f - mountainInfluence[index] * 0.025f;
                    landScores[index] = radialScore + largeNoise * settings.FbmAmplitude + fineNoise * 0.05f + roleLandBias;
                }
            }
        }

        private static int CutToTargetLandCount(
            MacroLandformSettings settings,
            NativeArray<float> landScores,
            NativeArray<byte> landMask,
            NativeArray<byte> oceanMask)
        {
            HeightFieldDimensions dimensions = settings.Dimensions;
            int interiorCapacity = (dimensions.Width - settings.HardWaterBorderThickness * 2)
                * (dimensions.Height - settings.HardWaterBorderThickness * 2);

            LandScoreCandidate[] candidates = new LandScoreCandidate[interiorCapacity];
            int candidateCount = 0;

            for (int y = settings.HardWaterBorderThickness; y < dimensions.Height - settings.HardWaterBorderThickness; y++)
            {
                for (int x = settings.HardWaterBorderThickness; x < dimensions.Width - settings.HardWaterBorderThickness; x++)
                {
                    int index = y * dimensions.Width + x;
                    candidates[candidateCount++] = new LandScoreCandidate(index, landScores[index]);
                }
            }

            Array.Sort(candidates, 0, candidateCount);

            for (int i = 0; i < landMask.Length; i++)
            {
                landMask[i] = MaskOff;
                oceanMask[i] = MaskOn;
            }

            int targetLandCount = settings.TargetLandSampleCount;

            for (int i = 0; i < targetLandCount; i++)
            {
                int index = candidates[i].Index;
                landMask[index] = MaskOn;
                oceanMask[index] = MaskOff;
            }

            return targetLandCount;
        }

        private static int KeepLargestLandComponent(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<byte> oceanMask,
            NativeArray<int> componentIds,
            NativeArray<int> queue)
        {
            for (int i = 0; i < componentIds.Length; i++)
                componentIds[i] = 0;

            int nextComponentId = 1;
            int largestComponentId = 0;
            int largestComponentSize = 0;

            for (int i = 0; i < landMask.Length; i++)
            {
                if (landMask[i] == MaskOff || componentIds[i] != 0)
                    continue;

                int size = FloodFillComponent(dimensions, landMask, componentIds, queue, i, nextComponentId);

                if (size > largestComponentSize)
                {
                    largestComponentSize = size;
                    largestComponentId = nextComponentId;
                }

                nextComponentId++;
            }

            int keptLandCount = 0;

            for (int i = 0; i < landMask.Length; i++)
            {
                if (landMask[i] == MaskOn && componentIds[i] == largestComponentId)
                {
                    keptLandCount++;
                    oceanMask[i] = MaskOff;
                }
                else
                {
                    landMask[i] = MaskOff;
                    oceanMask[i] = MaskOn;
                }
            }

            return keptLandCount;
        }

        private static int GrowLandBackToTarget(
            MacroLandformSettings settings,
            NativeArray<float> landScores,
            NativeArray<byte> landMask,
            NativeArray<byte> oceanMask,
            int currentLandCount)
        {
            int targetLandCount = settings.TargetLandSampleCount;

            if (currentLandCount >= targetLandCount)
                return currentLandCount;

            HeightFieldDimensions dimensions = settings.Dimensions;
            int candidateCapacity = dimensions.SampleCount - currentLandCount;
            LandScoreCandidate[] candidates = new LandScoreCandidate[candidateCapacity];
            int candidateCount = 0;

            for (int y = settings.HardWaterBorderThickness; y < dimensions.Height - settings.HardWaterBorderThickness; y++)
            {
                for (int x = settings.HardWaterBorderThickness; x < dimensions.Width - settings.HardWaterBorderThickness; x++)
                {
                    int index = y * dimensions.Width + x;

                    if (landMask[index] == MaskOff)
                        candidates[candidateCount++] = new LandScoreCandidate(index, landScores[index]);
                }
            }

            Array.Sort(candidates, 0, candidateCount);

            bool madeProgress = true;

            while (currentLandCount < targetLandCount && madeProgress)
            {
                madeProgress = false;

                for (int i = 0; i < candidateCount && currentLandCount < targetLandCount; i++)
                {
                    int index = candidates[i].Index;

                    if (landMask[index] == MaskOn)
                        continue;

                    if (!HasAdjacentLand4(dimensions, landMask, index))
                        continue;

                    landMask[index] = MaskOn;
                    oceanMask[index] = MaskOff;
                    currentLandCount++;
                    madeProgress = true;
                }
            }

            if (currentLandCount != targetLandCount)
                throw new InvalidOperationException("Macro landform area compensation failed to restore the target connected land count.");

            return currentLandCount;
        }

        private static int FloodFillComponent(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<int> componentIds,
            NativeArray<int> queue,
            int startIndex,
            int componentId)
        {
            int head = 0;
            int tail = 0;
            int size = 0;

            queue[tail++] = startIndex;
            componentIds[startIndex] = componentId;

            while (head < tail)
            {
                int index = queue[head++];
                size++;

                dimensions.ToCoordinates(index, out int x, out int y);

                TryEnqueueLand(dimensions, landMask, componentIds, queue, ref tail, x - 1, y, componentId);
                TryEnqueueLand(dimensions, landMask, componentIds, queue, ref tail, x + 1, y, componentId);
                TryEnqueueLand(dimensions, landMask, componentIds, queue, ref tail, x, y - 1, componentId);
                TryEnqueueLand(dimensions, landMask, componentIds, queue, ref tail, x, y + 1, componentId);
            }

            return size;
        }

        private static void TryEnqueueLand(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<int> componentIds,
            NativeArray<int> queue,
            ref int tail,
            int x,
            int y,
            int componentId)
        {
            if (!dimensions.Contains(x, y))
                return;

            int index = y * dimensions.Width + x;

            if (landMask[index] == MaskOff || componentIds[index] != 0)
                return;

            componentIds[index] = componentId;
            queue[tail++] = index;
        }

        private static bool HasAdjacentLand4(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            int index)
        {
            dimensions.ToCoordinates(index, out int x, out int y);

            return IsLand(dimensions, landMask, x - 1, y)
                || IsLand(dimensions, landMask, x + 1, y)
                || IsLand(dimensions, landMask, x, y - 1)
                || IsLand(dimensions, landMask, x, y + 1);
        }

        private static bool IsLand(HeightFieldDimensions dimensions, NativeArray<byte> landMask, int x, int y)
        {
            if (!dimensions.Contains(x, y))
                return false;

            return landMask[y * dimensions.Width + x] == MaskOn;
        }

        private static void BuildHeight(
            MacroLandformSettings settings,
            NativeArray<float> landScores,
            NativeArray<byte> landMask,
            NativeArray<float> height,
            NativeArray<float> mountainInfluence,
            NativeArray<float> basinInfluence,
            NativeArray<float> plainInfluence)
        {
            float minLandScore = float.PositiveInfinity;
            float maxLandScore = float.NegativeInfinity;

            for (int i = 0; i < landScores.Length; i++)
            {
                if (landMask[i] == MaskOff)
                    continue;

                float score = landScores[i];

                if (score < minLandScore)
                    minLandScore = score;

                if (score > maxLandScore)
                    maxLandScore = score;
            }

            float landRange = math.max(0.000001f, maxLandScore - minLandScore);

            for (int i = 0; i < height.Length; i++)
            {
                if (landMask[i] == MaskOff)
                {
                    float waterScore = (!float.IsNaN(landScores[i]) && !float.IsInfinity(landScores[i])) ? math.saturate(0.5f + landScores[i] * 0.15f) : 0f;
                    height[i] = math.saturate(0.03f + waterScore * 0.14f);
                    continue;
                }

                float normalizedScore = math.saturate((landScores[i] - minLandScore) / landRange);
                float terrain =
                    0.28f
                    + normalizedScore * 0.34f
                    + mountainInfluence[i] * settings.MountainHeightContribution
                    - basinInfluence[i] * settings.BasinHeightContribution
                    + plainInfluence[i] * settings.PlainHeightContribution;

                height[i] = math.saturate(terrain);
            }
        }

        private static void BuildDistanceFields(
            HeightFieldDimensions dimensions,
            NativeArray<byte> landMask,
            NativeArray<float> coastDistance,
            NativeArray<float> continentDistance,
            NativeArray<int> queue)
        {
            const float Unvisited = -1f;

            for (int i = 0; i < coastDistance.Length; i++)
            {
                coastDistance[i] = Unvisited;
                continentDistance[i] = 0f;
            }

            int head = 0;
            int tail = 0;

            for (int i = 0; i < landMask.Length; i++)
            {
                if (landMask[i] == MaskOff)
                {
                    coastDistance[i] = 0f;
                    queue[tail++] = i;
                }
            }

            while (head < tail)
            {
                int index = queue[head++];
                float nextDistance = coastDistance[index] + 1f;
                dimensions.ToCoordinates(index, out int x, out int y);

                TryVisitDistance(dimensions, coastDistance, queue, ref tail, x - 1, y, nextDistance);
                TryVisitDistance(dimensions, coastDistance, queue, ref tail, x + 1, y, nextDistance);
                TryVisitDistance(dimensions, coastDistance, queue, ref tail, x, y - 1, nextDistance);
                TryVisitDistance(dimensions, coastDistance, queue, ref tail, x, y + 1, nextDistance);
            }

            float maxDistance = 1f;

            for (int i = 0; i < coastDistance.Length; i++)
            {
                if (coastDistance[i] > maxDistance)
                    maxDistance = coastDistance[i];
            }

            for (int i = 0; i < coastDistance.Length; i++)
                continentDistance[i] = landMask[i] == MaskOn ? coastDistance[i] / maxDistance : 0f;
        }

        private static void TryVisitDistance(
            HeightFieldDimensions dimensions,
            NativeArray<float> distance,
            NativeArray<int> queue,
            ref int tail,
            int x,
            int y,
            float nextDistance)
        {
            if (!dimensions.Contains(x, y))
                return;

            int index = y * dimensions.Width + x;

            if (distance[index] >= 0f)
                return;

            distance[index] = nextDistance;
            queue[tail++] = index;
        }

        private static void BuildSlope(
            HeightFieldDimensions dimensions,
            NativeArray<float> height,
            NativeArray<float> slope)
        {
            for (int y = 0; y < dimensions.Height; y++)
            {
                for (int x = 0; x < dimensions.Width; x++)
                {
                    int index = y * dimensions.Width + x;
                    float center = height[index];
                    float maxDelta = 0f;

                    if (x > 0)
                        maxDelta = math.max(maxDelta, math.abs(center - height[index - 1]));

                    if (x < dimensions.Width - 1)
                        maxDelta = math.max(maxDelta, math.abs(center - height[index + 1]));

                    if (y > 0)
                        maxDelta = math.max(maxDelta, math.abs(center - height[index - dimensions.Width]));

                    if (y < dimensions.Height - 1)
                        maxDelta = math.max(maxDelta, math.abs(center - height[index + dimensions.Width]));

                    slope[index] = maxDelta;
                }
            }
        }

        private static void BuildBuildability(
            MacroLandformSettings settings,
            NativeArray<byte> landMask,
            NativeArray<float> slope,
            NativeArray<float> mountainInfluence,
            NativeArray<float> buildability)
        {
            for (int i = 0; i < buildability.Length; i++)
            {
                if (landMask[i] == MaskOff)
                {
                    buildability[i] = 0f;
                    continue;
                }

                float slopePenalty = math.saturate(slope[i] / settings.FullyUnbuildableSlope);
                float mountainPenalty = mountainInfluence[i] * settings.MountainBuildabilityPenalty;
                buildability[i] = math.saturate(1f - slopePenalty - mountainPenalty);
            }
        }

        private static void GetRoleInfluence(
            RegionPrimaryIdentity identity,
            out float mountain,
            out float basin,
            out float plain)
        {
            switch (identity)
            {
                case RegionPrimaryIdentity.FertileBreadbasket:
                    mountain = 0.05f;
                    basin = 0.55f;
                    plain = 0.90f;
                    break;

                case RegionPrimaryIdentity.MiningUplands:
                    mountain = 0.95f;
                    basin = 0.20f;
                    plain = 0.15f;
                    break;

                case RegionPrimaryIdentity.Timberland:
                    mountain = 0.35f;
                    basin = 0.30f;
                    plain = 0.45f;
                    break;

                case RegionPrimaryIdentity.PortTradeCoast:
                    mountain = 0.10f;
                    basin = 0.35f;
                    plain = 0.70f;
                    break;

                case RegionPrimaryIdentity.RiverlandGrowth:
                    mountain = 0.05f;
                    basin = 0.80f;
                    plain = 0.65f;
                    break;

                case RegionPrimaryIdentity.IndustrialPlateau:
                    mountain = 0.45f;
                    basin = 0.35f;
                    plain = 0.35f;
                    break;

                case RegionPrimaryIdentity.DryExtractorBasin:
                    mountain = 0.25f;
                    basin = 0.75f;
                    plain = 0.35f;
                    break;

                case RegionPrimaryIdentity.BalancedFrontier:
                default:
                    mountain = 0.30f;
                    basin = 0.35f;
                    plain = 0.45f;
                    break;
            }
        }

        private static bool IsHardWaterBorder(MacroLandformSettings settings, int x, int y)
        {
            int border = settings.HardWaterBorderThickness;
            HeightFieldDimensions dimensions = settings.Dimensions;

            return x < border
                || y < border
                || x >= dimensions.Width - border
                || y >= dimensions.Height - border;
        }

        private static float FbmValueNoise(
            float2 p,
            uint seed,
            int octaves,
            float baseWavelength,
            float persistence,
            float lacunarity)
        {
            float amplitude = 1f;
            float frequency = 1f / math.max(0.0001f, baseWavelength);
            float value = 0f;
            float amplitudeSum = 0f;

            for (int octave = 0; octave < octaves; octave++)
            {
                value += ValueNoise2D(p * frequency, unchecked(seed + (uint)octave * 0x9E3779B9u)) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return amplitudeSum <= 0f ? 0f : value / amplitudeSum;
        }

        private static float ValueNoise2D(float2 p, uint seed)
        {
            int x0 = (int)math.floor(p.x);
            int y0 = (int)math.floor(p.y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float tx = p.x - x0;
            float ty = p.y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float v00 = HashToSignedUnit(x0, y0, seed);
            float v10 = HashToSignedUnit(x1, y0, seed);
            float v01 = HashToSignedUnit(x0, y1, seed);
            float v11 = HashToSignedUnit(x1, y1, seed);

            float a = math.lerp(v00, v10, tx);
            float b = math.lerp(v01, v11, tx);
            return math.lerp(a, b, ty);
        }

        private static float HashToSignedUnit(int x, int y, uint seed)
        {
            uint hash = HashGrid(x, y, seed);
            return (hash / 4294967295f) * 2f - 1f;
        }

        private static uint HashGrid(int x, int y, uint seed)
        {
            unchecked
            {
                uint h = seed;
                h ^= (uint)x * 0x8DA6B343u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xD8163841u;
                h *= 0x85EBCA6Bu;
                h ^= h >> 16;
                h *= 0xC2B2AE35u;
                h ^= h >> 13;
                return h;
            }
        }

        private static StableHash128 ComputeArtifactHash(
            MacroLandformSettings settings,
            NativeArray<float> height,
            NativeArray<byte> landMask,
            NativeArray<byte> oceanMask,
            NativeArray<float> coastDistance,
            NativeArray<float> continentDistance,
            NativeArray<float> slope,
            NativeArray<float> buildability,
            NativeArray<float> mountainInfluence,
            NativeArray<float> basinInfluence,
            NativeArray<float> plainInfluence,
            int landCount)
        {
            StableHash128 hash = StableHash128.FromStableName("MacroLandformResult.v0.0.1")
                .Append(settings.Dimensions.Width)
                .Append(settings.Dimensions.Height)
                .Append(settings.Seed.Value)
                .Append(settings.TargetLandPercent)
                .Append(settings.HardWaterBorderThickness)
                .Append(landCount);

            for (int i = 0; i < landMask.Length; i++)
            {
                hash = hash
                    .Append(landMask[i])
                    .Append(oceanMask[i])
                    .Append(height[i])
                    .Append(coastDistance[i])
                    .Append(continentDistance[i])
                    .Append(slope[i])
                    .Append(buildability[i])
                    .Append(mountainInfluence[i])
                    .Append(basinInfluence[i])
                    .Append(plainInfluence[i]);
            }

            return hash;
        }

        private readonly struct LandScoreCandidate : IComparable<LandScoreCandidate>
        {
            public readonly int Index;
            public readonly float Score;

            public LandScoreCandidate(int index, float score)
            {
                Index = index;
                Score = score;
            }

            public int CompareTo(LandScoreCandidate other)
            {
                int scoreCompare = other.Score.CompareTo(Score);

                if (scoreCompare != 0)
                    return scoreCompare;

                return Index.CompareTo(other.Index);
            }
        }
    }
}
