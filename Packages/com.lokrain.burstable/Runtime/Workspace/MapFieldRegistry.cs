using System;
using System.Collections.Generic;

namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Immutable registry of generated map field definitions.
    /// </summary>
    /// <remarks>
    /// A map field registry owns validated metadata for fields that may exist in a
    /// <see cref="MapWorkspace"/>. It prevents duplicate identifiers and duplicate symbolic
    /// names before workspace storage is allocated.
    ///
    /// This type is intentionally managed metadata. It is suitable for setup, validation,
    /// diagnostics, editor tooling, preview configuration, and export. It is not intended to be
    /// captured by Burst-compiled jobs or used for per-tile hot-path lookups.
    ///
    /// Generation paths should resolve field definitions before scheduling work and pass direct
    /// field storage to jobs.
    /// </remarks>
    public sealed class MapFieldRegistry
    {
        private readonly MapFieldDefinition[] definitions;
        private readonly Dictionary<int, int> indexById;
        private readonly Dictionary<string, int> indexByName;

        /// <summary>
        /// Initializes a new immutable field registry from field definitions.
        /// </summary>
        /// <param name="definitions">Field definitions to include in the registry.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="definitions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a definition is invalid, when two definitions use the same field
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
            if (string.IsNullOrWhiteSpace(name) ||
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
        /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no field definition exists for <paramref name="name"/>.
        /// </exception>
        public MapFieldDefinition GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Map field name must not be null, empty, or whitespace.",
                    nameof(name));
            }

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
        /// <remarks>
        /// The returned array may be modified by the caller without affecting the registry.
        /// </remarks>
        public MapFieldDefinition[] ToArray()
        {
            MapFieldDefinition[] copy = new MapFieldDefinition[definitions.Length];
            Array.Copy(definitions, copy, definitions.Length);
            return copy;
        }
    }
}