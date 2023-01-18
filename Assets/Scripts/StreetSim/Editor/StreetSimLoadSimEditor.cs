using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CustomEditor(typeof(StreetSimLoadSim))]
public class StreetSimLoadSimEditor : Editor
{

    public override void OnInspectorGUI() {
        StreetSimLoadSim controller = (StreetSimLoadSim)target;
        GUIStyle gs = new GUIStyle();
        gs.normal.background = MakeTex(600, 1, new Color(1.0f, 1.0f, 1.0f, 0.1f));

        DrawDefaultInspector();

        for(int i = 0; i < controller.gazeObjectTracking.Count; i++) {
            if (i % 2 == 0) GUILayout.BeginHorizontal(gs);
            else GUILayout.BeginHorizontal();

            ExperimentID curID = controller.gazeObjectTracking[i];
            EditorGUILayout.LabelField("\""+curID.id+"\":");
            if(GUILayout.Button("Gaze Heat Map")) {
                // controller.TrackGazeOnObject(curID);
                controller.TrackGazeOnObjectFromFile(curID);
            }
            if(GUILayout.Button("Track Gaze Groups")) {
                controller.TrackGazeGroupsOnObject(curID);
            }

            GUILayout.EndHorizontal();
        }

        DrawPadding(10);

        if (!controller.initialized) return;
        if (!StreetSim.S.initialized) return;

        EditorGUILayout.LabelField("Global Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Test Sphere Grid")) {
            controller.GenerateSphereGrid();
        }
        if (GUILayout.Button("Load")) {
            controller.Load();
        }

        if (controller.participantData.Count == 0) return;
        
        if (GUILayout.Button("Generate Ground Truth Saliency")) {
            controller.GroundTruthSaliency();
        }

        if (GUILayout.Button("Calculate Ground Truth ROC")) {
            controller.GroundTruthROC();
        }

        DrawPadding(5);

        float z;
        string toggleText;
        for(int i = 0; i < controller.NumDiscretizations; i++) {
            if (i % 2 == 0) GUILayout.BeginHorizontal(gs);
            else GUILayout.BeginHorizontal();
            
            z = controller.GetDiscretizationFromIndex(i);
            EditorGUILayout.LabelField("Z: "+z.ToString());
            if(GUILayout.Button("Get Saliency")) {
                controller.TruthSaliencyAtZ(z);
            }
            if(GUILayout.Button("Get ROC")) {
                controller.ROCAtZ(z);
            }
            /*
            toggleText = (controller.discretizations[z]) ? "Turn off" : "Turn on";

            EditorGUILayout.LabelField("Z: "+z.ToString());   
            if (GUILayout.Button("Place Cam")) {
                controller.PlaceCam(z);
            }             
            if (GUILayout.Button(toggleText)) {
                controller.ToggleDiscretization(z);
            }
            */

            GUILayout.EndHorizontal();
        }

        DrawPadding(10);
        
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

        string buttonLabel;

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
                if (controller.participantData[controller.currentParticipant][i].averageFixations == default(LoadedFixationData)) {
                    if (GUILayout.Button("Get Av. Fix. Map")) {
                        controller.ManuallyGenerateFixationMap(controller.participantData[controller.currentParticipant][i]);
                    }
                } else {
                    if (GUILayout.Button("Show Av. Fix. Map")) {
                        controller.ToggleAverageFixationMap(controller.participantData[controller.currentParticipant][i]);
                    }
                }
                if (controller.participantData[controller.currentParticipant][i].discretizedFixations.Count == 0) {
                    if (GUILayout.Button("Get Dis. Fix. Map")) {
                        controller.ManuallyGenerateDiscretizedFixationMap(controller.participantData[controller.currentParticipant][i]);
                    }
                } else {
                    if (GUILayout.Button("Show Dis. Fixation Map")) {
                        controller.ToggleDiscretizedFixationMap(controller.participantData[controller.currentParticipant][i]);
                    }
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

    /*
    public void OnSceneGUI ()  {
        StreetSimLoadSim controller = (StreetSimLoadSim)target;
        if (controller.directions.Count == 0 || !controller.visualizeSphere) return;
        for(int i = 0; i < controller.directions.Count; i++) {
            Vector3 dir = controller.directions[i];
            Vector3 pos = controller.cam360.position + dir*controller.sphereRadius;
            Gizmos.color = controller.directionColors[dir];
            Gizmos.DrawSphere(
                pos,                                      // position
                //controller.cam360.position - pos,      // normal
                controller.visualDistanceBetweenPoints
                //((2*Mathf.PI*controller.sphereRadius*(float)controller.sphereAngle)/360f)*0.5f*Mathf.Pow(2f,0.5f) // radius
            );
        }
    }
    */
}
