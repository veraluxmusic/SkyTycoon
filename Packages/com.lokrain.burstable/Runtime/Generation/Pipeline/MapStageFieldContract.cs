using System;
using System.Collections;
using System.Collections.Generic;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Immutable field contract declared by a generation stage.
    /// </summary>
    /// <remarks>
    /// A stage field contract describes which workspace fields a stage reads and writes. This is
    /// pipeline metadata, not workspace storage metadata.
    ///
    /// The contract is useful for stage validation, automatic workspace registry construction,
    /// pipeline diagnostics, editor preview setup, documentation, and tests.
    ///
    /// The contract is immutable after construction. The supplied requirement list is copied.
    /// </remarks>
    public sealed class MapStageFieldContract : IReadOnlyList<MapStageFieldRequirement>
    {
        private readonly MapStageFieldRequirement[] requirements;
        private readonly Dictionary<int, int> indexById;
        private readonly Dictionary<string, int> indexByName;

        /// <summary>
        /// Initializes a new immutable stage field contract.
        /// </summary>
        /// <param name="requirements">Field requirements declared by a stage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="requirements"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a requirement is invalid, when the contract contains duplicate field
        /// identifiers, or when the contract contains duplicate field names.
        /// </exception>
        public MapStageFieldContract(IReadOnlyList<MapStageFieldRequirement> requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            this.requirements = new MapStageFieldRequirement[requirements.Count];
            indexById = new Dictionary<int, int>(requirements.Count);
            indexByName = new Dictionary<string, int>(
                requirements.Count,
                StringComparer.Ordinal);

            for (int i = 0; i < requirements.Count; i++)
            {
                MapStageFieldRequirement requirement = requirements[i];
                requirement.Validate();

                if (indexById.ContainsKey(requirement.Definition.Id.Value))
                {
                    throw new ArgumentException(
                        "Stage field contract contains duplicate field identifiers.",
                        nameof(requirements));
                }

                if (indexByName.ContainsKey(requirement.Definition.Name))
                {
                    throw new ArgumentException(
                        "Stage field contract contains duplicate field names.",
                        nameof(requirements));
                }

                this.requirements[i] = requirement;
                indexById.Add(requirement.Definition.Id.Value, i);
                indexByName.Add(requirement.Definition.Name, i);
            }
        }

        /// <summary>
        /// Gets an empty stage field contract.
        /// </summary>
        public static MapStageFieldContract Empty { get; } = new MapStageFieldContract(
            Array.Empty<MapStageFieldRequirement>());

        /// <summary>
        /// Gets the number of requirements in the contract.
        /// </summary>
        public int Count => requirements.Length;

        /// <summary>
        /// Gets the requirement at the specified contract index.
        /// </summary>
        /// <param name="index">Zero-based requirement index.</param>
        /// <returns>The requirement at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the contract range.
        /// </exception>
        public MapStageFieldRequirement this[int index] => GetAt(index);

        /// <summary>
        /// Gets the requirement at the specified contract index.
        /// </summary>
        /// <param name="index">Zero-based requirement index.</param>
        /// <returns>The requirement at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the contract range.
        /// </exception>
        public MapStageFieldRequirement GetAt(int index)
        {
            if ((uint)index >= (uint)requirements.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Contract index is outside the field requirement range.");
            }

            return requirements[index];
        }

        /// <summary>
        /// Determines whether the contract contains a requirement for the specified field
        /// identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>
        /// <see langword="true"/> when the contract contains a requirement for the field;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(MapFieldId id)
        {
            return id.IsValid && indexById.ContainsKey(id.Value);
        }

        /// <summary>
        /// Attempts to get a requirement by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <param name="requirement">
        /// Matching requirement when found; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a matching requirement exists; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGet(
            MapFieldId id,
            out MapStageFieldRequirement requirement)
        {
            if (!id.IsValid || !indexById.TryGetValue(id.Value, out int index))
            {
                requirement = default;
                return false;
            }

            requirement = requirements[index];
            return true;
        }

        /// <summary>
        /// Gets a requirement by field identifier.
        /// </summary>
        /// <param name="id">Field identifier.</param>
        /// <returns>The matching requirement.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the contract does not contain a requirement for
        /// <paramref name="id"/>.
        /// </exception>
        public MapStageFieldRequirement Get(MapFieldId id)
        {
            id.Validate();

            if (!indexById.TryGetValue(id.Value, out int index))
            {
                throw new KeyNotFoundException(
                    "Stage field contract does not contain the requested field identifier.");
            }

            return requirements[index];
        }

        /// <summary>
        /// Validates that a workspace satisfies this stage field contract.
        /// </summary>
        /// <param name="workspace">Workspace to validate.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="workspace"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when <paramref name="workspace"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a required field is missing, or when a present field has metadata that
        /// does not match the contract.
        /// </exception>
        /// <remarks>
        /// Optional fields may be absent. If an optional field is present, its metadata must
        /// still match the contract.
        /// </remarks>
        public void ValidateWorkspace(MapWorkspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            for (int i = 0; i < requirements.Length; i++)
            {
                MapStageFieldRequirement requirement = requirements[i];

                bool exists = workspace.FieldRegistry.TryGet(
                    requirement.Definition.Id,
                    out MapFieldDefinition actualDefinition);

                if (!exists)
                {
                    if (requirement.IsRequired)
                    {
                        throw new ArgumentException(
                            "Workspace does not contain a required stage field.",
                            nameof(workspace));
                    }

                    continue;
                }

                if (actualDefinition != requirement.Definition)
                {
                    throw new ArgumentException(
                        "Workspace field definition does not match the stage field contract.",
                        nameof(workspace));
                }
            }
        }

        /// <summary>
        /// Creates a field registry containing all fields declared by this contract.
        /// </summary>
        /// <returns>A field registry containing this contract's field definitions.</returns>
        /// <remarks>
        /// This is useful for tests, simple pipelines, and editor preview setup. Larger
        /// pipelines should usually merge multiple stage contracts into one registry through a
        /// dedicated pipeline builder.
        /// </remarks>
        public MapFieldRegistry ToFieldRegistry()
        {
            MapFieldDefinition[] definitions = new MapFieldDefinition[requirements.Length];

            for (int i = 0; i < requirements.Length; i++)
            {
                definitions[i] = requirements[i].Definition;
            }

            return new MapFieldRegistry(definitions);
        }

        /// <summary>
        /// Copies all requirements to a new array.
        /// </summary>
        /// <returns>A new array containing all requirements.</returns>
        public MapStageFieldRequirement[] ToArray()
        {
            MapStageFieldRequirement[] copy = new MapStageFieldRequirement[requirements.Length];
            Array.Copy(requirements, copy, requirements.Length);
            return copy;
        }

        /// <summary>
        /// Returns an enumerator over the contract requirements.
        /// </summary>
        /// <returns>An enumerator over the contract requirements.</returns>
        public IEnumerator<MapStageFieldRequirement> GetEnumerator()
        {
            for (int i = 0; i < requirements.Length; i++)
            {
                yield return requirements[i];
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator over the contract requirements.
        /// </summary>
        /// <returns>A non-generic enumerator over the contract requirements.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}