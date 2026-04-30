#nullable enable

using System;
using Lokrain.SkyTycoon.MapGeneration.Core.Fields;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;

namespace Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform
{
    /// <summary>
    /// Owns output fields from Stage 1: Macro Landform.
    /// Dispose this result when the generated fields are no longer needed.
    /// </summary>
    public sealed class MacroLandformResult : IDisposable
    {
        private NativeField2D<float> _baseHeightField;
        private NativeField2D<byte> _landMaskField;
        private NativeField2D<byte> _oceanMaskField;
        private NativeField2D<float> _coastDistanceField;
        private NativeField2D<float> _continentDistanceField;
        private NativeField2D<float> _slopeField;
        private NativeField2D<float> _buildabilityField;
        private NativeField2D<float> _mountainInfluenceField;
        private NativeField2D<float> _basinInfluenceField;
        private NativeField2D<float> _plainInfluenceField;
        private bool _disposed;

        public MacroLandformResult(
            MacroLandformSettings settings,
            NativeField2D<float> baseHeightField,
            NativeField2D<byte> landMaskField,
            NativeField2D<byte> oceanMaskField,
            NativeField2D<float> coastDistanceField,
            NativeField2D<float> continentDistanceField,
            NativeField2D<float> slopeField,
            NativeField2D<float> buildabilityField,
            NativeField2D<float> mountainInfluenceField,
            NativeField2D<float> basinInfluenceField,
            NativeField2D<float> plainInfluenceField,
            int landSampleCount,
            StableHash128 artifactHash)
        {
            settings.Validate();

            ValidateField(baseHeightField, settings.Dimensions, nameof(baseHeightField));
            ValidateField(landMaskField, settings.Dimensions, nameof(landMaskField));
            ValidateField(oceanMaskField, settings.Dimensions, nameof(oceanMaskField));
            ValidateField(coastDistanceField, settings.Dimensions, nameof(coastDistanceField));
            ValidateField(continentDistanceField, settings.Dimensions, nameof(continentDistanceField));
            ValidateField(slopeField, settings.Dimensions, nameof(slopeField));
            ValidateField(buildabilityField, settings.Dimensions, nameof(buildabilityField));
            ValidateField(mountainInfluenceField, settings.Dimensions, nameof(mountainInfluenceField));
            ValidateField(basinInfluenceField, settings.Dimensions, nameof(basinInfluenceField));
            ValidateField(plainInfluenceField, settings.Dimensions, nameof(plainInfluenceField));

            if (landSampleCount < 0 || landSampleCount > settings.Dimensions.SampleCount)
                throw new ArgumentOutOfRangeException(nameof(landSampleCount), landSampleCount, "Land sample count is outside the valid range.");

            Settings = settings;
            Dimensions = settings.Dimensions;
            _baseHeightField = baseHeightField;
            _landMaskField = landMaskField;
            _oceanMaskField = oceanMaskField;
            _coastDistanceField = coastDistanceField;
            _continentDistanceField = continentDistanceField;
            _slopeField = slopeField;
            _buildabilityField = buildabilityField;
            _mountainInfluenceField = mountainInfluenceField;
            _basinInfluenceField = basinInfluenceField;
            _plainInfluenceField = plainInfluenceField;
            LandSampleCount = landSampleCount;
            ArtifactHash = artifactHash;
        }

        public MacroLandformSettings Settings { get; }
        public HeightFieldDimensions Dimensions { get; }
        public int LandSampleCount { get; }
        public StableHash128 ArtifactHash { get; }

        public NativeField2D<float> BaseHeightField => _baseHeightField;
        public NativeField2D<byte> LandMaskField => _landMaskField;
        public NativeField2D<byte> OceanMaskField => _oceanMaskField;
        public NativeField2D<float> CoastDistanceField => _coastDistanceField;
        public NativeField2D<float> ContinentDistanceField => _continentDistanceField;
        public NativeField2D<float> SlopeField => _slopeField;
        public NativeField2D<float> BuildabilityField => _buildabilityField;
        public NativeField2D<float> MountainInfluenceField => _mountainInfluenceField;
        public NativeField2D<float> BasinInfluenceField => _basinInfluenceField;
        public NativeField2D<float> PlainInfluenceField => _plainInfluenceField;

        public void Dispose()
        {
            if (_disposed)
                return;

            _baseHeightField.Dispose();
            _landMaskField.Dispose();
            _oceanMaskField.Dispose();
            _coastDistanceField.Dispose();
            _continentDistanceField.Dispose();
            _slopeField.Dispose();
            _buildabilityField.Dispose();
            _mountainInfluenceField.Dispose();
            _basinInfluenceField.Dispose();
            _plainInfluenceField.Dispose();

            _disposed = true;
        }

        private static void ValidateField<T>(NativeField2D<T> field, HeightFieldDimensions dimensions, string argumentName)
            where T : unmanaged
        {
            if (!field.IsCreated)
                throw new ArgumentException("Macro landform output field must be created.", argumentName);

            if (field.Dimensions != dimensions)
                throw new ArgumentException("Macro landform output field dimensions do not match settings.", argumentName);
        }
    }
}
