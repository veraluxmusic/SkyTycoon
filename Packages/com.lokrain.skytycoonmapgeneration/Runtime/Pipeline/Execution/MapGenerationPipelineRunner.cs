#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Core.Hashing;
using Lokrain.SkyTycoon.MapGeneration.Core.Results;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation;
using Unity.Collections;

namespace Lokrain.SkyTycoon.MapGeneration.Pipeline.Execution
{
    /// <summary>
    /// Executes a compiled map-generation plan.
    ///
    /// This class is orchestration only. It must not contain editor code, texture code,
    /// or authoring-object references.
    /// </summary>
    public sealed class MapGenerationPipelineRunner
    {
        private readonly RegionSkeletonGenerator _regionSkeletonGenerator;
        private readonly RegionSkeletonValidator _regionSkeletonValidator;
        private readonly MacroLandformGenerator _macroLandformGenerator;
        private readonly MacroLandformValidator _macroLandformValidator;

        public MapGenerationPipelineRunner()
            : this(
                new RegionSkeletonGenerator(),
                new RegionSkeletonValidator(),
                new MacroLandformGenerator(),
                new MacroLandformValidator())
        {
        }

        public MapGenerationPipelineRunner(
            RegionSkeletonGenerator regionSkeletonGenerator,
            RegionSkeletonValidator regionSkeletonValidator,
            MacroLandformGenerator macroLandformGenerator,
            MacroLandformValidator macroLandformValidator)
        {
            _regionSkeletonGenerator = regionSkeletonGenerator ?? throw new ArgumentNullException(nameof(regionSkeletonGenerator));
            _regionSkeletonValidator = regionSkeletonValidator ?? throw new ArgumentNullException(nameof(regionSkeletonValidator));
            _macroLandformGenerator = macroLandformGenerator ?? throw new ArgumentNullException(nameof(macroLandformGenerator));
            _macroLandformValidator = macroLandformValidator ?? throw new ArgumentNullException(nameof(macroLandformValidator));
        }

        public MapGenerationExecutionResult Run(
            CompiledMapGenerationPlan plan,
            Allocator allocator = Allocator.Persistent)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (allocator == Allocator.Invalid || allocator == Allocator.None)
                throw new ArgumentOutOfRangeException(nameof(allocator), allocator, "Map generation output allocator must create native storage.");

            plan.Validate();

            RegionSkeletonResult? regionSkeletonResult = null;
            MacroLandformResult? macroLandformResult = null;

            try
            {
                regionSkeletonResult = _regionSkeletonGenerator.Generate(
                    plan.RegionSkeleton.Settings,
                    plan.RegionSkeleton.RoleCatalog,
                    allocator);

                macroLandformResult = _macroLandformGenerator.Generate(
                    plan.MacroLandform.Settings,
                    regionSkeletonResult,
                    plan.RegionSkeleton.RoleCatalog,
                    allocator);

                MapValidationReport regionReport = _regionSkeletonValidator.Validate(regionSkeletonResult);
                MapValidationReport macroReport = _macroLandformValidator.Validate(macroLandformResult);
                MapValidationReport validationReport = CombineValidationReports(regionReport, macroReport);

                MapGenerationStatus status = validationReport.Passed
                    ? MapGenerationStatus.Succeeded
                    : MapGenerationStatus.FailedValidation;

                StableHash128 artifactHash = ComputeExecutionArtifactHash(
                    plan,
                    regionSkeletonResult,
                    macroLandformResult,
                    validationReport,
                    status);

                MapGenerationResult summary = new(
                    plan.Request,
                    status,
                    artifactHash,
                    validationReport);

                MapGenerationExecutionResult result = new(
                    summary,
                    regionSkeletonResult,
                    macroLandformResult);

                regionSkeletonResult = null;
                macroLandformResult = null;
                return result;
            }
            finally
            {
                if (macroLandformResult != null)
                    macroLandformResult.Dispose();

                if (regionSkeletonResult != null)
                    regionSkeletonResult.Dispose();
            }
        }

        private static MapValidationReport CombineValidationReports(params MapValidationReport[] reports)
        {
            List<GenerationIssue> issues = new(16);
            StableHash128 hash = StableHash128.FromStableName("CombinedMapValidationReport.v0.0.1");

            for (int i = 0; i < reports.Length; i++)
            {
                MapValidationReport report = reports[i];
                hash = hash.Append(report.ArtifactHash).Append(report.FatalIssueCount).Append(report.ErrorIssueCount).Append(report.WarningIssueCount);

                for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
                    issues.Add(report.Issues[issueIndex]);
            }

            return new MapValidationReport(hash, issues);
        }

        private static StableHash128 ComputeExecutionArtifactHash(
            CompiledMapGenerationPlan plan,
            RegionSkeletonResult regionSkeletonResult,
            MacroLandformResult macroLandformResult,
            MapValidationReport validationReport,
            MapGenerationStatus status)
        {
            return StableHash128.FromStableName("MapGenerationExecutionResult.v0.0.1")
                .Append(plan.PlanHash)
                .Append(regionSkeletonResult.ArtifactHash)
                .Append(macroLandformResult.ArtifactHash)
                .Append(validationReport.ArtifactHash)
                .Append((int)status)
                .Append(validationReport.FatalIssueCount)
                .Append(validationReport.ErrorIssueCount)
                .Append(validationReport.WarningIssueCount);
        }
    }
}
