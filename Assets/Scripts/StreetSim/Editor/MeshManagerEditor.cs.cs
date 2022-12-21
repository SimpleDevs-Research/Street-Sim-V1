using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(MeshManager))]
public class MeshManagerEditor : Editor
{

    public override void OnInspectorGUI() {
        MeshManager controller = (MeshManager)target;

        DrawDefaultInspector();

        if(GUILayout.Button("Map Triangles To IDs")) {
            controller.MapTrianglesToIDs();
        }

    }
}
