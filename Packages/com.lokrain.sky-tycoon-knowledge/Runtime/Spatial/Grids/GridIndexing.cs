namespace Lokrain.SkyTycoon.Knowledge.Spatial.Grids
{
    /// <summary>
    /// Unchecked row-major grid indexing helpers for algorithm kernels.
    /// </summary>
    public static class GridIndexing
    {
        public static int ToIndexUnchecked(int x, int y, int width)
        {
            return y * width + x;
        }

        public static int LeftUnchecked(int index)
        {
            return index - 1;
        }

        public static int UpUnchecked(int index, int width)
        {
            return index - width;
        }

        public static int UpLeftUnchecked(int index, int width)
        {
            return index - width - 1;
        }

        public static int UpRightUnchecked(int index, int width)
        {
            return index - width + 1;
        }
    }
}
