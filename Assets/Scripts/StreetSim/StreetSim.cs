using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Helpers;
using SerializableTypes;
using PathCreation;

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

    [SerializeField] private Transform xrTrackingSpace;
    [SerializeField] private Transform m_agentParent, m_agentMeshParent;
    public Transform agentParent { get { return m_agentParent; } set{} }
    public Transform agentMeshParent { get { return m_agentMeshParent; } set{} }

    [SerializeField, Tooltip("The folder where the simulation will save/load user tracking data. Omit the ending '/'")] private string m_sourceDirectory;
    public string sourceDirectory { get{ return m_sourceDirectory; } set{} }
    [SerializeField, Tooltip("The name of the participant. Format doesn't matter, but it must be one single string w/out spaces.")] private string m_participantName;
    public string participantName { get{ return m_participantName; } set{}}
    public string saveDirectory { get{ return m_sourceDirectory + "/" + simulationPayload.startTime + "_" + m_participantName + "/"; } set{} }

    [SerializeField] private List<StreetSimTrial> m_trials = new List<StreetSimTrial>();
    private Queue<StreetSimTrial> m_trialQueue;
    [SerializeField] private TrialRotation m_trialRotation = TrialRotation.Randomized;
    public TrialRotation trialRotation { get{ return m_trialRotation; } set{} }

    [SerializeField, Tooltip("NPC Behaviors")] private StreetSimModelPath[] npcPaths;
    [SerializeField, Tooltip("Car Behaviors")] private StreetSimCarPath[] carPaths;

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

    private SimulationData simulationPayload;
    private TrialData trialPayload;

    private void Awake() {
        S = this;
        if (m_agentParent == null) m_agentParent = this.transform;
        if (m_agentMeshParent == null) m_agentMeshParent = this.transform;
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
        yield return null;
        /*
        foreach(StreetSimModelPath npcPath in trial.npcPaths) {
            PositionPlayerAtStart(trial.startPositionRef);
            InitializeNPC(npcPath);
            yield return new WaitForSeconds(0.25f);
        }
        */
    }

    private void PositionPlayerAtStart(Transform start) {
        xrTrackingSpace.transform.position = start.position;
        xrTrackingSpace.transform.rotation = start.rotation;
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
            StreetSimAgent npc = Instantiate(modelPath.agent, points[0].position, path.points[0].rotation, agentParent) as StreetSimAgent;
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
        Debug.Log("[STREET SIM] Starting Simulation...");
        // Prepare the payload
        simulationPayload = new SimulationData(
            m_participantName,
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
        );
        // Prepare the save folder, and only continue if the folder was created
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        if (!SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Save folder based on source folder and/or participant name could not be created. Ending simulation prematurely without saivng.");
            return;
        }

        // Set the start time
        m_simulationStartTime = Time.time;
        // Start the simulation, starting from the first trial in `m_trialQueue`.
        StartTrial();
    }
    public void EndSimulation(bool fromEditor = false) {
        if (simulationPayload == null) {
            // We can't end a simulation that hasn't even been instantiated yet. So we return early
            Debug.Log("[STREET SIM] ERROR: Cannot end a simulaton that hasn't even started yet...");
            return;
        }
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
            simulationPayload.duration = m_simulationDuration;
        }
    }   
    private void StartTrial() {
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot start a trial for a simulation that isn't instantiated yet.");
            return;
        }
        // m_trialQueue is a Queue, so we just need to pop from the queue
        m_currentTrial = m_trialQueue.Dequeue();
        // Set up our trial payload
        trialPayload = new TrialData(
            m_currentTrial.name,
            m_currentTrial.modelPath.agent.GetComponent<ExperimentID>().id,
            m_currentTrial.modelBehavior.ToString(),
            m_currentTrial.modelPath.pathName,
            m_currentTrial.startPositionRef.GetComponent<ExperimentID>().id
        );
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
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot end a trial for a simulation that isn't instantiated yet.");
            return;
        }
        if (trialPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot end a trial that hasn't been started yet...");
            return;
        }
        // unset reference to current trial
        m_currentTrial = null;
        // Get the end time of the trial
        m_trialEndTime = Time.time;
        // Calculate the trial dureation
        m_trialDuration = m_trialStartTime - m_trialEndTime;

        // Save the trial data. Upon successful save, we add to our simulation payload to acknowledge the trial was saved.
        trialPayload.participantGazeData = StreetSimRaycaster.R.hits;
        trialPayload.duration = m_trialDuration;
        SaveTrialData();

        // Set status to "Idle", which will stop tracking
        m_streetSimStatus = StreetSimStatus.Idle;

        // We now need to check if we have another trial or if we can end the simulation
        if (m_trialQueue.Count > 0) {
            // Continue to the next trial
            StartTrial();
        } else {
            // End the simulation
            EndSimulation();
        }
    }
    private void Update() {
        /*
        switch(m_streetSimStatus) {
            case StreetSimStatus.Tracking:
                // We can actually run the cars now
                if (carPaths.Length > 0) {
                    // We need to manage our cars
                    foreach(StreetSimCarPath carPath in carPaths) {
                        switch(carPath.pathDensity) {
                            case StreetSimCarPath.StreetPathDensity.Sparse:
                                if()
                                break;
                            case StreetSimCarPath.StreetPathDensity.Congested:

                                break;
                        }
                    }
                }
                break;
        }
        */
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
                    //StreetSimRaycaster.R.CheckRaycast();
                }
                // We check if the user has reached the end or not.
                foreach(Transform endPosTransform in m_currentTrial.endPositionRefs) {
                    if (Vector3.Distance(xrTrackingSpace.transform.position,endPosTransform.position) < 0.05f) {
                        EndTrial();
                    }
                }
                break;
        }
        // No case for Idle...
    }

    public void SaveSimulationData() {
        // We can't save if there's no simulation data to begin with...
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot save if there is no simulation data to begin with.");
            return;
        }
        // We can't save if we're tracking! Have to end the simulation first
        if (m_streetSimStatus == StreetSimStatus.Tracking) {
            Debug.Log("[STREET SIM] ERROR: Cannot save while in the middle of tracking data.");
            return;
        }

        // We got our payload during the trial - so let's cut our losses here and save now.
        string dataToSave = SaveSystemMethods.ConvertToJSON<SimulationData>(simulationPayload);
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            if (SaveSystemMethods.SaveJSON(dirToSaveIn + "simulationMetadata", dataToSave)) {
                Debug.Log("[STREET SIM] Simulation Data Saved inside of " + saveDirectory);
            }
        }
    }
    public bool SaveTrialData() {
        if (simulationPayload == null || trialPayload == null) return false;
        string dataToSave = SaveSystemMethods.ConvertToJSON<TrialData>(trialPayload);
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            if (SaveSystemMethods.SaveJSON(dirToSaveIn + trialPayload.name, dataToSave)) {
                simulationPayload.trials.Add(trialPayload.name);
                return true;
            } else {
                Debug.Log("[STREET SIM] ERROR: Cannot save trial data for " + trialPayload.name);
                return false;
            }
        } else {
            Debug.Log("[STREET SIM] ERROR: Cannot check or create save directory when ending trial. Unable to save trial");
            return false;
        }
        return false;
    }
    public void LoadSimulationData() {

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
[System.Serializable]
public class StreetSimCarPath {
    public PathCreator path;
    public CarPathFollower[] carPrefabs;
    public enum StreetPathDensity {
        None,
        Sparse,
        Congested
    }
    public StreetPathDensity pathDensity = StreetPathDensity.Sparse;
    
    public List<CarPathFollower> activeCars = new List<CarPathFollower>();
}

[System.Serializable]
public class SimulationData {
    public string participantName;
    public string startTime;
    public float duration;
    public List<string> trials;
    
    public SimulationData(string pname, string startTime, float duration, List<string> trials) {
        this.participantName = pname;
        this.startTime = startTime;
        this.duration = duration;
        this.trials = trials;
    }
    public SimulationData(string pname, string startTime) {
        this.participantName = pname;
        this.startTime = startTime;
        this.trials = new List<string>();
    }
}
[System.Serializable]
public class TrialData {
    public string name;
    public string modelID;
    public string modelBehavior;
    public string modelPathName;
    public string participantStartPositionID;
    public float duration;
    public List<RaycastHitRow> participantGazeData;
    
    public TrialData(string name, string modelID, string modelBehavior, string modelPathName, string participantStartPositionID, float duration, List<RaycastHitRow> participantGazeData) {
        this.name = name;
        this.modelID = modelID;
        this.modelBehavior = modelBehavior;
        this.modelPathName = modelPathName;
        this.participantStartPositionID = participantStartPositionID;
        this.duration = duration;
        this.participantGazeData = participantGazeData;
    }
    public TrialData(string name, string modelID, string modelBehavior, string modelPathName, string participantStartPositionID) {
        this.name = name;
        this.modelID = modelID;
        this.modelBehavior = modelBehavior;
        this.modelPathName = modelPathName;
        this.participantStartPositionID = participantStartPositionID;
        this.participantGazeData = new List<RaycastHitRow>();
    }

}