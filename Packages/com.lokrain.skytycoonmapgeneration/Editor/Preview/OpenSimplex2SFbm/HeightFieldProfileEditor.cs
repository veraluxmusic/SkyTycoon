#nullable enable

using Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm;
using Lokrain.SkyTycoon.MapGeneration.Domain.HeightFields;
using Lokrain.SkyTycoon.MapGeneration.Preview.OpenSimplex2SFbm;
using UnityEditor;
using UnityEngine;

namespace Lokrain.SkyTycoon.MapGeneration.Editor.Preview.OpenSimplex2SFbm
{
    [CustomEditor(typeof(HeightFieldProfile))]  
    internal sealed class HeightFieldProfileEditor : UnityEditor.Editor
    {
        private const int PreviewWidth = 128;
        private const int PreviewHeight = 128;

        private Texture2D? previewTexture;
        private string? errorMessage;

        private Vector2 noiseSpaceOrigin = Vector2.zero;
        private Vector2 noiseSpaceSize = new(4f, 4f);
        private HeightFieldValueRange outputRange = HeightFieldValueRange.UnsignedZeroToOne;
        private HeightFieldPreviewTextureMode textureMode =
            HeightFieldPreviewTextureMode.Rgba32Grayscale;

        private void OnEnable()
        {
            RebuildPreview();
        }

        private void OnDisable()
        {
            DestroyPreviewTexture();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Height Field Preview", EditorStyles.boldLabel);

            DrawPreviewControls();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Regenerate Preview"))
                RebuildPreview();

            EditorGUILayout.Space(6f);

            DrawPreview();
        }

        private void DrawPreviewControls()
        {
            EditorGUI.BeginChangeCheck();

            noiseSpaceOrigin = EditorGUILayout.Vector2Field("Noise Space Origin", noiseSpaceOrigin);
            noiseSpaceSize = EditorGUILayout.Vector2Field("Noise Space Size", noiseSpaceSize);
            outputRange = (HeightFieldValueRange)EditorGUILayout.EnumPopup("Output Range", outputRange);
            textureMode = (HeightFieldPreviewTextureMode)EditorGUILayout.EnumPopup("Texture Mode", textureMode);

            noiseSpaceSize.x = Mathf.Max(0.0001f, noiseSpaceSize.x);
            noiseSpaceSize.y = Mathf.Max(0.0001f, noiseSpaceSize.y);

            if (EditorGUI.EndChangeCheck())
                RebuildPreview();
        }

        private void RebuildPreview()
        {
            DestroyPreviewTexture();

            HeightFieldProfile? profile = target as HeightFieldProfile;

            if (profile == null)
            {
                errorMessage = "Selected object is not a height field profile.";
                return;
            }

            bool built = HeightFieldPreviewTextureBuilder.TryBuild(
                profile,
                PreviewWidth,
                PreviewHeight,
                noiseSpaceOrigin,
                noiseSpaceSize,
                outputRange,
                textureMode,
                out Texture2D? texture,
                out string? buildErrorMessage);

            if (!built)
            {
                previewTexture = null;
                errorMessage = buildErrorMessage ?? "Failed to build height field preview.";
                return;
            }

            previewTexture = texture;
            errorMessage = null;
        }

        private void DrawPreview()
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                return;
            }

            if (previewTexture == null)
            {
                EditorGUILayout.HelpBox("No preview texture generated.", MessageType.Info);
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(
                PreviewWidth,
                PreviewHeight,
                GUILayout.ExpandWidth(false));

            EditorGUI.DrawPreviewTexture(rect, previewTexture);
        }

        private void DestroyPreviewTexture()
        {
            if (previewTexture == null)
                return;

            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
}