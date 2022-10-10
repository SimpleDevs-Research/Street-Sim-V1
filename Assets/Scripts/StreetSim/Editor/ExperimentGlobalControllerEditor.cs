using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(ExperimentGlobalController))]
public class ExperimentGlobalControllerEditor : Editor
{

    public override void OnInspectorGUI() {
        ExperimentGlobalController experimentGlobalController = (ExperimentGlobalController)target;

        DrawDefaultInspector();

        if(GUILayout.Button("Start Tracking")) {
            experimentGlobalController.StartTrackingEvents();
        }

        if(GUILayout.Button("End Tracking")) {
            experimentGlobalController.EndTrackingEvents();
        }

        if(GUILayout.Button("Save Tracking Data")) {
            experimentGlobalController.SaveTrackingEvents();
        }

        if(GUILayout.Button("Load Tracking Data")) {
            experimentGlobalController.LoadTrackingEvents();
        }

        if(GUILayout.Button("Prepare Replay")) {
            experimentGlobalController.PrepareReplay();
        }
        if(GUILayout.Button("Replay")) {
            experimentGlobalController.ReplayLoadedEvents();
        }
        if(GUILayout.Button("End Replay")) {
            experimentGlobalController.EndReplay();
        }

    }

}
