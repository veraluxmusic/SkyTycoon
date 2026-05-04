using System;
using Lokrain.Burstable.Workspace;

namespace Lokrain.Burstable.Generation.Pipeline
{
    /// <summary>
    /// Describes one field requirement declared by a generation stage.
    /// </summary>
    /// <remarks>
    /// A requirement binds a workspace field definition to a stage-relative usage role. The
    /// same field definition may be an output of one stage and an input of a later stage.
    ///
    /// Required fields must exist in the workspace before the stage runs. Optional fields may be
    /// absent, but when present their definition must match the declared definition.
    /// </remarks>
    public readonly struct MapStageFieldRequirement : IEquatable<MapStageFieldRequirement>
    {
        /// <summary>
        /// Creates a stage field requirement.
        /// </summary>
        /// <param name="definition">Field definition used by the stage.</param>
        /// <param name="usage">Stage-relative field usage.</param>
        /// <param name="isRequired">Whether the field must exist in the workspace.</param>
        /// <remarks>
        /// The constructor stores values as provided. Call <see cref="Validate"/> before using
        /// the requirement in a stage contract.
        /// </remarks>
        public MapStageFieldRequirement(
            MapFieldDefinition definition,
            MapStageFieldUsage usage,
            bool isRequired)
        {
            Definition = definition;
            Usage = usage;
            IsRequired = isRequired;
        }

        /// <summary>
        /// Gets the field definition used by the stage.
        /// </summary>
        public MapFieldDefinition Definition { get; }

        /// <summary>
        /// Gets the stage-relative field usage.
        /// </summary>
        public MapStageFieldUsage Usage { get; }

        /// <summary>
        /// Gets a value indicating whether the field must exist in the workspace.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets a value indicating whether the stage reads this field.
        /// </summary>
        public bool IsInput => Usage == MapStageFieldUsage.Input ||
                               Usage == MapStageFieldUsage.InputOutput;

        /// <summary>
        /// Gets a value indicating whether the stage writes this field.
        /// </summary>
        public bool IsOutput => Usage == MapStageFieldUsage.Output ||
                                Usage == MapStageFieldUsage.InputOutput;

        /// <summary>
        /// Creates a required input field requirement.
        /// </summary>
        /// <param name="definition">Input field definition.</param>
        /// <returns>A required input field requirement.</returns>
        public static MapStageFieldRequirement RequiredInput(MapFieldDefinition definition)
        {
            return new MapStageFieldRequirement(
                definition,
                MapStageFieldUsage.Input,
                isRequired: true);
        }

        /// <summary>
        /// Creates a required output field requirement.
        /// </summary>
        /// <param name="definition">Output field definition.</param>
        /// <returns>A required output field requirement.</returns>
        public static MapStageFieldRequirement RequiredOutput(MapFieldDefinition definition)
        {
            return new MapStageFieldRequirement(
                definition,
                MapStageFieldUsage.Output,
                isRequired: true);
        }

        /// <summary>
        /// Creates a required input/output field requirement.
        /// </summary>
        /// <param name="definition">Input/output field definition.</param>
        /// <returns>A required input/output field requirement.</returns>
        public static MapStageFieldRequirement RequiredInputOutput(MapFieldDefinition definition)
        {
            return new MapStageFieldRequirement(
                definition,
                MapStageFieldUsage.InputOutput,
                isRequired: true);
        }

        /// <summary>
        /// Creates an optional input field requirement.
        /// </summary>
        /// <param name="definition">Optional input field definition.</param>
        /// <returns>An optional input field requirement.</returns>
        public static MapStageFieldRequirement OptionalInput(MapFieldDefinition definition)
        {
            return new MapStageFieldRequirement(
                definition,
                MapStageFieldUsage.Input,
                isRequired: false);
        }

        /// <summary>
        /// Validates this requirement.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the field definition or usage is invalid.
        /// </exception>
        public void Validate()
        {
            Definition.Validate();

            if (Usage == MapStageFieldUsage.Invalid)
            {
                throw new ArgumentException(
                    "Stage field usage must be valid.",
                    nameof(Usage));
            }
        }

        /// <summary>
        /// Determines whether this requirement is equal to another requirement.
        /// </summary>
        /// <param name="other">Other requirement.</param>
        /// <returns>
        /// <see langword="true"/> when both requirements are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool Equals(MapStageFieldRequirement other)
        {
            return Definition == other.Definition &&
                   Usage == other.Usage &&
                   IsRequired == other.IsRequired;
        }

        /// <summary>
        /// Determines whether this requirement is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare with this requirement.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="obj"/> is a
        /// <see cref="MapStageFieldRequirement"/> value equal to this requirement; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is MapStageFieldRequirement other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this requirement.
        /// </summary>
        /// <returns>A hash code derived from the definition, usage, and required flag.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Definition.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Usage;
                hashCode = (hashCode * 397) ^ IsRequired.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two requirements are equal.
        /// </summary>
        /// <param name="left">Left requirement.</param>
        /// <param name="right">Right requirement.</param>
        /// <returns>
        /// <see langword="true"/> when both requirements are equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator ==(
            MapStageFieldRequirement left,
            MapStageFieldRequirement right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two requirements are not equal.
        /// </summary>
        /// <param name="left">Left requirement.</param>
        /// <param name="right">Right requirement.</param>
        /// <returns>
        /// <see langword="true"/> when the requirements are not equal; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool operator !=(
            MapStageFieldRequirement left,
            MapStageFieldRequirement right)
        {
            return !left.Equals(right);
        }
    }
}