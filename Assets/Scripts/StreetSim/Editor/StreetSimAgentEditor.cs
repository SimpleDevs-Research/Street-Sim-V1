using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif


[CustomEditor(typeof(StreetSimAgent))]
public class StreetSimAgentEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimAgent agent = (StreetSimAgent)target;

        DrawDefaultInspector();

        if(GUILayout.Button("Set Children ExIDs")) {
            agent.GetAllChildren();
        }
        if(GUILayout.Button("Generate Mesh Copy")) {
            agent.GenerateMesh();
        }
    }

}
