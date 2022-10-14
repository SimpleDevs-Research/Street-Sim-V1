using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Helpers;
using SerializableTypes;

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
    public enum StreetSimStatus {
        Idle,
        Tracking,
    }

    [SerializeField] public OVRCameraRig cameraRigRef;

    [SerializeField, Tooltip("The folder where the simulation will save/load user tracking data. Omit the ending '/'")] private string m_sourceDirectory;
    public string sourceDirectory { get{ return m_sourceDirectory; } set{} }
    [SerializeField, Tooltip("The name of the participant. Format doesn't matter, but it must be one single string w/out spaces.")] private string m_participantName;
    public string participantName { get{ return m_participantName; } set{}}
    public string saveDirectory { get{ return m_sourceDirectory + "/" + m_participantName + "/"; } set{} }

    [SerializeField] private List<StreetSimTrial> m_trials = new List<StreetSimTrial>();
    private Queue<StreetSimTrial> m_trialQueue;
    [SerializeField] private TrialRotation m_trialRotation = TrialRotation.Randomized;
    public TrialRotation trialRotation { get{ return m_trialRotation; } set{} }

    [SerializeField] private StreetSimStatus m_streetSimStatus = StreetSimStatus.Idle;
    public StreetSimStatus streetSimStatus { get { return m_streetSimStatus; } set {} }
    [SerializeField] private float m_simulationStartTime = 0f, m_simulationEndTime = 0f, m_simulationDuration = 0f;
    [SerializeField] private float m_trialStartTime = 0f, m_trialEndTime = 0f, m_trialDuration = 0f;
    [SerializeField] private int m_trialFrameIndex = -1;
    public int trialFrameIndex { get { return m_trialFrameIndex; } set {} }
    [SerializeField] private float m_trialFrameTimestamp = -1f;
    private float m_prevTrialFrameTimestamp = -1f;
    public float trialFrameTimestamp { get{ return m_trialFrameTimestamp; } set {} }
    [SerializeField] private float m_trialFrameOffset = 0.01f;
    [SerializeField] private StreetSimTrial m_currentTrial;


    private void Awake() {
        S = this;
    }

    private void Start() {
        switch(trialRotation) {
            case TrialRotation.InOrder:
                m_trialQueue = new Queue<StreetSimTrial>(m_trials);
                break;
            case TrialRotation.Randomized:
                m_trialQueue = new Queue<StreetSimTrial>(m_trials.Shuffle<StreetSimTrial>());
                break;
        }
    }

    private IEnumerator InitializeTrial(StreetSimTrial trial) {
        InitializeNPC(trial.modelPath, trial.modelBehavior, true);
        foreach(StreetSimModelPath npcPath in trial.npcPaths) {
            PositionPlayerAtStart(trial.startPositionRef);
            InitializeNPC(npcPath);
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void PositionPlayerAtStart(Transform start) {
        cameraRigRef.transform.position = start.position;
        cameraRigRef.transform.rotation = start.rotation;
    }

    private void InitializeNPC(StreetSimModelPath modelPath, StreetSimTrial.ModelBehavior behave = StreetSimTrial.ModelBehavior.Safe, bool isModel = false) {
        NPCPath path;
        if (TargetPositioningVisualizer.current.GetPathFromName(modelPath.pathName, out path)) {
            Transform[] points;
            if (modelPath.reversePath) {
                points = new Transform[path.points.Length];
                for(int i = path.points.Length-1; i >= 0; i--) {
                    points[(path.points.Length-1)-i] = path.points[i];
                }
            }
            else {
                points = path.points;
            }
            StreetSimAgent npc = Instantiate(modelPath.agent, points[0].position, path.points[0].rotation) as StreetSimAgent;
            if (isModel) {
                // We need an extra step since this is a model...
                // We only render the model if there's an equivalent mesh running around
                if (StreetSimModelMapper.M.MapMeshToModel(npc)) {
                    npc.Initialize(points, behave, modelPath.shouldLoop, modelPath.shouldWarpOnLoop);
                }
            }  else {
                // Just initialize like normal
                npc.Initialize(points, behave, modelPath.shouldLoop, modelPath.shouldWarpOnLoop);
            }
        } else {
            Debug.Log("[StreetSim] ERROR: No path fits " + modelPath.pathName);
        }
    }

    public void StartSimulation() {
        // Set the start time
        m_simulationStartTime = Time.time;
        // Start the simulation, starting from the first trial in `m_trialQueue`.
        StartTrial();
    }
    public void EndSimulation(bool fromEditor = false) {
        if (fromEditor) {
            // We've stopped the simulation for some reason - so we need to end the trial
            m_trialQueue.Clear();
            EndTrial();
        } else {
            // Set the end time
            m_simulationEndTime = Time.time;
            // Calculate duration
            m_simulationDuration = m_simulationEndTime - m_simulationStartTime;
            
            // Save the data
            // === TO IMPLEMENT LATER ===
        }
    }   
    private void StartTrial() {
        // m_trialQueue is a Queue, so we just need to pop from the queue
        m_currentTrial = m_trialQueue.Dequeue();
        // Set up the trial
        StartCoroutine(InitializeTrial(m_currentTrial));
        // Get the start time of the trial
        m_trialStartTime = Time.time;
        // Reset frame index to -1 (it'll be advanecd to 0 at the first frame of recording)
        m_trialFrameIndex = -1;
        // Set the previous frame timestamp
        m_prevTrialFrameTimestamp = 0f;
        // Set status to "Tracking"
        m_streetSimStatus = StreetSimStatus.Tracking;
    }
    private void EndTrial() {
        // unset reference to current trial
        m_currentTrial = null;
        // Get the end time of the trial
        m_trialEndTime = Time.time;
        // Calculate the trial dureation
        m_trialDuration = m_trialStartTime - m_trialEndTime;
        // Set status to "Idle", which will stop tracking
        m_streetSimStatus = StreetSimStatus.Idle;

        // Save the trial data
        // === TO IMPLEMENT LATER ===

        // We now need to check if we have another trial or if we can end the simulation
        if (m_trialQueue.Count > 0) {
            // Continue to the next trial
            StartTrial();
        } else {
            // End the simulation
            EndSimulation();
        }
    }
    private void FixedUpdate() {
        switch(m_streetSimStatus) {
            case StreetSimStatus.Tracking:
                // Calculate frame timestamp
                m_trialFrameTimestamp = Time.time - m_trialStartTime;
                // Only track if we've surpassed the frame offset
                if (m_trialFrameTimestamp - m_prevTrialFrameTimestamp >= m_trialFrameOffset) {
                    // save the current timestamp to the previous one
                    m_prevTrialFrameTimestamp = m_trialFrameTimestamp;
                    // advance the frame by 1
                    m_trialFrameIndex += 1;
                    // Track GazeData
                    StreetSimRaycaster.R.CheckRaycast();
                }
                // We check if the user has reached the end or not.
                // === TO IMPLEMENT LATER ===
                break;
        }
        // No case for Idle...
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
    [Tooltip("Where should the player be at the start of this trial?")]
    public Transform startPositionRef;
    [Tooltip("Which target points should we consider that the person has successfully crossed the street?")]
    public Transform[] endPositionRefs;
}

[System.Serializable]
public class StreetSimModelPath {
    public StreetSimAgent agent;
    public string pathName;
    public bool reversePath = false, shouldLoop, shouldWarpOnLoop;
}