using System;
using NUnit.Framework;
using Lokrain.Burstable.Generation.Stages.Climate;
using Unity.Jobs;

namespace Lokrain.Burstable.Tests.Runtime.Generation.Stages.Climate
{
    /// <summary>
    /// Tests public scheduling boundary behavior owned by <see cref="ClimateStage"/>.
    /// </summary>
    public sealed class ClimateStageScheduleTests
    {
        /// <summary>
        /// Verifies that scheduling climate generation rejects a null generation context.
        /// </summary>
        [Test]
        public void Schedule_WhenContextIsNull_ThrowsArgumentNullException()
        {
            ClimateStage stage = ClimateStage.Default;

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => stage.Schedule(
                    context: null,
                    dependency: default(JobHandle)));

            Assert.AreEqual("context", exception.ParamName);
        }

        /// <summary>
        /// Verifies that synchronous climate execution rejects a null generation context.
        /// </summary>
        [Test]
        public void Execute_WhenContextIsNull_ThrowsArgumentNullException()
        {
            ClimateStage stage = ClimateStage.Default;

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => stage.Execute(context: null));

            Assert.AreEqual("context", exception.ParamName);
        }
    }
}