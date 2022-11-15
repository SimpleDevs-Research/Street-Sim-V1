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

        if(GUILayout.Button("Generate Test Groups")) {
            streetSim.GenerateTestGroups();
        }

        if(GUILayout.Button("Start Simulation")) {
            streetSim.StartSimulation();
        }

        if(GUILayout.Button("End Simulation")) {
            streetSim.EndSimulation(true);
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save")) {
            streetSim.SaveSimulationData();
        }
        if (GUILayout.Button("Load")) {

        }

        GUILayout.EndHorizontal();

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

}
