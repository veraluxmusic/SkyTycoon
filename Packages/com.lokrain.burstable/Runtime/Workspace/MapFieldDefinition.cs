using System;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Immutable metadata definition for a generated map field.
    /// </summary>
    /// <remarks>
    /// A map field definition describes the identity, stored value type, and stable symbolic
    /// name of a generated field.
    ///
    /// This type does not allocate field storage, own generated data, validate field lengths,
    /// schedule jobs, or define terrain semantics. Runtime field memory belongs to the map
    /// workspace. Domain meaning belongs to the generation stage or consumer that owns the
    /// field contract.
    ///
    /// Field names are metadata for diagnostics, registries, previews, export, and tooling.
    /// Generation paths should use <see cref="MapFieldId"/> rather than string lookups.
    /// </remarks>
    public readonly struct MapFieldDefinition : IEquatable<MapFieldDefinition>
    {
        /// <summary>
        /// Creates immutable metadata for a generated map field.
        /// </summary>
        /// <param name="id">Stable field identifier.</param>
        /// <param name="valueType">Stored value type of the field.</param>
        /// <param name="name">Stable symbolic field name.</param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before adding
        /// the definition to a registry or using it to allocate workspace storage.
        /// </remarks>
        public MapFieldDefinition(
            MapFieldId id,
            MapFieldValueType valueType,
            string name)
        {
            Id = id;
            ValueType = valueType;
            Name = name;
        }

        /// <summary>
        /// Gets the stable field identifier.
        /// </summary>
        public MapFieldId Id { get; }

        /// <summary>
        /// Gets the stored value type of the field.
        /// </summary>
        public MapFieldValueType ValueType { get; }

        /// <summary>
        /// Gets the stable symbolic field name.
        /// </summary>
        /// <remarks>
        /// This name is intended for diagnostics, registries, previews, export, and tooling.
        /// It should be stable and should not be localized.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Validates that this definition can be used by a map field registry or workspace.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="Id"/> is not a valid field identifier.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="ValueType"/> is unsupported, or when <see cref="Name"/> is
        /// null, empty, or whitespace.
        /// </exception>
        public void Validate()
        {
            Id.Validate();

            if (!IsSupportedValueType(ValueType))
            {
                throw new ArgumentException(
                    "Unsupported map field value type.",
                    nameof(ValueType));
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentException(
                    "Map field name must not be null, empty, or whitespace.",
                    nameof(Name));
            }
        }

        /// <summary>
        /// Determines whether this definition is equal to another definition.
        /// </summary>
        /// <param name="other">Other field definition.</param>
        /// <returns>
        /// <see langword="true"/> when both definitions are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapFieldDefinition other)
        {
            return Id == other.Id &&
                   ValueType == other.ValueType &&
                   string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapFieldDefinition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ValueType;
                hashCode = (hashCode * 397) ^ GetDeterministicOrdinalHashCode(Name);
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two field definitions are equal.
        /// </summary>
        public static bool operator ==(MapFieldDefinition left, MapFieldDefinition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two field definitions are not equal.
        /// </summary>
        public static bool operator !=(MapFieldDefinition left, MapFieldDefinition right)
        {
            return !left.Equals(right);
        }

        private static bool IsSupportedValueType(MapFieldValueType valueType)
        {
            return valueType == MapFieldValueType.Int32 ||
                   valueType == MapFieldValueType.UInt8;
        }

        private static int GetDeterministicOrdinalHashCode(string value)
        {
            if (value == null)
            {
                return 0;
            }

            unchecked
            {
                const uint offsetBasis = 2166136261u;
                const uint prime = 16777619u;

                uint hash = offsetBasis;

                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return (int)hash;
            }
        }
    }
}