using Lokrain.SkyTycoon.Knowledge.Core;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;
using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents
{
    internal static class BinaryConnectedComponentsKernel
    {
        public static AlgorithmStatus ExecuteFourConnected(
            NativeArray<byte> inputMask,
            int width,
            int height,
            byte foregroundValue,
            NativeArray<int> parents,
            NativeArray<int> areas,
            NativeArray<int> outputLabels,
            out BinaryConnectedComponentsResult result)
        {
            int nextLabel = 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = GridIndexing.ToIndexUnchecked(x, y, width);

                    if (inputMask[index] != foregroundValue)
                    {
                        outputLabels[index] = 0;
                        continue;
                    }

                    int selectedLabel = 0;

                    if (x > 0)
                        selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.LeftUnchecked(index)]);

                    if (y > 0)
                        selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.UpUnchecked(index, width)]);

                    AlgorithmStatus status = AssignLabel(parents, areas, ref nextLabel, selectedLabel, out int assignedLabel);
                    if (status != AlgorithmStatus.Success)
                    {
                        result = BinaryConnectedComponentsResult.Failed(status);
                        return status;
                    }

                    outputLabels[index] = assignedLabel;
                }
            }

            result = FinalizeLabels(parents, areas, outputLabels, width * height, nextLabel);
            return result.Status;
        }

        public static AlgorithmStatus ExecuteEightConnected(
            NativeArray<byte> inputMask,
            int width,
            int height,
            byte foregroundValue,
            NativeArray<int> parents,
            NativeArray<int> areas,
            NativeArray<int> outputLabels,
            out BinaryConnectedComponentsResult result)
        {
            int nextLabel = 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = GridIndexing.ToIndexUnchecked(x, y, width);

                    if (inputMask[index] != foregroundValue)
                    {
                        outputLabels[index] = 0;
                        continue;
                    }

                    int selectedLabel = 0;

                    if (x > 0)
                        selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.LeftUnchecked(index)]);

                    if (y > 0)
                    {
                        if (x > 0)
                            selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.UpLeftUnchecked(index, width)]);

                        selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.UpUnchecked(index, width)]);

                        if (x + 1 < width)
                            selectedLabel = MergeNeighbor(parents, selectedLabel, outputLabels[GridIndexing.UpRightUnchecked(index, width)]);
                    }

                    AlgorithmStatus status = AssignLabel(parents, areas, ref nextLabel, selectedLabel, out int assignedLabel);
                    if (status != AlgorithmStatus.Success)
                    {
                        result = BinaryConnectedComponentsResult.Failed(status);
                        return status;
                    }

                    outputLabels[index] = assignedLabel;
                }
            }

            result = FinalizeLabels(parents, areas, outputLabels, width * height, nextLabel);
            return result.Status;
        }

        private static AlgorithmStatus AssignLabel(
            NativeArray<int> parents,
            NativeArray<int> areas,
            ref int nextLabel,
            int selectedLabel,
            out int assignedLabel)
        {
            if (selectedLabel != 0)
            {
                assignedLabel = Find(parents, selectedLabel);
                return AlgorithmStatus.Success;
            }

            if (nextLabel >= parents.Length)
            {
                assignedLabel = 0;
                return AlgorithmStatus.ComponentCapacityExceeded;
            }

            assignedLabel = nextLabel;
            parents[assignedLabel] = assignedLabel;
            areas[assignedLabel] = 0;
            nextLabel++;
            return AlgorithmStatus.Success;
        }

        private static int MergeNeighbor(NativeArray<int> parents, int selectedLabel, int neighborLabel)
        {
            if (neighborLabel == 0)
                return selectedLabel;

            int neighborRoot = Find(parents, neighborLabel);
            if (selectedLabel == 0)
                return neighborRoot;

            return Union(parents, selectedLabel, neighborRoot);
        }

        private static int Union(NativeArray<int> parents, int leftLabel, int rightLabel)
        {
            int leftRoot = Find(parents, leftLabel);
            int rightRoot = Find(parents, rightLabel);

            if (leftRoot == rightRoot)
                return leftRoot;

            int lowerRoot = leftRoot < rightRoot ? leftRoot : rightRoot;
            int higherRoot = leftRoot < rightRoot ? rightRoot : leftRoot;
            parents[higherRoot] = lowerRoot;
            return lowerRoot;
        }

        private static int Find(NativeArray<int> parents, int label)
        {
            int root = label;
            while (parents[root] != root)
                root = parents[root];

            while (parents[label] != label)
            {
                int parent = parents[label];
                parents[label] = root;
                label = parent;
            }

            return root;
        }

        private static BinaryConnectedComponentsResult FinalizeLabels(
            NativeArray<int> parents,
            NativeArray<int> areas,
            NativeArray<int> outputLabels,
            int cellCount,
            int nextLabel)
        {
            int foregroundCellCount = 0;

            for (int i = 0; i < cellCount; i++)
            {
                int label = outputLabels[i];
                if (label == 0)
                    continue;

                int root = Find(parents, label);
                outputLabels[i] = root;
                areas[root] = areas[root] + 1;
                foregroundCellCount++;
            }

            int componentCount = 0;
            int largestComponentLabel = 0;
            int largestComponentArea = 0;

            for (int label = 1; label < nextLabel; label++)
            {
                if (parents[label] != label)
                    continue;

                int area = areas[label];
                if (area <= 0)
                    continue;

                componentCount++;

                if (area > largestComponentArea ||
                    (area == largestComponentArea && (largestComponentLabel == 0 || label < largestComponentLabel)))
                {
                    largestComponentLabel = label;
                    largestComponentArea = area;
                }
            }

            return new BinaryConnectedComponentsResult(
                AlgorithmStatus.Success,
                componentCount,
                largestComponentLabel,
                largestComponentArea,
                foregroundCellCount);
        }
    }
}
