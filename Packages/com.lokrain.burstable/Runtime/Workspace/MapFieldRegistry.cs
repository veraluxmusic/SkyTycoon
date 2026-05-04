using System;
using System.Collections;
using System.Collections.Generic;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Immutable registry of generated map field definitions.
    /// </summary>
    /// <remarks>
    /// A field registry owns validated managed metadata for fields that may exist in a
    /// <see cref="MapWorkspace"/>. It rejects invalid field definitions, duplicate field
    /// identifiers, and duplicate symbolic names before native storage is allocated.
    ///
    /// This type is setup metadata. It is suitable for validation, diagnostics, editor tooling,
    /// previews, export, and tests. It is not intended for Burst jobs or per-tile hot-path
    /// lookups.
    ///
    /// The registry is immutable after construction. The supplied definition list is copied.
    /// </remarks>
    public sealed class MapFieldRegistry : IReadOnlyList<MapFieldDefinition>
    {
        private readonly MapFieldDefinition[] definitions;
        private readonly Dictionary<int, int> indexById;
        private readonly Dictionary<string, int> indexByName;

        /// <summary>
        /// Initializes a new immutable field registry.
        /// </summary>
        /// <param name="definitions">Field definitions to include.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="definitions"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a field definition is invalid, when two definitions use the same field
        /// identifier, or when two definitions use the same symbolic name.
        /// </exception>
        public MapFieldRegistry(IReadOnlyList<MapFieldDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            this.definitions = new MapFieldDefinition[definitions.Count];
            indexById = new Dictionary<int, int>(definitions.Count);
            indexByName = new Dictionary<string, int>(
                definitions.Count,
                StringComparer.Ordinal);

            for (int i = 0; i < definitions.Count; i++)
            {
                MapFieldDefinition definition = definitions[i];
                definition.Validate();

                if (indexById.ContainsKey(definition.Id.Value))
                {
                    throw new ArgumentException(
                        "Map field registry contains duplicate field identifiers.",
                        nameof(definitions));
                }

                if (indexByName.ContainsKey(definition.Name))
                {
                    throw new ArgumentException(
                        "Map field registry contains duplicate field names.",
                        nameof(definitions));
                }

                this.definitions[i] = definition;
                indexById.Add(definition.Id.Value, i);
                indexByName.Add(definition.Name, i);
            }
        }

        /// <summary>
        /// Gets an empty field registry.
        /// </summary>
        public static MapFieldRegistry Empty { get; } = new MapFieldRegistry(
            Array.Empty<MapFieldDefinition>());

        /// <summary>
        /// Gets the number of field definitions in the registry.
        /// </summary>
        public int Count => definitions.Length;

        /// <summary>
        /// Gets the field definition at the specified registry index.
        /// </summary>
        /// <param name="index">Zero-based registry index.</param>
        /// <returns>The field definition at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the registry range.
        /// </exception>
        public MapFieldDefinition this[int index] => GetAt(index);

        /// <summary>
        /// Gets the field definition at the specified registry index.
        /// </summary>
        /// <param name="index">Zero-based registry index.</param>
        /// <returns>The field definition at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the registry range.
        /// </exception>
        public MapFieldDefinition GetAt(int index)
        {
            if ((uint)index >= (uint)definitions.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Registry index is outside the field definition range.");
            }

            return definitions[index];
        }

        /// <summary>
        /// Determines whether the registry contains a field definition with the specified
        /// identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when a matching field definition exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Contains(MapFieldId id)
        {
            return id.IsValid && indexById.ContainsKey(id.Value);
        }

        /// <summary>
        /// Determines whether the registry contains a field definition with the specified
        /// symbolic name.
        /// </summary>
        /// <param name="name">Stable symbolic field name.</param>
        /// <returns>
        /// <see langword="true"/> when a matching field definition exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool ContainsName(string name)
        {
            return MapFieldDefinition.IsValidName(name) && indexByName.ContainsKey(name);
        }

        /// <summary>
        /// Attempts to get a field definition by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <param name="definition">
        /// Matching field definition when found; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching field definition exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGet(
            MapFieldId id,
            out MapFieldDefinition definition)
        {
            if (!id.IsValid || !indexById.TryGetValue(id.Value, out int index))
            {
                definition = default;
                return false;
            }

            definition = definitions[index];
            return true;
        }

        /// <summary>
        /// Gets a field definition by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>The matching field definition.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no field definition exists for <paramref name="id"/>.
        /// </exception>
        public MapFieldDefinition Get(MapFieldId id)
        {
            id.Validate();

            if (!indexById.TryGetValue(id.Value, out int index))
            {
                throw new KeyNotFoundException(
                    "Map field registry does not contain the requested field identifier.");
            }

            return definitions[index];
        }

        /// <summary>
        /// Attempts to get a field definition by symbolic field name.
        /// </summary>
        /// <param name="name">Stable symbolic field name.</param>
        /// <param name="definition">
        /// Matching field definition when found; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching field definition exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetByName(
            string name,
            out MapFieldDefinition definition)
        {
            if (!MapFieldDefinition.IsValidName(name) ||
                !indexByName.TryGetValue(name, out int index))
            {
                definition = default;
                return false;
            }

            definition = definitions[index];
            return true;
        }

        /// <summary>
        /// Gets a field definition by symbolic field name.
        /// </summary>
        /// <param name="name">Stable symbolic field name.</param>
        /// <returns>The matching field definition.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> is not a valid field name.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no field definition exists for <paramref name="name"/>.
        /// </exception>
        public MapFieldDefinition GetByName(string name)
        {
            MapFieldDefinition.ValidateName(name);

            if (!indexByName.TryGetValue(name, out int index))
            {
                throw new KeyNotFoundException(
                    "Map field registry does not contain the requested field name.");
            }

            return definitions[index];
        }

        /// <summary>
        /// Copies all field definitions to a new array.
        /// </summary>
        /// <returns>A new array containing the registry's field definitions.</returns>
        public MapFieldDefinition[] ToArray()
        {
            MapFieldDefinition[] copy = new MapFieldDefinition[definitions.Length];
            Array.Copy(definitions, copy, definitions.Length);
            return copy;
        }

        /// <summary>
        /// Returns an enumerator over the registry definitions.
        /// </summary>
        /// <returns>An enumerator over the registry definitions.</returns>
        public IEnumerator<MapFieldDefinition> GetEnumerator()
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                yield return definitions[i];
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator over the registry definitions.
        /// </summary>
        /// <returns>A non-generic enumerator over the registry definitions.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}