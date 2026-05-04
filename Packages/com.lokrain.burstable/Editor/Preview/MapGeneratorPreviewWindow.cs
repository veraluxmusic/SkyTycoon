using Lokrain.Burstable.Authoring;
using Lokrain.Burstable.Generation;
using Lokrain.Burstable.Workspace;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Lokrain.Burstable.Editor.Preview
{
    /// <summary>
    /// Editor window for generating and inspecting deterministic tile-map previews.
    /// </summary>
    /// <remarks>
    /// This window is an editor-only visualization layer over the runtime map generation pipeline.
    /// It owns the preview <see cref="MapWorkspace"/> and preview <see cref="Texture2D"/> for the
    /// lifetime of the window, but it does not own or mutate generated runtime algorithms.
    ///
    /// Generation is scheduled through <see cref="MapGenerator"/> and completed on the editor update
    /// loop. The generated workspace is then converted into a preview texture according to the
    /// selected <see cref="MapPreviewMode"/>.
    ///
    /// Keep this class free of map-generation logic. Any logic that affects generated data belongs in
    /// Runtime, not Editor.
    /// </remarks>
    public sealed class MapGeneratorPreviewWindow : EditorWindow
    {
        private const string WindowTitle = "Map Generator";
        private const string MenuPath = "Tools/Burstable/Map Generator Preview";

        private MapGenerationProfile _profile;
        private MapWorkspace _workspace;
        private Texture2D _previewTexture;
        private MapPreviewMode _previewMode = MapPreviewMode.Terrain;

        private JobHandle _generationHandle;
        private bool _isGenerating;
        private double _generationStartedAt;

        /// <summary>
        /// Opens the map generator preview window.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<MapGeneratorPreviewWindow>(WindowTitle);
        }

        /// <summary>
        /// Completes pending generation and releases editor-owned native and UnityEngine resources.
        /// </summary>
        private void OnDisable()
        {
            CompleteGenerationIfNeeded();
            DestroyPreviewTexture();
            DisposeWorkspace();
        }

        /// <summary>
        /// Draws the editor UI and handles user-triggered generation actions.
        /// </summary>
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            _profile = (MapGenerationProfile)EditorGUILayout.ObjectField(
                "Profile",
                _profile,
                typeof(MapGenerationProfile),
                false);

            var previousPreviewMode = _previewMode;
            _previewMode = (MapPreviewMode)EditorGUILayout.EnumPopup("Preview Mode", _previewMode);

            if (EditorGUI.EndChangeCheck())
            {
                if (_workspace != null &&
                    _workspace.IsCreated &&
                    !_isGenerating &&
                    previousPreviewMode != _previewMode)
                {
                    RebuildPreviewTexture();
                }

                Repaint();
            }

            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Map Generation Profile to generate a preview.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(_profile == null || _isGenerating))
            {
                if (GUILayout.Button("Generate Preview"))
                {
                    Generate();
                }
            }

            using (new EditorGUI.DisabledScope(_profile == null || _isGenerating))
            {
                if (GUILayout.Button("Randomize Seed + Generate"))
                {
                    Undo.RecordObject(_profile, "Randomize Map Seed");
                    _profile.RandomizeSeed();
                    EditorUtility.SetDirty(_profile);
                    Generate();
                }
            }

            if (_isGenerating)
            {
                EditorGUILayout.HelpBox("Generating preview...", MessageType.Info);
            }

            DrawWorkspaceInfo();
            DrawPreviewTexture();
        }

        /// <summary>
        /// Polls the scheduled generation job and rebuilds the preview texture once generation completes.
        /// </summary>
        private void Update()
        {
            if (!_isGenerating || !_generationHandle.IsCompleted)
            {
                return;
            }

            _generationHandle.Complete();
            _isGenerating = false;

            RebuildPreviewTexture();

            var elapsedMs = (EditorApplication.timeSinceStartup - _generationStartedAt) * 1000.0;
            Debug.Log($"Burstable map preview generated in {elapsedMs:0.00} ms.");

            Repaint();
        }

        /// <summary>
        /// Starts generation using the currently assigned profile.
        /// </summary>
        /// <remarks>
        /// Any pending generation is completed before a new one is scheduled because the workspace is
        /// reused and must not be written by two generation requests at once.
        /// </remarks>
        private void Generate()
        {
            if (_profile == null)
            {
                return;
            }

            CompleteGenerationIfNeeded();

            var settings = _profile.ToSettings();
            EnsureWorkspace(settings.Width, settings.Height);

            _workspace.Clear();

            _generationStartedAt = EditorApplication.timeSinceStartup;
            _generationHandle = MapGenerator.Generate(settings, _workspace);
            _isGenerating = true;
        }

        /// <summary>
        /// Ensures the preview workspace exists and matches the requested dimensions.
        /// </summary>
        /// <param name="width">Requested map width in tiles.</param>
        /// <param name="height">Requested map height in tiles.</param>
        private void EnsureWorkspace(int width, int height)
        {
            if (_workspace != null &&
                _workspace.IsCreated &&
                _workspace.Width == width &&
                _workspace.Height == height)
            {
                return;
            }

            DisposeWorkspace();
            _workspace = new MapWorkspace(width, height, Allocator.Persistent);
        }

        /// <summary>
        /// Rebuilds the preview texture from the current workspace and preview mode.
        /// </summary>
        private void RebuildPreviewTexture()
        {
            if (_workspace == null || !_workspace.IsCreated)
            {
                return;
            }

            _previewTexture = MapPreviewTextureBuilder.BuildOrUpdate(
                _previewTexture,
                _workspace,
                _previewMode);
        }

        /// <summary>
        /// Draws basic information about the currently allocated preview workspace.
        /// </summary>
        private void DrawWorkspaceInfo()
        {
            if (_workspace == null || !_workspace.IsCreated)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Workspace",
                $"{_workspace.Width}x{_workspace.Height} tiles");
        }

        /// <summary>
        /// Draws the generated preview texture while preserving its aspect ratio.
        /// </summary>
        private void DrawPreviewTexture()
        {
            if (_previewTexture == null)
            {
                return;
            }

            var availableWidth = Mathf.Max(64f, position.width - 16f);
            var aspect = (float)_previewTexture.height / _previewTexture.width;
            var rect = GUILayoutUtility.GetRect(
                availableWidth,
                availableWidth * aspect,
                GUILayout.ExpandWidth(true));

            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// Completes the currently scheduled generation job if one is active.
        /// </summary>
        /// <remarks>
        /// This is required before disposing or reusing the workspace because native arrays may still
        /// be referenced by scheduled jobs.
        /// </remarks>
        private void CompleteGenerationIfNeeded()
        {
            if (!_isGenerating)
            {
                return;
            }

            _generationHandle.Complete();
            _isGenerating = false;
        }

        /// <summary>
        /// Disposes the editor-owned native workspace.
        /// </summary>
        private void DisposeWorkspace()
        {
            if (_workspace == null)
            {
                return;
            }

            _workspace.Dispose();
            _workspace = null;
        }

        /// <summary>
        /// Destroys the editor-owned preview texture.
        /// </summary>
        private void DestroyPreviewTexture()
        {
            if (_previewTexture == null)
            {
                return;
            }

            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }
}