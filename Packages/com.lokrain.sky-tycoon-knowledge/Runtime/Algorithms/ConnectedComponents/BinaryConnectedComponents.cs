using System;
using Lokrain.SkyTycoon.Knowledge.Core;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;
using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents
{
    /// <summary>
    /// Safe facade for deterministic binary connected-component labeling.
    /// </summary>
    public static class BinaryConnectedComponents
    {
        public static AlgorithmStatus Execute(
            NativeArray<byte> inputMask,
            GridDimensions dimensions,
            BinaryConnectedComponentsSettings settings,
            BinaryConnectedComponentsWorkspace workspace,
            NativeArray<int> outputLabels,
            out BinaryConnectedComponentsResult result)
        {
            AlgorithmStatus validationStatus = ValidateInputs(inputMask, dimensions, settings, workspace, outputLabels);
            if (validationStatus != AlgorithmStatus.Success)
            {
                result = BinaryConnectedComponentsResult.Failed(validationStatus);
                return validationStatus;
            }

            int width = dimensions.Width;
            int height = dimensions.Height;

            switch (settings.Connectivity)
            {
                case GridConnectivity.Four:
                    return BinaryConnectedComponentsKernel.ExecuteFourConnected(
                        inputMask,
                        width,
                        height,
                        settings.ForegroundValue,
                        workspace.Parents,
                        workspace.Areas,
                        outputLabels,
                        out result);

                case GridConnectivity.Eight:
                    return BinaryConnectedComponentsKernel.ExecuteEightConnected(
                        inputMask,
                        width,
                        height,
                        settings.ForegroundValue,
                        workspace.Parents,
                        workspace.Areas,
                        outputLabels,
                        out result);

                default:
                    result = BinaryConnectedComponentsResult.Failed(AlgorithmStatus.UnsupportedConnectivity);
                    return AlgorithmStatus.UnsupportedConnectivity;
            }
        }

        public static BinaryConnectedComponentsResult ExecuteChecked(
            NativeArray<byte> inputMask,
            GridDimensions dimensions,
            BinaryConnectedComponentsSettings settings,
            BinaryConnectedComponentsWorkspace workspace,
            NativeArray<int> outputLabels)
        {
            AlgorithmStatus status = Execute(inputMask, dimensions, settings, workspace, outputLabels, out BinaryConnectedComponentsResult result);
            if (status != AlgorithmStatus.Success)
                throw new InvalidOperationException("Binary connected components failed with status " + status + ".");

            return result;
        }

        private static AlgorithmStatus ValidateInputs(
            NativeArray<byte> inputMask,
            GridDimensions dimensions,
            BinaryConnectedComponentsSettings settings,
            BinaryConnectedComponentsWorkspace workspace,
            NativeArray<int> outputLabels)
        {
            if (!dimensions.IsValid)
                return AlgorithmStatus.InvalidDimensions;

            AlgorithmStatus settingsStatus = settings.Validate();
            if (settingsStatus != AlgorithmStatus.Success)
                return settingsStatus;

            int cellCount = dimensions.CellCount;

            if (!inputMask.IsCreated || inputMask.Length < cellCount)
                return AlgorithmStatus.InputTooSmall;

            if (!outputLabels.IsCreated || outputLabels.Length < cellCount)
                return AlgorithmStatus.OutputTooSmall;

            if (workspace == null || !workspace.IsCreated || workspace.LabelCapacity < cellCount)
                return AlgorithmStatus.WorkspaceTooSmall;

            return AlgorithmStatus.Success;
        }
    }
}
