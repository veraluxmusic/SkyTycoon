#nullable enable

using System;
using System.Globalization;
using System.IO;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.MacroLandform;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Recipes;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Compilation;
using Lokrain.SkyTycoon.MapGeneration.Pipeline.Execution;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.MacroLandform
{
    /// <summary>
    /// Editor-facing trust tool for Stage 0 + Stage 1 generation.
    /// Runs the same compiled recipe path as production and previews every macro-landform field.
    /// </summary>
    public sealed class MacroLandformPreviewWindow : EditorWindow
    {
        private const int MinPreviewSize = 32;
        private const int MaxPreviewSize = 1024;
        private const int DefaultPreviewSize = 128;

        [SerializeField] private MapGenerationRecipe? _recipe;
        [SerializeField] private RegionSkeletonProfile? _regionSkeletonProfile;
        [SerializeField] private MacroLandformProfile? _macroLandformProfile;
        [SerializeField] private int _width = DefaultPreviewSize;
        [SerializeField] private int _height = DefaultPreviewSize;
        [SerializeField] private string _seedText = "12345";
        [SerializeField] private MacroLandformPreviewLayer _previewLayer = MacroLandformPreviewLayer.Composite;
        [SerializeField] private bool _drawRegionBoundaries = true;
        [SerializeField] private bool _drawCoastline = true;
        [SerializeField] private float _previewZoom = 4.0f;

        private readonly MapGenerationPipelineRunner _runner = new();

        private MapGenerationExecutionResult? _result;
        private Texture2D? _previewTexture;
        private Vector2 _scroll;
        private string? _lastError;
        private bool _showSummary = true;
        private bool _showRoles = true;
        private bool _showValidation = true;
        private bool _showSettings = true;

        [MenuItem("Tools/Sky Tycoon/Map Generation/Macro Landform Preview")]
        public static void Open()
        {
            MacroLandformPreviewWindow window = GetWindow<MacroLandformPreviewWindow>();
            window.titleContent = new GUIContent("Macro Landform");
            window.minSize = new Vector2(780f, 600f);
            window.Show();
        }

        private void OnDisable()
        {
            DisposeGeneratedState();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(360f)))
                {
                    DrawGenerationControls();
                    EditorGUILayout.Space(8f);
                    DrawReports();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawPreview();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Macro Landform", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                GUI.enabled = _result != null && _previewTexture != null;

                if (GUILayout.Button("Export PNG", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                    ExportPreviewPng();

                GUI.enabled = true;
            }
        }

        private void DrawGenerationControls()
        {
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            _recipe = (MapGenerationRecipe?)EditorGUILayout.ObjectField("Recipe", _recipe, typeof(MapGenerationRecipe), false);

            if (_recipe == null)
            {
                EditorGUILayout.HelpBox(
                    "No recipe assigned. The preview compiles from the profile fields below. Missing profiles are replaced with transient Tycoon defaults.",
                    MessageType.Info);

                _regionSkeletonProfile = (RegionSkeletonProfile?)EditorGUILayout.ObjectField("Region Profile", _regionSkeletonProfile, typeof(RegionSkeletonProfile), false);
                _macroLandformProfile = (MacroLandformProfile?)EditorGUILayout.ObjectField("Landform Profile", _macroLandformProfile, typeof(MacroLandformProfile), false);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Recipe assigned. Preview uses MapGenerationRecipe.Compile(...), then MapGenerationPipelineRunner.Run(...).",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            _width = EditorGUILayout.IntSlider("Width", _width, MinPreviewSize, MaxPreviewSize);
            _height = EditorGUILayout.IntSlider("Height", _height, MinPreviewSize, MaxPreviewSize);
            _seedText = EditorGUILayout.TextField("Seed", _seedText);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _previewLayer = (MacroLandformPreviewLayer)EditorGUILayout.EnumPopup("Layer", _previewLayer);
            _drawRegionBoundaries = EditorGUILayout.Toggle("Region Boundaries", _drawRegionBoundaries);
            _drawCoastline = EditorGUILayout.Toggle("Coastline", _drawCoastline);
            _previewZoom = EditorGUILayout.Slider("Zoom", _previewZoom, 1f, 8f);
            bool previewChanged = EditorGUI.EndChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate", GUILayout.Height(28f)))
                    Generate();

                GUI.enabled = _result != null;

                if (GUILayout.Button("Rebuild Preview", GUILayout.Height(28f)))
                    RebuildPreviewTexture();

                GUI.enabled = true;
            }

            if (previewChanged && _result != null)
                RebuildPreviewTexture();

            if (!string.IsNullOrWhiteSpace(_lastError))
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
        }

        private void DrawReports()
        {
            if (_result == null)
            {
                EditorGUILayout.HelpBox(
                    "Generate a macro landform. This executes Stage 0 Region Skeleton and Stage 1 Macro Landform through the compiled runtime pipeline.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawSettings();
            DrawRoles();
            DrawValidation();
        }

        private void DrawSummary()
        {
            _showSummary = EditorGUILayout.Foldout(_showSummary, "Summary", true);

            if (!_showSummary || _result == null)
                return;

            MacroLandformResult landform = _result.MacroLandformResult;
            HeightFieldDimensions dimensions = landform.Dimensions;
            float actualLandPercent = (float)landform.LandSampleCount / dimensions.SampleCount;

            EditorGUILayout.SelectableLabel("Execution Hash: " + _result.ArtifactHash, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("Landform Hash: " + landform.ArtifactHash, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("Dimensions", dimensions.ToString());
            EditorGUILayout.LabelField("Land Samples", landform.LandSampleCount.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Land Percent", actualLandPercent.ToString("P2", CultureInfo.InvariantCulture));

            MessageType messageType = _result.ValidationReport.Passed ? MessageType.Info : MessageType.Error;
            string message = _result.ValidationReport.Passed
                ? "Validation passed."
                : "Validation failed. Fatal: "
                  + _result.ValidationReport.FatalIssueCount.ToString(CultureInfo.InvariantCulture)
                  + ", Errors: "
                  + _result.ValidationReport.ErrorIssueCount.ToString(CultureInfo.InvariantCulture)
                  + ", Warnings: "
                  + _result.ValidationReport.WarningIssueCount.ToString(CultureInfo.InvariantCulture);

            EditorGUILayout.HelpBox(message, messageType);
        }

        private void DrawSettings()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "Macro Landform Settings", true);

            if (!_showSettings || _result == null)
                return;

            MacroLandformSettings settings = _result.MacroLandformResult.Settings;
            EditorGUILayout.LabelField("Target Land", settings.TargetLandPercent.ToString("P2", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Hard Water Border", settings.HardWaterBorderThickness.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Falloff Exponent", settings.ContinentFalloffExponent.ToString("0.###", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("fBm Octaves", settings.FbmOctaves.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("fBm Wavelength", settings.FbmBaseWavelengthTiles.ToString("0.###", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Warp Amplitude", settings.DomainWarpAmplitudeTiles.ToString("0.###", CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Warp Wavelength", settings.DomainWarpWavelengthTiles.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private void DrawRoles()
        {
            _showRoles = EditorGUILayout.Foldout(_showRoles, "Region Roles", true);

            if (!_showRoles || _result == null)
                return;

            RegionSkeletonResult skeleton = _result.RegionSkeletonResult;
            RegionRoleCatalog roleCatalog = BuildCurrentRoleCatalog();

            for (int i = 0; i < skeleton.RoleAssignments.Count; i++)
            {
                RegionRoleAssignment assignment = skeleton.RoleAssignments[i];
                RegionRoleDefinition definition = roleCatalog.GetRequired(assignment.RoleId);
                Color32 color = RegionSkeletonPreviewPalette.GetRegionColor(assignment.RegionId.Value);

                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatchRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f), GUILayout.Height(18f));
                    EditorGUI.DrawRect(swatchRect, color);
                    EditorGUILayout.LabelField("Region " + assignment.RegionId.Value.ToString(CultureInfo.InvariantCulture), GUILayout.Width(72f));
                    EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
                }

                EditorGUILayout.LabelField("Terrain", definition.PreferredTerrain);
                EditorGUILayout.LabelField("Strength", definition.RequiredLocalStrength);
                EditorGUILayout.LabelField("Weakness", definition.RequiredWeakness);
                EditorGUILayout.Space(3f);
            }
        }

        private void DrawValidation()
        {
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Validation Issues", true);

            if (!_showValidation || _result == null)
                return;

            MapValidationReport report = _result.ValidationReport;

            if (report.Issues.Count == 0)
            {
                EditorGUILayout.LabelField("No issues.");
                return;
            }

            for (int i = 0; i < report.Issues.Count; i++)
            {
                GenerationIssue issue = report.Issues[i];
                MessageType messageType = issue.Severity == GenerationIssueSeverity.Warning
                    ? MessageType.Warning
                    : MessageType.Error;

                EditorGUILayout.HelpBox(
                    issue.Severity
                    + " | "
                    + issue.Id
                    + " | "
                    + issue.Message,
                    messageType);
            }
        }

        private void DrawPreview()
        {
            if (_previewTexture == null)
            {
                EditorGUILayout.HelpBox("No preview texture. Generate first.", MessageType.Info);
                return;
            }

            float width = _previewTexture.width * _previewZoom;
            float height = _previewTexture.height * _previewZoom;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.StretchToFill);
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            try
            {
                _lastError = null;
                DisposeGeneratedState();

                MapGenerationRequest request = CreateRequest();
                CompiledMapGenerationPlan plan = CompilePlan(request);
                _result = _runner.Run(plan, Allocator.Persistent);
                RebuildPreviewTexture();
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Debug.LogException(exception);
                DisposeGeneratedState();
            }
        }

        private void RebuildPreviewTexture()
        {
            if (_result == null)
                return;

            if (_previewTexture == null
                || _previewTexture.width != _result.Request.Dimensions.Width
                || _previewTexture.height != _result.Request.Dimensions.Height)
            {
                DestroyPreviewTexture();
                _previewTexture = MacroLandformPreviewTextureBuilder.CreateTexture(
                    _result.RegionSkeletonResult,
                    _result.MacroLandformResult,
                    _previewLayer,
                    _drawRegionBoundaries,
                    _drawCoastline);
                return;
            }

            MacroLandformPreviewTextureBuilder.RebuildTexture(
                _previewTexture,
                _result.RegionSkeletonResult,
                _result.MacroLandformResult,
                _previewLayer,
                _drawRegionBoundaries,
                _drawCoastline);
        }

        private MapGenerationRequest CreateRequest()
        {
            uint seed = ParseSeed(_seedText);
            return MapGenerationRequest.CreateDefaultPreview(_width, _height, seed);
        }

        private CompiledMapGenerationPlan CompilePlan(MapGenerationRequest request)
        {
            if (_recipe != null)
                return _recipe.Compile(request);

            RegionSkeletonProfile? transientRegionProfile = null;
            MacroLandformProfile? transientLandformProfile = null;

            try
            {
                RegionSkeletonProfile regionProfile;

                if (_regionSkeletonProfile != null)
                {
                    regionProfile = _regionSkeletonProfile;
                }
                else
                {
                    transientRegionProfile = RegionSkeletonProfile.CreateTransientTycoonDefault();
                    regionProfile = transientRegionProfile;
                }

                MacroLandformProfile landformProfile;

                if (_macroLandformProfile != null)
                {
                    landformProfile = _macroLandformProfile;
                }
                else
                {
                    transientLandformProfile = MacroLandformProfile.CreateTransientDefault();
                    landformProfile = transientLandformProfile;
                }

                RegionSkeletonSettings regionSettings = regionProfile.CompileSettings(request);
                RegionRoleCatalog roleCatalog = regionProfile.CreateRoleCatalog();
                MacroLandformSettings landformSettings = landformProfile.CompileSettings(request);

                return new CompiledMapGenerationPlan(
                    request,
                    new CompiledRegionSkeletonStage(regionSettings, roleCatalog),
                    new CompiledMacroLandformStage(landformSettings));
            }
            finally
            {
                if (transientRegionProfile != null)
                    DestroyImmediate(transientRegionProfile);

                if (transientLandformProfile != null)
                    DestroyImmediate(transientLandformProfile);
            }
        }

        private RegionRoleCatalog BuildCurrentRoleCatalog()
        {
            if (_result == null)
                return RegionRoleCatalog.CreateTycoonEightRegionDefault();

            if (_recipe != null && _recipe.HasRegionSkeletonProfile)
                return _recipe.CreateRegionRoleCatalog();

            if (_regionSkeletonProfile != null)
                return _regionSkeletonProfile.CreateRoleCatalog();

            return RegionRoleCatalog.CreateTycoonEightRegionDefault();
        }

        private static uint ParseSeed(string seedText)
        {
            if (uint.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint seed) && seed != 0u)
                return seed;

            unchecked
            {
                uint hash = 2166136261u;

                for (int i = 0; i < seedText.Length; i++)
                {
                    hash ^= seedText[i];
                    hash *= 16777619u;
                }

                if (hash == 0u)
                    hash = 1u;

                return hash;
            }
        }

        private void ExportPreviewPng()
        {
            if (_previewTexture == null)
                return;

            string path = EditorUtility.SaveFilePanel(
                "Export Macro Landform Preview",
                Application.dataPath,
                "macro-landform-" + _previewLayer + ".png",
                "png");

            if (string.IsNullOrWhiteSpace(path))
                return;

            File.WriteAllBytes(path, _previewTexture.EncodeToPNG());
            AssetDatabase.Refresh();
        }

        private void DisposeGeneratedState()
        {
            DestroyPreviewTexture();

            if (_result != null)
            {
                _result.Dispose();
                _result = null;
            }
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture == null)
                return;

            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }
}
