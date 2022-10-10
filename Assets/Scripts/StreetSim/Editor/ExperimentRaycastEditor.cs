using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif


[CustomEditor(typeof(ExperimentRaycast))]
public class ExperimentRaycastEditor : Editor
{
    /*
    public override void OnInspectorGUI() {
        ExperimentRaycast experimentRaycast = (ExperimentRaycast)target;

        if(GUILayout.Button("Start Casting")) experimentRaycast.StartCasting();

        if(GUILayout.Button("End Casting")) experimentRaycast.EndCasting();
        if(GUILayout.Button("Save Casting Data")) experimentRaycast.SaveGazeData();
        if(GUILayout.Button("Load Gaze Data")) experimentRaycast.LoadGazeData();

        DrawDefaultInspector();
    }
    */

}
