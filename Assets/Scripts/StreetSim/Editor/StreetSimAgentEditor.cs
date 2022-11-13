using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif


[CustomEditor(typeof(StreetSimAgent))]
public class StreetSimAgentEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimAgent agent = (StreetSimAgent)target;
        if(GUILayout.Button("Set Children ExIDs")) {
            agent.GetAllChildren();
        }

        DrawDefaultInspector();
    }

}
