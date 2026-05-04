namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Identifies the stored value type of a generated map field.
    /// </summary>
    /// <remarks>
    /// Map field value types are metadata used by field definitions, registries, workspaces,
    /// validation, previews, and exporters.
    ///
    /// This value does not allocate storage, convert data, own field memory, or describe a
    /// field's semantic meaning. Semantic meaning belongs to <see cref="MapFieldDefinition"/>.
    ///
    /// The value zero is reserved for <see cref="Invalid"/>. Valid field definitions must use
    /// a supported non-zero value type.
    ///
    /// Existing value meanings are part of the workspace contract. Adding new values is safe;
    /// changing the meaning of an existing value should be treated as a package contract break.
    /// </remarks>
    public enum MapFieldValueType : byte
    {
        /// <summary>
        /// Invalid, unknown, or unassigned field value type.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Signed 32-bit integer field values.
        /// </summary>
        /// <remarks>
        /// Use this for deterministic scalar fields such as elevation, moisture, temperature,
        /// masks, costs, and fixed-point intermediate generation data.
        /// </remarks>
        Int32 = 1,

        /// <summary>
        /// Unsigned 8-bit integer field values.
        /// </summary>
        /// <remarks>
        /// Use this for compact categorical fields such as terrain kinds, biome kinds, flags,
        /// and low-cardinality classifications.
        /// </remarks>
        UInt8 = 2
    }
}