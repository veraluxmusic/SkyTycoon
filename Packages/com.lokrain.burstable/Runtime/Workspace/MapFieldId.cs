using System;
using System.Globalization;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Stable identifier for a generated map field.
    /// </summary>
    /// <remarks>
    /// Map fields are generated data arrays owned by a map workspace. This value identifies
    /// those fields without requiring strings, reflection, managed objects, or allocation in
    /// generation paths.
    ///
    /// Field identifiers are value-based and deterministic. They do not describe the field's
    /// element type, length, ownership, or display name. Those concerns belong to field
    /// definitions and the field registry.
    ///
    /// The value zero is reserved for <see cref="Invalid"/>. Valid field identifiers must use
    /// positive values.
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
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// an identifier as a field definition key or workspace lookup key.
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
        public static MapFieldId Invalid => new(InvalidValue);

        /// <summary>
        /// Gets a value indicating whether this identifier is valid.
        /// </summary>
        public bool IsValid => Value >= MinimumValidValue;

        /// <summary>
        /// Validates that this identifier can be used as a field definition or workspace key.
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
        /// <see langword="true"/> when both identifiers are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapFieldId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MapFieldId other && Equals(other);
        }

        /// <inheritdoc />
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
        public static bool operator ==(MapFieldId left, MapFieldId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two field identifiers are not equal.
        /// </summary>
        public static bool operator !=(MapFieldId left, MapFieldId right)
        {
            return !left.Equals(right);
        }
    }
}