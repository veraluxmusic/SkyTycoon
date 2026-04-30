#nullable enable

using System;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Regions
{
    /// <summary>
    /// Compact region-to-role assignment suitable for generated results and deterministic hashing.
    /// </summary>
    [Serializable]
    public readonly struct RegionRoleAssignment : IEquatable<RegionRoleAssignment>
    {
        public readonly RegionId RegionId;
        public readonly RegionRoleId RoleId;

        public RegionRoleAssignment(RegionId regionId, RegionRoleId roleId)
        {
            regionId.Validate();
            roleId.Validate();

            RegionId = regionId;
            RoleId = roleId;
        }

        public bool Equals(RegionRoleAssignment other)
        {
            return RegionId == other.RegionId && RoleId == other.RoleId;
        }

        public override bool Equals(object? obj)
        {
            return obj is RegionRoleAssignment other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + RegionId.GetHashCode();
                hash = hash * 31 + RoleId.GetHashCode();
                return hash;
            }
        }
    }
}
