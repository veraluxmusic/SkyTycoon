using System;
using Lokrain.SkyTycoon.Knowledge.Spatial.Grids;
using Unity.Collections;

namespace Lokrain.SkyTycoon.Knowledge.Algorithms.ConnectedComponents
{
    /// <summary>
    /// Caller-owned temporary storage for binary connected-component labeling.
    /// </summary>
    public sealed class BinaryConnectedComponentsWorkspace : IDisposable
    {
        public BinaryConnectedComponentsWorkspace(GridDimensions dimensions, Allocator allocator)
        {
            dimensions.Validate();

            int cellCount = dimensions.CellCount;
            if (cellCount == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(dimensions), "Workspace capacity exceeds supported array indexing.");

            int labelStorageLength = cellCount + 1;
            Parents = new NativeArray<int>(labelStorageLength, allocator, NativeArrayOptions.UninitializedMemory);
            Areas = new NativeArray<int>(labelStorageLength, allocator, NativeArrayOptions.UninitializedMemory);
        }

        internal NativeArray<int> Parents { get; private set; }

        internal NativeArray<int> Areas { get; private set; }

        public bool IsCreated => Parents.IsCreated && Areas.IsCreated;

        public int LabelCapacity => IsCreated ? Parents.Length - 1 : 0;

        public void Dispose()
        {
            if (Parents.IsCreated)
                Parents.Dispose();

            if (Areas.IsCreated)
                Areas.Dispose();
        }
    }
}
