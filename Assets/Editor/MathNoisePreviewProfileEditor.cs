using UnityEditor;
using UnityEngine;

namespace Lokrain.SkyTycoon.Map.Editor
{
    [CustomEditor(typeof(MathNoisePreviewProfile))]
    public sealed class MathNoisePreviewProfileEditor : UnityEditor.Editor
    {
        private Texture2D previewTexture;
        private bool dirty = true;

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                dirty = true;
                Repaint();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Regenerate Preview"))
            {
                dirty = true;
                Repaint();
            }
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EnsurePreviewTexture();

            if (previewTexture == null)
                return;

            GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit, alphaBlend: false);
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Unity.Mathematics Noise Preview");
        }

        private void EnsurePreviewTexture()
        {
            var profile = (MathNoisePreviewProfile)target;
            int resolution = profile.Resolution;

            if (previewTexture == null ||
                previewTexture.width != resolution ||
                previewTexture.height != resolution)
            {
                DestroyPreviewTexture();

                previewTexture = new Texture2D(
                    resolution,
                    resolution,
                    TextureFormat.RGBA32,
                    mipChain: false,
                    linear: true)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = profile.FilterMode
                };

                dirty = true;
            }

            if (!dirty)
                return;

            profile.GenerateInto(previewTexture);
            dirty = false;
        }

        private void OnDisable()
        {
            DestroyPreviewTexture();
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