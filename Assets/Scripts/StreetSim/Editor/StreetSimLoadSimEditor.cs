using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(StreetSimLoadSim))]
public class StreetSimLoadSimEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimLoadSim controller = (StreetSimLoadSim)target;

        DrawDefaultInspector();

        if (!controller.initialized) return;
        if (!StreetSim.S.initialized) return;

        EditorGUILayout.LabelField("Global Controls", EditorStyles.boldLabel);
        if (GUILayout.Button("Load")) {
            controller.Load();
        }
        if (GUILayout.Button("Sphere Grid")) {
            controller.GenerateSphereGrid();
        }

        if (controller.participantData.Count == 0) return;

        DrawPadding(5);
        
        // At this point, we know we have SOME data to work with
        // What appears will differ based on if controller.currentParticipant != null and controller.participantData[currentParticipant] != null
        if (controller.currentParticipant != null && controller.currentParticipant.Length > 0) {
            // We've staged a participant. Let's show the UI for that participant only
            DrawStagedParticipantUI(controller);
        } else {
            // We haven't staged a participant. We need to show all options
            DrawGlobalUI(controller);
        }
    }

    private void DrawGlobalUI(StreetSimLoadSim controller) {
        
        EditorGUILayout.LabelField("Available Participants", EditorStyles.boldLabel);
        
        GUIStyle gs = new GUIStyle();
        gs.normal.background = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));

        int i = -1;
        foreach(string participantName in controller.participantData.Keys) {
            i++;
            if (i % 2 == 0) GUILayout.BeginHorizontal(gs);
            else GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(participantName);
            if (GUILayout.Button("Open")) {
                controller.StageParticipant(participantName);
            }
            
            GUILayout.EndHorizontal();
        }
    }

    private void DrawStagedParticipantUI(StreetSimLoadSim controller) {
        if (GUILayout.Button("Back")) {
            controller.StageParticipant(null);
        }
        DrawPadding(5);
        EditorGUILayout.LabelField(controller.currentParticipant, EditorStyles.boldLabel);
        DrawPadding(5);
        EditorGUILayout.LabelField("Trials", EditorStyles.boldLabel);

        GUIStyle gs = new GUIStyle();
        gs.normal.background = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));

        for(int i = 0; i < controller.participantData[controller.currentParticipant].Count; i++) {
             if (i % 2 == 0) GUILayout.BeginVertical(gs);
            else GUILayout.BeginVertical();

            EditorGUILayout.LabelField(controller.participantData[controller.currentParticipant][i].trialName);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Gaze")) {
                StreetSimRaycaster.R.ResetGazeReplay();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (controller.participantData[controller.currentParticipant][i].positionData != null) {
                if (GUILayout.Button("Replay")) {
                    StreetSimIDController.ID.ReplayRecord(controller.participantData[controller.currentParticipant][i].positionData);
                }
                if (GUILayout.Button("Fixation Map")) {
                    StreetSimRaycaster.R.ReplayRecord(controller.participantData[controller.currentParticipant][i]);
                }
                if (GUILayout.Button("Gaze Hits")) {
                    StreetSimRaycaster.R.ReplayGazeHits(controller.participantData[controller.currentParticipant][i]);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
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

    public void OnSceneGUI ()  {
        StreetSimLoadSim controller = (StreetSimLoadSim)target;
        if (controller.points.Count == 0) return;
        foreach(Vector3 point in controller.points) {
            Vector3 pos = controller.cam360.position + point;
            Handles.color = Color.white;
            Handles.DrawWireDisc(
                pos,                                      // position
                controller.cam360.position - pos,      // normal
                controller.averageDistanceBetweenPoints
                //((2*Mathf.PI*controller.sphereRadius*(float)controller.sphereAngle)/360f)*0.5f*Mathf.Pow(2f,0.5f) // radius
            );
        }
    }
}
