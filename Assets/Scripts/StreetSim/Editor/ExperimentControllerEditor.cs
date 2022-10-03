using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(ExperimentController))]
public class ExperimentControllerEditor : Editor
{

    public override void OnInspectorGUI() {
        ExperimentController experimentController = (ExperimentController)target;
        if(GUILayout.Button("Start Tracking")) {
            experimentController.StartTracking();
        }
        if(GUILayout.Button("End Tracking")) {
            experimentController.EndTracking();
        }

        if(GUILayout.Button("End Tracking + Save Data")) {
            if (experimentController.EndTracking()) {
                experimentController.SaveTrackingData();
            }
        }
        DrawDefaultInspector();
    }

}
