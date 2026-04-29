using Lokrain.SkyTycoon.MapGeneration.Authoring.OpenSimplex2SFbm;
using UnityEditor;

namespace Lokrain.SkyTycoon.MapGenerator.Editor.Authoring.OpenSimplex2SFbm
{
    [CustomEditor(typeof(HeightFieldProfile))]
    public sealed class HeightFieldProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Stage-1 OpenSimplex2S fBM height-field source only. This profile does not perform domain warp, landmass falloff, land/water classification, percentile cut, connectivity, or area compensation.",
                MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
