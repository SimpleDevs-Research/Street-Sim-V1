using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(StreetSimIDController))]
public class StreetSimIDControllerEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimIDController controller = (StreetSimIDController)target;
        
        DrawDefaultInspector();

        if (controller.loadedAssets.Count == 0) return;

        DrawUILine(Color.grey, 2, 10);

        foreach(LoadedPositionData data in controller.loadedAssets) {
            if (data.textAsset == null) continue;
            EditorGUILayout.LabelField(data.trialName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(data.idsTracked.Count + " unique IDs");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Replay")) {
                controller.ReplayRecord(data);
            }
            /*
            if (GUILayout.Button("Generate GazeMap")) {
                controller.LoadData(data);
            }
            */
            GUILayout.EndHorizontal();
            DrawUILine(Color.grey, 1, 5);
        }
        /*
        foreach(ExperimentID key in controller.payloads.Keys) {
            if (controller.payloads[key].Count <= 1) continue;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key.id + ": " + controller.payloads[key].Count + " positions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Replay")) {
                controller.ReplayRecord(key, false);
            }
            if (GUILayout.Button("Replay w/ Gaze")) {
                controller.ReplayRecord(key, true);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }
        */

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

    // Code attributed to: https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/
    public static void DrawUILine(Color color, int thickness = 2, int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
        r.height = thickness;
        r.y+=padding/2;
        r.x-=2;
        r.width +=6;
        EditorGUI.DrawRect(r, color);
    }

}
