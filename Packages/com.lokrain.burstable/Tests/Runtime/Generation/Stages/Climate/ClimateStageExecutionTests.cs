using NUnit.Framework;
using Lokrain.Burstable.Generation.Pipeline;
using Lokrain.Burstable.Generation.Stages.Climate;
using Lokrain.Burstable.Generation.Stages.Elevation;
using Lokrain.Burstable.Workspace;
using Unity.Collections;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests workspace-backed climate generation behavior owned by <see cref="ClimateStage"/>.
    /// </summary>
    public sealed class ClimateStageExecutionTests
    {
        /// <summary>
        /// Verifies that climate execution reads the primary elevation field and writes the primary
        /// temperature and moisture fields.
        /// </summary>
        [Test]
        public void Execute_WhenFieldsExist_WritesTemperatureAndMoisture()
        {
            MapGenerationContext context = CreateContext(
                width: 4,
                height: 1,
                includeElevation: true,
                includeTemperature: true,
                includeMoisture: true);

            try
            {
                ClimateSettings settings = new(
                    baseTemperature: 100,
                    baseMoisture: 40,
                    elevationTemperaturePenalty: 2);

                ClimateStage stage = new(settings);

                MapField<int> elevation = context.Workspace.GetInt32Field(
                    ElevationFields.ElevationId);

                MapField<int> temperature = context.Workspace.GetInt32Field(
                    ClimateFields.TemperatureId);

                MapField<int> moisture = context.Workspace.GetInt32Field(
                    ClimateFields.MoistureId);

                NativeArray<int> elevationValues = elevation.AsNativeArray();
                elevationValues[0] = 0;
                elevationValues[1] = 5;
                elevationValues[2] = -3;
                elevationValues[3] = 10;

                stage.Execute(context);

                NativeArray<int> temperatureValues = temperature.AsNativeArray();
                NativeArray<int> moistureValues = moisture.AsNativeArray();

                Assert.AreEqual(
                    ClimateAlgorithm.CalculateTemperature(elevationValues[0], settings),
                    temperatureValues[0]);

                Assert.AreEqual(
                    ClimateAlgorithm.CalculateTemperature(elevationValues[1], settings),
                    temperatureValues[1]);

                Assert.AreEqual(
                    ClimateAlgorithm.CalculateTemperature(elevationValues[2], settings),
                    temperatureValues[2]);

                Assert.AreEqual(
                    ClimateAlgorithm.CalculateTemperature(elevationValues[3], settings),
                    temperatureValues[3]);

                Assert.AreEqual(ClimateAlgorithm.CalculateMoisture(settings), moistureValues[0]);
                Assert.AreEqual(ClimateAlgorithm.CalculateMoisture(settings), moistureValues[1]);
                Assert.AreEqual(ClimateAlgorithm.CalculateMoisture(settings), moistureValues[2]);
                Assert.AreEqual(ClimateAlgorithm.CalculateMoisture(settings), moistureValues[3]);
            }
            finally
            {
                context.Workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that scheduling climate generation rejects a workspace without the primary
        /// elevation field.
        /// </summary>
        [Test]
        public void Execute_WhenElevationFieldIsMissing_ThrowsArgumentException()
        {
            MapGenerationContext context = CreateContext(
                width: 4,
                height: 1,
                includeElevation: false,
                includeTemperature: true,
                includeMoisture: true);

            try
            {
                ClimateStage stage = ClimateStage.Default;

                Assert.Throws<System.ArgumentException>(() => stage.Execute(context));
            }
            finally
            {
                context.Workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that scheduling climate generation rejects a workspace without the primary
        /// temperature field.
        /// </summary>
        [Test]
        public void Execute_WhenTemperatureFieldIsMissing_ThrowsArgumentException()
        {
            MapGenerationContext context = CreateContext(
                width: 4,
                height: 1,
                includeElevation: true,
                includeTemperature: false,
                includeMoisture: true);

            try
            {
                ClimateStage stage = ClimateStage.Default;

                Assert.Throws<System.ArgumentException>(() => stage.Execute(context));
            }
            finally
            {
                context.Workspace.Dispose();
            }
        }

        /// <summary>
        /// Verifies that scheduling climate generation rejects a workspace without the primary
        /// moisture field.
        /// </summary>
        [Test]
        public void Execute_WhenMoistureFieldIsMissing_ThrowsArgumentException()
        {
            MapGenerationContext context = CreateContext(
                width: 4,
                height: 1,
                includeElevation: true,
                includeTemperature: true,
                includeMoisture: false);

            try
            {
                ClimateStage stage = ClimateStage.Default;

                Assert.Throws<System.ArgumentException>(() => stage.Execute(context));
            }
            finally
            {
                context.Workspace.Dispose();
            }
        }

        /// <summary>
        /// Creates a test generation context with selectively registered workspace fields.
        /// </summary>
        /// <param name="width">Map width in tiles.</param>
        /// <param name="height">Map height in tiles.</param>
        /// <param name="includeElevation">
        /// Whether to include the primary elevation field.
        /// </param>
        /// <param name="includeTemperature">
        /// Whether to include the primary temperature field.
        /// </param>
        /// <param name="includeMoisture">
        /// Whether to include the primary moisture field.
        /// </param>
        /// <returns>A generation context suitable for climate stage execution tests.</returns>
        private static MapGenerationContext CreateContext(
            int width,
            int height,
            bool includeElevation,
            bool includeTemperature,
            bool includeMoisture)
        {
            /*
             * Replace this helper body with the exact same workspace/context construction pattern
             * used by ElevationStage execution tests.
             *
             * The tests above intentionally depend only on public ClimateStage behavior. The setup
             * detail belongs here because MapWorkspace / MapGenerationContext construction is
             * package-specific and should stay consistent across all stage tests.
             *
             * Required setup:
             *
             * - Create a MapWorkspace for width * height tiles.
             * - Register/allocate ElevationFields.Elevation when includeElevation is true.
             * - Register/allocate ClimateFields.Temperature when includeTemperature is true.
             * - Register/allocate ClimateFields.Moisture when includeMoisture is true.
             * - Create a MapGenerationContext using that workspace.
             * - Ensure context.Length equals width * height.
             * - Ensure context.ExecutionSettings.InnerLoopBatchCount is valid.
             *
             * Example shape only:
             *
             * MapWorkspace workspace = new MapWorkspace(
             *     length: width * height,
             *     allocator: Allocator.Persistent);
             *
             * if (includeElevation)
             * {
             *     workspace.CreateField(ElevationFields.Elevation);
             * }
             *
             * if (includeTemperature)
             * {
             *     workspace.CreateField(ClimateFields.Temperature);
             * }
             *
             * if (includeMoisture)
             * {
             *     workspace.CreateField(ClimateFields.Moisture);
             * }
             *
             * return new MapGenerationContext(...);
             */

            throw new System.NotImplementedException(
                "Use the same MapWorkspace and MapGenerationContext setup used by ElevationStage tests.");
        }
    }
}