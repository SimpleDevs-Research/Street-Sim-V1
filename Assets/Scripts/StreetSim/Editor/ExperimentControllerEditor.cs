using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(TransformTrackingController))]
public class TransformTrackingControllerEditor : Editor
{

    public override void OnInspectorGUI() {
        TransformTrackingController experimentController = (TransformTrackingController)target;
        
        DrawDefaultInspector();
        
        /*
        if(GUILayout.Button("Start Tracking")) {
            experimentController.StartTracking();
        }
        if(GUILayout.Button("End Tracking")) {
            experimentController.EndTracking();
        }
        if(GUILayout.Button("Start Replay")) {
            experimentController.StartReplay();
        }
        if(GUILayout.Button("End Replay")) {
            experimentController.EndReplay();
        }
        if(GUILayout.Button("Save Tracked Data")) {
            experimentController.SaveTrackingData();
        }
        */
    }

}
