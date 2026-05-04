namespace Lokrain.Burstable.Workspace
{
    /// <summary>
    /// Identifies the stored value type of a generated map field.
    /// </summary>
    /// <remarks>
    /// Map field value types are storage metadata used by field definitions, registries,
    /// workspaces, validation, previews, exporters, diagnostics, and tooling.
    ///
    /// This value does not allocate storage, convert data, own field memory, validate field
    /// length, or describe a field's semantic meaning. Semantic meaning belongs to the feature,
    /// generation stage, preview, exporter, or consumer that owns the field contract.
    ///
    /// The value zero is reserved for <see cref="Invalid"/>. Valid field definitions must use
    /// a supported non-zero value type.
    ///
    /// Adding a new value type requires updating workspace allocation, typed accessors, tests,
    /// and documentation together. Changing the meaning of an existing value is a package
    /// contract break.
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
        /// Use this for deterministic scalar fields such as elevation, temperature, moisture,
        /// costs, masks, fixed-point intermediate values, and generated numeric data that
        /// should avoid floating-point platform variance.
        /// </remarks>
        Int32 = 1,

        /// <summary>
        /// Unsigned 8-bit integer field values.
        /// </summary>
        /// <remarks>
        /// Use this for compact categorical fields such as terrain kinds, biome kinds, flags,
        /// masks, and low-cardinality classifications.
        /// </remarks>
        UInt8 = 2
    }
}