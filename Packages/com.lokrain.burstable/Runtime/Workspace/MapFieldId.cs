using System;
using System.Globalization;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Stable identifier for a generated map field.
    /// </summary>
    /// <remarks>
    /// Map field identifiers are deterministic value keys used to identify generated map fields
    /// without string lookup, reflection, managed object identity, or allocation in generation
    /// paths.
    ///
    /// A field identifier does not describe the field's value type, symbolic name, length,
    /// allocator, owner, or semantic meaning. Those concerns belong to
    /// <see cref="MapFieldDefinition"/>, <see cref="MapFieldRegistry"/>, and
    /// <see cref="MapWorkspace"/>.
    ///
    /// The value zero is reserved for <see cref="Invalid"/>. Public field identifiers are
    /// package data-contract values. Changing an existing public field identifier should be
    /// treated as a breaking change.
    /// </remarks>
    public readonly struct MapFieldId : IEquatable<MapFieldId>
    {
        /// <summary>
        /// Raw identifier value reserved for invalid or unassigned fields.
        /// </summary>
        public const int InvalidValue = 0;

        /// <summary>
        /// Minimum raw identifier value for a valid field.
        /// </summary>
        public const int MinimumValidValue = 1;

        /// <summary>
        /// Creates a map field identifier.
        /// </summary>
        /// <param name="value">Raw field identifier value.</param>
        /// <remarks>
        /// The constructor stores values as provided so default and invalid values can exist as
        /// data. Call <see cref="Validate"/> at API boundaries where a valid identifier is
        /// required.
        /// </remarks>
        public MapFieldId(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the raw field identifier value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Gets the invalid or unassigned field identifier.
        /// </summary>
        public static MapFieldId Invalid => new MapFieldId(InvalidValue);

        /// <summary>
        /// Gets a value indicating whether this identifier is valid.
        /// </summary>
        public bool IsValid => Value >= MinimumValidValue;

        /// <summary>
        /// Validates that this identifier can be used as a registry or workspace key.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <see cref="Value"/> is not a positive field identifier.
        /// </exception>
        public void Validate()
        {
            if (!IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Value),
                    Value,
                    "Map field identifier value must be positive.");
            }
        }

        /// <summary>
        /// Determines whether this identifier is equal to another identifier.
        /// </summary>
        /// <param name="other">Other field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when both identifiers contain the same raw value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapFieldId other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Determines whether this identifier is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this identifier.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is a
        /// <see cref="MapFieldId"/> value equal to this identifier; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is MapFieldId other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this identifier.
        /// </summary>
        /// <returns>The raw field identifier value.</returns>
        public override int GetHashCode()
        {
            return Value;
        }

        /// <summary>
        /// Returns the raw field identifier value as an invariant-culture string.
        /// </summary>
        /// <returns>The raw field identifier value as a string.</returns>
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether two field identifiers are equal.
        /// </summary>
        /// <param name="left">Left field identifier.</param>
        /// <param name="right">Right field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when both identifiers contain the same raw value; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(MapFieldId left, MapFieldId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two field identifiers are not equal.
        /// </summary>
        /// <param name="left">Left field identifier.</param>
        /// <param name="right">Right field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when the identifiers contain different raw values; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(MapFieldId left, MapFieldId right)
        {
            return !left.Equals(right);
        }
    }
}