#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Stages;

namespace Lokrain.SkyTycoon.MapGeneration.Core.Fields
{
    /// <summary>
    /// Managed metadata for a generated field. Descriptors are used by previews,
    /// validation reports, and artifact manifests; jobs should only receive the actual arrays.
    /// </summary>
    [Serializable]
    public sealed class MapFieldDescriptor
    {
        public MapFieldDescriptor(
            MapFieldId id,
            string name,
            FieldElementType elementType,
            FieldRange expectedRange,
            FieldPreviewHint previewHint,
            MapGenerationStageId producerStageId)
        {
            if (id.IsNone)
                throw new ArgumentException("Field descriptor id must not be None.", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field descriptor name must not be null, empty, or whitespace.", nameof(name));

            Id = id;
            Name = name;
            ElementType = elementType;
            ExpectedRange = expectedRange;
            PreviewHint = previewHint;
            ProducerStageId = producerStageId;
        }

        public MapFieldId Id { get; }
        public string Name { get; }
        public FieldElementType ElementType { get; }
        public FieldRange ExpectedRange { get; }
        public FieldPreviewHint PreviewHint { get; }
        public MapGenerationStageId ProducerStageId { get; }
    }
}
