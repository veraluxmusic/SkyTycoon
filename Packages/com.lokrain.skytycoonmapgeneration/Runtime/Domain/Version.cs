namespace Lokrain.SkyTycoon.MapGeneration.Domain
{
    public readonly struct Version
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}