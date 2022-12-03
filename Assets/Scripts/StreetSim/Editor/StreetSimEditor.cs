using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(StreetSim))]
public class StreetSimEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSim streetSim = (StreetSim)target;

        DrawDefaultInspector();

        DrawUILine(Color.grey, 2, 10);
        EditorGUILayout.LabelField("Click this to generate randomized trial groups", EditorStyles.boldLabel);
    
        GUI.enabled = !streetSim.initialized;
        if(GUILayout.Button("Generate Test Groups")) {
            streetSim.GenerateTestGroups();
        }
        GUI.enabled = true;

        if (!streetSim.initialized) return;

        DrawUILine(Color.grey, 2, 10);

        /*
        EditorGUILayout.LabelField("Should the simulation start on run?", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUI.enabled = !streetSim.startSimulationOnRun;
        if (GUILayout.Button("Enable Simulation")) {
            streetSim.startSimulationOnRun = true;
            EditorUtility.SetDirty(streetSim);
        }
        GUI.enabled = streetSim.startSimulationOnRun;
        if (GUILayout.Button("Disable Simulation")) {
            streetSim.startSimulationOnRun = false;
            EditorUtility.SetDirty(streetSim);
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
        */

        EditorGUILayout.LabelField("Simulator Controls", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUI.enabled = !streetSim.isRunning;
        if(GUILayout.Button("Start Simulation")) {
            streetSim.StartSimulation();
        }
        GUI.enabled = streetSim.isRunning;
        if(GUILayout.Button("End Simulation")) {
            streetSim.EndSimulation(true);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Save")) {
            streetSim.SaveSimulationData();
        }

        DrawUILine(Color.grey, 2, 10);

        /*
        EditorGUILayout.LabelField("Preprocess Gaze Data", EditorStyles.boldLabel);
        if (GUILayout.Button("Load")) {
            streetSim.LoadSimulationData();
        }
        */

        if (streetSim.loadedTrials.Count == 0) return;

        DrawPadding(30);
        EditorGUILayout.LabelField("Loaded Data", EditorStyles.boldLabel);
        GUIStyle gs = new GUIStyle();
        gs.normal.background = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));
        for(int i = 0; i < streetSim.loadedTrials.Count; i++) {
            if (i % 2 == 0) {
                GUILayout.BeginHorizontal(gs);
            } else {
                GUILayout.BeginHorizontal();
            }
            EditorGUILayout.LabelField(streetSim.loadedTrials[i].trialName);
            GUILayout.BeginHorizontal();
            if (streetSim.loadedTrials[i].positionData != null) {
                if (GUILayout.Button("Replay")) {
                    StreetSimIDController.ID.ReplayRecord(streetSim.loadedTrials[i].positionData);
                }
                if (GUILayout.Button("Gaze Map")) {
                    StreetSimRaycaster.R.ReplayRecord(streetSim.loadedTrials[i]);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }


        /*
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

    public static void DrawPadding(int padding = 10) {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding));
        r.height = padding;
        r.x-=2;
        r.width += 6;
        EditorGUI.DrawRect(r,Color.clear);
    }

    // Code attributed to: https://forum.unity.com/threads/changing-the-background-color-for-beginhorizontal.66015/
    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width*height];
        for(int i = 0; i < pix.Length; i++) {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
