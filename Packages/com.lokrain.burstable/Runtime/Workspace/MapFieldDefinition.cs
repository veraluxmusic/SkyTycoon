using System;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Immutable metadata definition for a generated map field.
    /// </summary>
    /// <remarks>
    /// A field definition describes a field's stable identifier, stored value type, and stable
    /// symbolic name.
    ///
    /// This type does not allocate storage, own generated data, schedule jobs, validate field
    /// length, define input/output stage usage, or own terrain semantics. Runtime storage
    /// belongs to <see cref="MapWorkspace"/>. Stage usage belongs to generation pipeline field
    /// contracts. Domain meaning belongs to the feature or stage that owns the field contract.
    ///
    /// Field names are stable package identifiers for diagnostics, registry lookup, previews,
    /// export, tooling, and documentation. Hot generation paths should use
    /// <see cref="MapFieldId"/>.
    /// </remarks>
    public readonly struct MapFieldDefinition : IEquatable<MapFieldDefinition>
    {
        /// <summary>
        /// Maximum supported symbolic field name length.
        /// </summary>
        public const int MaximumNameLength = 128;

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
        public string Name { get; }

        /// <summary>
        /// Validates that this definition can be used by a field registry or workspace.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="Id"/> is not valid.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="ValueType"/> is unsupported or <see cref="Name"/> is invalid.
        /// </exception>
        public void Validate()
        {
            Id.Validate();
            ValidateValueType(ValueType);
            ValidateName(Name);
        }

        /// <summary>
        /// Determines whether the supplied value type is supported by the workspace layer.
        /// </summary>
        /// <param name="valueType">Value type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the value type is supported; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool IsSupportedValueType(MapFieldValueType valueType)
        {
            return valueType == MapFieldValueType.Int32 ||
                   valueType == MapFieldValueType.UInt8;
        }

        /// <summary>
        /// Determines whether a symbolic field name is valid.
        /// </summary>
        /// <param name="name">Field name to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="name"/> is a valid field name;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Valid names must be non-null, non-empty, no longer than
        /// <see cref="MaximumNameLength"/>, start with a lowercase ASCII letter, end with a
        /// lowercase ASCII letter or digit, and contain only lowercase ASCII letters, digits,
        /// '.', '-', or '_'.
        /// </remarks>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > MaximumNameLength)
            {
                return false;
            }

            if (!IsLowercaseAsciiLetter(name[0]))
            {
                return false;
            }

            char lastCharacter = name[name.Length - 1];

            if (!IsLowercaseAsciiLetter(lastCharacter) && !IsAsciiDigit(lastCharacter))
            {
                return false;
            }

            for (int i = 1; i < name.Length - 1; i++)
            {
                char character = name[i];

                if (!IsLowercaseAsciiLetter(character) &&
                    !IsAsciiDigit(character) &&
                    character != '.' &&
                    character != '-' &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates a symbolic field name.
        /// </summary>
        /// <param name="name">Field name to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> is not a valid field name.
        /// </exception>
        public static void ValidateName(string name)
        {
            if (!IsValidName(name))
            {
                throw new ArgumentException(
                    "Map field name must be a lowercase ASCII identifier. It must start with a lowercase letter, end with a lowercase letter or digit, contain only lowercase letters, digits, '.', '-', or '_', and be at most 128 characters long.",
                    nameof(name));
            }
        }

        /// <summary>
        /// Determines whether this definition is equal to another definition.
        /// </summary>
        /// <param name="other">Other field definition.</param>
        /// <returns>
        /// <see langword="true"/> when both definitions contain the same identifier, value
        /// type, and symbolic name; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(MapFieldDefinition other)
        {
            return Id == other.Id &&
                   ValueType == other.ValueType &&
                   string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether this definition is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this definition.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is a
        /// <see cref="MapFieldDefinition"/> value equal to this definition; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is MapFieldDefinition other && Equals(other);
        }

        /// <summary>
        /// Gets a deterministic hash code for this definition.
        /// </summary>
        /// <returns>A deterministic hash code derived from identifier, value type, and name.</returns>
        /// <remarks>
        /// This method intentionally avoids <see cref="string.GetHashCode()"/> because string
        /// hash behavior can vary across runtimes.
        /// </remarks>
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
        /// <param name="left">Left field definition.</param>
        /// <param name="right">Right field definition.</param>
        /// <returns>
        /// <see langword="true"/> when both definitions are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(MapFieldDefinition left, MapFieldDefinition right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two field definitions are not equal.
        /// </summary>
        /// <param name="left">Left field definition.</param>
        /// <param name="right">Right field definition.</param>
        /// <returns>
        /// <see langword="true"/> when the definitions are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(MapFieldDefinition left, MapFieldDefinition right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Validates that a field value type is supported.
        /// </summary>
        /// <param name="valueType">Value type to validate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="valueType"/> is unsupported.
        /// </exception>
        private static void ValidateValueType(MapFieldValueType valueType)
        {
            if (!IsSupportedValueType(valueType))
            {
                throw new ArgumentException(
                    "Unsupported map field value type.",
                    nameof(valueType));
            }
        }

        /// <summary>
        /// Determines whether a character is a lowercase ASCII letter.
        /// </summary>
        /// <param name="character">Character to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the character is between 'a' and 'z'; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsLowercaseAsciiLetter(char character)
        {
            return character >= 'a' && character <= 'z';
        }

        /// <summary>
        /// Determines whether a character is an ASCII digit.
        /// </summary>
        /// <param name="character">Character to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the character is between '0' and '9'; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsAsciiDigit(char character)
        {
            return character >= '0' && character <= '9';
        }

        /// <summary>
        /// Gets a deterministic ordinal hash code for a string.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>A deterministic ordinal hash code.</returns>
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