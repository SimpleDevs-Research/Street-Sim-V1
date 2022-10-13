using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StreetSim : MonoBehaviour
{

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
            InitializeTrial(trial);
        }
    }

    private void InitializeTrial(StreetSimTrial trial) {
        InitializeNPC(trial.modelPath, trial.modelBehavior);
        foreach(StreetSimModelPath npcPath in trial.npcPaths) {
            InitializeNPC(npcPath);
        }
    }

    private void InitializeNPC(StreetSimModelPath modelPath, StreetSimTrial.ModelBehavior behave = StreetSimTrial.ModelBehavior.Safe) {
        StreetSimAgent npc = Instantiate(modelPath.agent, modelPath.targets[0].position, modelPath.targets[0].rotation) as StreetSimAgent;
        npc.Initialize(modelPath.targets, behave, modelPath.shouldLoop, modelPath.shouldWarpOnLoop);
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
    public Transform[] targets;
    public bool shouldLoop, shouldWarpOnLoop;
}