using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

public class StreetSim : MonoBehaviour
{
    #if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        if (TargetPositioningVisualizer.current == null || m_trials.Count == 0) return;
        NPCPath path;
        foreach(StreetSimTrial trial in m_trials) {
            if (TargetPositioningVisualizer.current.GetPathFromName(trial.modelPath.pathName, out path)) {
                Debug.Log("FOUND PATH");
                TargetPositioningVisualizer.DrawPath(path);
            }
        }
    }
    #endif

    public static StreetSim S;
    public enum TrialRotation {
        InOrder,
        Randomized
    }

    [SerializeField, Tooltip("The folder where the simulation will save/load user tracking data. Omit the ending '/'")] private string m_sourceDirectory;
    public string sourceDirectory { get{ return m_sourceDirectory; } set{} }
    [SerializeField, Tooltip("The name of the participant. Format doesn't matter, but it must be one single string w/out spaces.")] private string m_participantName;
    public string participantName { get{ return m_participantName; } set{}}
    public string saveDirectory { get{ return m_sourceDirectory + "/" + m_participantName + "/"; } set{} }

    [SerializeField] private List<StreetSimTrial> m_trials = new List<StreetSimTrial>();
    public List<StreetSimTrial> trials { get{ return m_trials; } set{} }
    [SerializeField] private TrialRotation m_trialRotation = TrialRotation.Randomized;
    public TrialRotation trialRotation { get{ return m_trialRotation; } set{} }

    private void Awake() {
        S = this;
    }

    private void Start() {
        foreach(StreetSimTrial trial in m_trials) {
            StartCoroutine(InitializeTrial(trial));
        }
    }

    private IEnumerator InitializeTrial(StreetSimTrial trial) {
        InitializeNPC(trial.modelPath, trial.modelBehavior);
        foreach(StreetSimModelPath npcPath in trial.npcPaths) {
            InitializeNPC(npcPath);
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void InitializeNPC(StreetSimModelPath modelPath, StreetSimTrial.ModelBehavior behave = StreetSimTrial.ModelBehavior.Safe) {
        NPCPath path;
        if (TargetPositioningVisualizer.current.GetPathFromName(modelPath.pathName, out path)) {
            Transform[] points;
            if (modelPath.reversePath) {
                points = new Transform[path.points.Length];
                for(int i = path.points.Length-1; i >= 0; i--) {
                    points[(path.points.Length-1)-i] = path.points[i];
                }
            } else {
                points = path.points;
            }
            StreetSimAgent npc = Instantiate(modelPath.agent, points[0].position, path.points[0].rotation) as StreetSimAgent;
            npc.Initialize(points, behave, modelPath.shouldLoop, modelPath.shouldWarpOnLoop);
        } else {
            Debug.Log("[StreetSim] ERROR: No path fits " + modelPath.pathName);
        }
    }
}

[System.Serializable]
public class StreetSimTrial {
    public enum ModelBehavior {
        Safe,
        Risky
    }
    [Tooltip("Name of the trial; must be unique from other trials.")] 
    public string name;
    [Tooltip("The Model's Path")] 
    public StreetSimModelPath modelPath;
    [Tooltip("How should the model behave regarding crossing?")]
    public ModelBehavior modelBehavior;
    [Tooltip("NPC Behaviors")]
    public StreetSimModelPath[] npcPaths;
}

[System.Serializable]
public class StreetSimModelPath {
    public StreetSimAgent agent;
    public string pathName;
    public bool reversePath = false, shouldLoop, shouldWarpOnLoop;
}