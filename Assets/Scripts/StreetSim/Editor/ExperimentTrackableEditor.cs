using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif


[CustomEditor(typeof(ExperimentTrackable))]
public class ExperimentTrackableEditor : Editor
{

    public override void OnInspectorGUI() {
        ExperimentTrackable experimentTrackable = (ExperimentTrackable)target;
        if(GUILayout.Button("Start Tracking")) {
            experimentTrackable.StartTracking();
        }
        if(GUILayout.Button("End Tracking")) {
            experimentTrackable.EndTracking();
        }
        if(GUILayout.Button("Start Replay")) {
            experimentTrackable.StartReplay();
        }
        if(GUILayout.Button("End Replay")) {
            experimentTrackable.EndReplay();
        }
        if(GUILayout.Button("Clear Data")) {
            experimentTrackable.ClearData();
        }

        DrawDefaultInspector();
    }

}
