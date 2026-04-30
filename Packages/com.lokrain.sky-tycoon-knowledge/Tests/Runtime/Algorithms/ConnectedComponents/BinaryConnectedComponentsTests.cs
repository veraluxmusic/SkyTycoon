using Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents;
using Lokrain.SkyTycoon.Knowledge.Core;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;
using NUnit.Framework;
using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Tests.Algorithms.ConnectedComponents
{
    public sealed class BinaryConnectedComponentsTests
    {
        [Test]
        public void FourConnected_DoesNotJoinDiagonalCells()
        {
            byte[] mask =
            {
                1, 0,
                0, 1
            };

            BinaryConnectedComponentsResult result = Execute(mask, 2, 2, GridConnectivity.Four, out int[] labels);

            Assert.AreEqual(AlgorithmStatus.Success, result.Status);
            Assert.AreEqual(2, result.ComponentCount);
            Assert.AreEqual(2, result.ForegroundCellCount);
            Assert.AreNotEqual(labels[0], labels[3]);
            Assert.Greater(labels[0], 0);
            Assert.Greater(labels[3], 0);
        }

        [Test]
        public void EightConnected_JoinsDiagonalCells()
        {
            byte[] mask =
            {
                1, 0,
                0, 1
            };

            BinaryConnectedComponentsResult result = Execute(mask, 2, 2, GridConnectivity.Eight, out int[] labels);

            Assert.AreEqual(AlgorithmStatus.Success, result.Status);
            Assert.AreEqual(1, result.ComponentCount);
            Assert.AreEqual(2, result.LargestComponentArea);
            Assert.AreEqual(labels[0], labels[3]);
        }

        [Test]
        public void EmptyMask_WritesZeroLabels()
        {
            byte[] mask =
            {
                0, 0, 0,
                0, 0, 0
            };

            BinaryConnectedComponentsResult result = Execute(mask, 3, 2, GridConnectivity.Four, out int[] labels);

            Assert.AreEqual(AlgorithmStatus.Success, result.Status);
            Assert.AreEqual(0, result.ComponentCount);
            Assert.AreEqual(0, result.ForegroundCellCount);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, 0 }, labels);
        }

        [Test]
        public void Execute_ReturnsOutputTooSmall()
        {
            GridDimensions dimensions = new GridDimensions(2, 2);
            NativeArray<byte> input = new NativeArray<byte>(new byte[] { 1, 1, 1, 1 }, Allocator.Temp);
            NativeArray<int> output = new NativeArray<int>(3, Allocator.Temp);
            BinaryConnectedComponentsWorkspace workspace = new BinaryConnectedComponentsWorkspace(dimensions, Allocator.Temp);

            try
            {
                AlgorithmStatus status = BinaryConnectedComponents.Execute(
                    input,
                    dimensions,
                    BinaryConnectedComponentsSettings.FourConnected(),
                    workspace,
                    output,
                    out BinaryConnectedComponentsResult result);

                Assert.AreEqual(AlgorithmStatus.OutputTooSmall, status);
                Assert.AreEqual(AlgorithmStatus.OutputTooSmall, result.Status);
            }
            finally
            {
                workspace.Dispose();
                output.Dispose();
                input.Dispose();
            }
        }

        [Test]
        public void Execute_ReturnsWorkspaceTooSmall()
        {
            GridDimensions dimensions = new GridDimensions(2, 2);
            NativeArray<byte> input = new NativeArray<byte>(new byte[] { 1, 1, 1, 1 }, Allocator.Temp);
            NativeArray<int> output = new NativeArray<int>(4, Allocator.Temp);
            BinaryConnectedComponentsWorkspace workspace = new BinaryConnectedComponentsWorkspace(GridDimensions.Square(1), Allocator.Temp);

            try
            {
                AlgorithmStatus status = BinaryConnectedComponents.Execute(
                    input,
                    dimensions,
                    BinaryConnectedComponentsSettings.FourConnected(),
                    workspace,
                    output,
                    out BinaryConnectedComponentsResult result);

                Assert.AreEqual(AlgorithmStatus.WorkspaceTooSmall, status);
                Assert.AreEqual(AlgorithmStatus.WorkspaceTooSmall, result.Status);
            }
            finally
            {
                workspace.Dispose();
                output.Dispose();
                input.Dispose();
            }
        }

        private static BinaryConnectedComponentsResult Execute(
            byte[] mask,
            int width,
            int height,
            GridConnectivity connectivity,
            out int[] labels)
        {
            GridDimensions dimensions = new GridDimensions(width, height);
            NativeArray<byte> input = new NativeArray<byte>(mask, Allocator.Temp);
            NativeArray<int> output = new NativeArray<int>(mask.Length, Allocator.Temp);
            BinaryConnectedComponentsWorkspace workspace = new BinaryConnectedComponentsWorkspace(dimensions, Allocator.Temp);

            try
            {
                AlgorithmStatus status = BinaryConnectedComponents.Execute(
                    input,
                    dimensions,
                    new BinaryConnectedComponentsSettings(connectivity, 1),
                    workspace,
                    output,
                    out BinaryConnectedComponentsResult result);

                Assert.AreEqual(AlgorithmStatus.Success, status);

                labels = new int[output.Length];
                for (int i = 0; i < output.Length; i++)
                    labels[i] = output[i];

                return result;
            }
            finally
            {
                workspace.Dispose();
                output.Dispose();
                input.Dispose();
            }
        }
    }
}
