namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Describes how a generation stage uses a map field.
    /// </summary>
    /// <remarks>
    /// Field usage is stage-relative. A field can be an output of one stage and an input to
    /// another stage. For that reason, usage belongs to stage field contracts, not to
    /// workspace-level field definitions.
    /// </remarks>
    public enum MapStageFieldUsage : byte
    {
        /// <summary>
        /// Invalid, unknown, or unassigned field usage.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The stage reads the field but does not own writing it.
        /// </summary>
        Input = 1,

        /// <summary>
        /// The stage writes the field and is responsible for producing its values.
        /// </summary>
        Output = 2,

        /// <summary>
        /// The stage reads existing field values and writes updated field values.
        /// </summary>
        InputOutput = 3
    }
}