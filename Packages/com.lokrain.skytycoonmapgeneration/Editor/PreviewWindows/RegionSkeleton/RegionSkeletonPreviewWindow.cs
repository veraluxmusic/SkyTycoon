#nullable enable

using System;
using System.Globalization;
using System.IO;
using Lokrain.SkyTycoon.MapGeneration.Algorithms.RegionSkeleton;
using Lokrain.SkyTycoon.MapGeneration.Authoring.Profiles;
using Lokrain.SkyTycoon.MapGeneration.Core.Regions;
using Lokrain.SkyTycoon.MapGeneration.Core.Requests;
using Lokrain.SkyTycoon.MapGeneration.Core.Validation;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.PreviewWindows.RegionSkeleton
{
    /// <summary>
    /// First editor-facing trust tool for the map generator.
    /// It previews the strategic ownership skeleton before terrain, hydrology, or resource placement exist.
    /// </summary>
    public sealed class RegionSkeletonPreviewWindow : EditorWindow
    {
        private const int MinPreviewSize = 32;
        private const int MaxPreviewSize = 1024;
        private const string DefaultSeedText = "12345";

        private RegionSkeletonProfile? _profile;
        private int _width = 128;
        private int _height = 128;
        private int _playerCount = 8;
        private string _seedText = DefaultSeedText;
        private float _neutralCoreRadius = 0.16f;
        private float _boundaryWarpAmplitudeTurns;
        private int _minUsefulRegionNeighbors = 2;
        private float _minRegionAreaPercentOfMap = 0.06f;
        private float _minNeutralAreaPercentOfMap = 0.025f;
        private int _minRegionsTouchingNeutralZone = 4;
        private RegionSkeletonRoleAssignmentPolicy _roleAssignmentPolicy = RegionSkeletonRoleAssignmentPolicy.DeterministicShuffle;
        private RegionSkeletonPreviewLayer _previewLayer = RegionSkeletonPreviewLayer.Composite;
        private bool _drawBoundaries = true;
        private float _previewZoom = 4f;
        private Vector2 _scroll;
        private bool _showRoles = true;
        private bool _showAdjacency = true;
        private bool _showValidation = true;

        private RegionRoleCatalog? _roleCatalog;
        private RegionSkeletonResult? _result;
        private MapValidationReport? _validationReport;
        private Texture2D? _previewTexture;
        private string? _lastError;

        [MenuItem("Tools/Sky Tycoon/Map Generation/Region Skeleton Preview")]
        public static void Open()
        {
            RegionSkeletonPreviewWindow window = GetWindow<RegionSkeletonPreviewWindow>();
            window.titleContent = new GUIContent("Region Skeleton");
            window.minSize = new Vector2(720f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            _roleCatalog = RegionRoleCatalog.CreateTycoonEightRegionDefault();
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
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(330f)))
                {
                    DrawGenerationControls();
                    EditorGUILayout.Space(8f);
                    DrawReportControls();
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
                GUILayout.Label("Competitive Economy Skeleton", EditorStyles.boldLabel);
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

            EditorGUI.BeginChangeCheck();

            _profile = (RegionSkeletonProfile?)EditorGUILayout.ObjectField("Profile", _profile, typeof(RegionSkeletonProfile), false);
            _width = EditorGUILayout.IntSlider("Width", _width, MinPreviewSize, MaxPreviewSize);
            _height = EditorGUILayout.IntSlider("Height", _height, MinPreviewSize, MaxPreviewSize);
            _playerCount = EditorGUILayout.IntSlider("Player Regions", _playerCount, RegionSkeletonSettings.MinPlayerRegionCount, RegionSkeletonSettings.MaxPlayerRegionCount);
            _seedText = EditorGUILayout.TextField("Seed", _seedText);

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No profile assigned. The preview compiles a transient Tycoon default RegionSkeletonProfile using the inline controls below.",
                    MessageType.Info);

                _roleAssignmentPolicy = (RegionSkeletonRoleAssignmentPolicy)EditorGUILayout.EnumPopup("Role Assignment", _roleAssignmentPolicy);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Neutral Core", EditorStyles.boldLabel);
                _neutralCoreRadius = EditorGUILayout.Slider("Radius % Min Dim", _neutralCoreRadius, 0.02f, 0.35f);
                _minNeutralAreaPercentOfMap = EditorGUILayout.Slider("Min Neutral Area", _minNeutralAreaPercentOfMap, 0f, 0.25f);
                _minRegionsTouchingNeutralZone = EditorGUILayout.IntSlider("Min Neutral Contacts", _minRegionsTouchingNeutralZone, 1, _playerCount);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Topology Validation", EditorStyles.boldLabel);
                _boundaryWarpAmplitudeTurns = EditorGUILayout.Slider("Boundary Warp", _boundaryWarpAmplitudeTurns, 0f, 0.08f);
                _minUsefulRegionNeighbors = EditorGUILayout.IntSlider("Min Direct Neighbors", _minUsefulRegionNeighbors, 0, _playerCount - 1);
                _minRegionAreaPercentOfMap = EditorGUILayout.Slider("Min Region Area", _minRegionAreaPercentOfMap, 0.01f, 0.20f);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Profile assigned. Generation compiles RegionSkeletonSettings and RegionRoleCatalog from the ScriptableObject, then passes only runtime data into the generator.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _previewLayer = (RegionSkeletonPreviewLayer)EditorGUILayout.EnumPopup("Layer", _previewLayer);
            _drawBoundaries = EditorGUILayout.Toggle("Draw Boundaries", _drawBoundaries);
            _previewZoom = EditorGUILayout.Slider("Zoom", _previewZoom, 1f, 8f);

            bool settingsChanged = EditorGUI.EndChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate", GUILayout.Height(28f)))
                    Generate();

                GUI.enabled = _result != null;

                if (GUILayout.Button("Rebuild Preview", GUILayout.Height(28f)))
                    RebuildPreviewTexture();

                GUI.enabled = true;
            }

            if (settingsChanged && _result != null)
                RebuildPreviewTexture();

            if (!string.IsNullOrWhiteSpace(_lastError))
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
        }

        private void DrawReportControls()
        {
            if (_result == null)
            {
                EditorGUILayout.HelpBox(
                    "Generate the region skeleton first. This stage is intentionally terrain-free: it proves ownership, neutral access, role assignment, and topology before landform exists.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawRoles();
            DrawAdjacency();
            DrawValidation();
        }

        private void DrawSummary()
        {
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("Hash: " + _result!.ArtifactHash, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (_validationReport != null)
            {
                MessageType type = _validationReport.Passed ? MessageType.Info : MessageType.Error;
                string message = _validationReport.Passed
                    ? "Validation passed."
                    : "Validation failed. Fatal: "
                      + _validationReport.FatalIssueCount.ToString(CultureInfo.InvariantCulture)
                      + ", Errors: "
                      + _validationReport.ErrorIssueCount.ToString(CultureInfo.InvariantCulture)
                      + ", Warnings: "
                      + _validationReport.WarningIssueCount.ToString(CultureInfo.InvariantCulture);

                EditorGUILayout.HelpBox(message, type);
            }
        }

        private void DrawRoles()
        {
            _showRoles = EditorGUILayout.Foldout(_showRoles, "Region Roles", true);

            if (!_showRoles || _roleCatalog == null || _result == null)
                return;

            for (int i = 0; i < _result.RoleAssignments.Count; i++)
            {
                RegionRoleAssignment assignment = _result.RoleAssignments[i];
                RegionRoleDefinition definition = _roleCatalog.GetRequired(assignment.RoleId);
                Color32 color = RegionSkeletonPreviewPalette.GetRegionColor(assignment.RegionId.Value);

                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatchRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f), GUILayout.Height(18f));
                    EditorGUI.DrawRect(swatchRect, color);
                    EditorGUILayout.LabelField(
                        "Region " + assignment.RegionId.Value.ToString(CultureInfo.InvariantCulture),
                        GUILayout.Width(72f));
                    EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
                }

                EditorGUILayout.LabelField("Strength", definition.RequiredLocalStrength);
                EditorGUILayout.LabelField("Weakness", definition.RequiredWeakness);
                EditorGUILayout.LabelField("Export", definition.ExportTarget);
                EditorGUILayout.LabelField("Import", definition.ImportTemptation);
                EditorGUILayout.Space(4f);
            }
        }

        private void DrawAdjacency()
        {
            _showAdjacency = EditorGUILayout.Foldout(_showAdjacency, "Adjacency", true);

            if (!_showAdjacency || _result == null)
                return;

            RegionAdjacencyGraph graph = _result.AdjacencyGraph;
            EditorGUILayout.LabelField("Region Edges", graph.RegionEdgeCount.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Region/Neutral Edges", graph.RegionNeutralEdgeCount.ToString(CultureInfo.InvariantCulture));

            for (int regionValue = 1; regionValue <= graph.RegionCount; regionValue++)
            {
                RegionId regionId = new(regionValue);
                RegionId[] neighbors = graph.GetRegionNeighbors(regionId);
                string neighborText = neighbors.Length == 0 ? "none" : JoinRegionIds(neighbors);
                int neutralContacts = graph.NeutralZoneCount > 0 && graph.HasRegionNeutralEdge(regionId, new NeutralZoneId(1)) ? 1 : 0;

                EditorGUILayout.LabelField(
                    "Region " + regionValue.ToString(CultureInfo.InvariantCulture),
                    "Neighbors: " + neighborText + " | Neutral contacts: " + neutralContacts.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void DrawValidation()
        {
            _showValidation = EditorGUILayout.Foldout(_showValidation, "Validation Issues", true);

            if (!_showValidation || _validationReport == null)
                return;

            if (_validationReport.Issues.Count == 0)
            {
                EditorGUILayout.LabelField("No validation issues.");
                return;
            }

            for (int i = 0; i < _validationReport.Issues.Count; i++)
            {
                GenerationIssue issue = _validationReport.Issues[i];
                EditorGUILayout.HelpBox(
                    issue.Severity + " | " + issue.Id + "\n" + issue.Message,
                    ToMessageType(issue.Severity));
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (_previewTexture == null)
            {
                Rect emptyRect = GUILayoutUtility.GetRect(256f, 256f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(emptyRect, new Color(0.10f, 0.10f, 0.11f));
                GUI.Label(emptyRect, "No preview generated", CenteredLabelStyle);
                return;
            }

            float width = _previewTexture.width * _previewZoom;
            float height = _previewTexture.height * _previewZoom;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Rect previewRect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            EditorGUI.DrawTextureTransparent(previewRect, _previewTexture, ScaleMode.StretchToFill);
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            try
            {
                DisposeGeneratedState();
                _lastError = null;

                uint seed = ParseSeed(_seedText);
                HeightFieldDimensions dimensions = new(_width, _height);
                MapGenerationRequest request = new(
                    dimensions,
                    new DeterministicSeed(seed),
                    MapArchetype.SingleContinentEightRegionNeutralCore,
                    MapGenerationMode.Preview,
                    _playerCount);

                RegionSkeletonProfile? transientProfile = null;
                RegionSkeletonProfile? compileProfile = _profile;

                if (compileProfile == null)
                {
                    transientProfile = RegionSkeletonProfile.CreateTransientTycoonDefault();
                    transientProfile.ApplyPreviewOverrides(
                        _roleAssignmentPolicy,
                        _neutralCoreRadius,
                        _boundaryWarpAmplitudeTurns,
                        _minUsefulRegionNeighbors,
                        _minRegionAreaPercentOfMap,
                        _minNeutralAreaPercentOfMap,
                        _minRegionsTouchingNeutralZone);
                    compileProfile = transientProfile;
                }

                if (compileProfile == null)
                    throw new InvalidOperationException("Region skeleton profile compilation failed because no profile was available.");

                RegionSkeletonProfile profileToCompile = compileProfile;
                RegionSkeletonSettings settings;
                RegionRoleCatalog roleCatalog;

                try
                {
                    settings = profileToCompile.CompileSettings(request);
                    roleCatalog = profileToCompile.CreateRoleCatalog();
                    _roleCatalog = roleCatalog;
                }
                finally
                {
                    if (transientProfile != null)
                        DestroyImmediate(transientProfile);
                }

                RegionSkeletonGenerator generator = new();
                RegionSkeletonValidator validator = new();

                _result = generator.Generate(settings, roleCatalog, Allocator.Persistent);
                _validationReport = validator.Validate(_result);
                _previewTexture = RegionSkeletonPreviewTextureBuilder.CreateTexture(_result, _previewLayer, _drawBoundaries);
            }
            catch (Exception exception)
            {
                DisposeGeneratedState();
                _lastError = exception.Message;
                Debug.LogException(exception);
            }
        }

        private void RebuildPreviewTexture()
        {
            if (_result == null)
                return;

            if (_previewTexture == null || _previewTexture.width != _result.Dimensions.Width || _previewTexture.height != _result.Dimensions.Height)
            {
                DestroyPreviewTexture();
                _previewTexture = RegionSkeletonPreviewTextureBuilder.CreateTexture(_result, _previewLayer, _drawBoundaries);
                return;
            }

            RegionSkeletonPreviewTextureBuilder.RebuildTexture(_previewTexture, _result, _previewLayer, _drawBoundaries);
        }

        private void ExportPreviewPng()
        {
            if (_previewTexture == null)
                return;

            string path = EditorUtility.SaveFilePanel(
                "Export Region Skeleton Preview",
                Application.dataPath,
                "region-skeleton-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png",
                "png");

            if (string.IsNullOrWhiteSpace(path))
                return;

            byte[] bytes = _previewTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }

        private void DisposeGeneratedState()
        {
            if (_result != null)
            {
                _result.Dispose();
                _result = null;
            }

            _validationReport = null;
            DestroyPreviewTexture();
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture == null)
                return;

            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        private static uint ParseSeed(string seedText)
        {
            if (!uint.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint seed))
                throw new FormatException("Seed must be an unsigned 32-bit integer.");

            return seed == 0u ? 1u : seed;
        }

        private static string JoinRegionIds(RegionId[] regionIds)
        {
            string[] values = new string[regionIds.Length];

            for (int i = 0; i < regionIds.Length; i++)
                values[i] = regionIds[i].Value.ToString(CultureInfo.InvariantCulture);

            return string.Join(", ", values);
        }

        private static MessageType ToMessageType(GenerationIssueSeverity severity)
        {
            switch (severity)
            {
                case GenerationIssueSeverity.Info:
                    return MessageType.Info;
                case GenerationIssueSeverity.Warning:
                    return MessageType.Warning;
                case GenerationIssueSeverity.Error:
                case GenerationIssueSeverity.Fatal:
                    return MessageType.Error;
                default:
                    return MessageType.None;
            }
        }

        private static GUIStyle? _centeredLabelStyle;

        private static GUIStyle CenteredLabelStyle
        {
            get
            {
                if (_centeredLabelStyle != null)
                    return _centeredLabelStyle;

                _centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.78f) }
                };

                return _centeredLabelStyle;
            }
        }
    }
}
