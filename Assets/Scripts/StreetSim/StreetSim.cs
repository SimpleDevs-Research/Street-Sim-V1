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

    [SerializeField] private StreetSimTrial m_initialSetup;
    [SerializeField] private List<StreetSimTrial> m_trials = new List<StreetSimTrial>();
    private LinkedList<StreetSimTrial> m_trialQueue;
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

    private SimulationData simulationPayload;
    private TrialData trialPayload;
    [SerializeField] private TrialAttempt m_currentAttempt;
    private bool m_currentlyAttempting = false;
    [SerializeField] private LayerMask downwardMask;
    [SerializeField] private Transform[] roadTransforms;
    [SerializeField] private Transform m_southSidewalk,m_northSidewalk;
    [SerializeField] private Transform m_resetCube, m_nextCylinder;
    [SerializeField] private Transform m_southResetPoint, m_northResetPoint, m_southNextPoint, m_northNextPoint;
    private bool nextTrialTriggered = false;

    private void Awake() {
        S = this;
        if (m_agentParent == null) m_agentParent = this.transform;
        if (m_agentMeshParent == null) m_agentMeshParent = this.transform;
    }

    private void Start() {
        switch(trialRotation) {
            case TrialRotation.InOrder:
                m_trialQueue = new LinkedList<StreetSimTrial>(m_trials);
                break;
            case TrialRotation.Randomized:
                m_trialQueue = new LinkedList<StreetSimTrial>(m_trials.Shuffle<StreetSimTrial>());
                break;
        }
        m_trialQueue.AddFirst(m_initialSetup);
    }

    private IEnumerator InitializeTrial(StreetSimTrial trial) {
        //InitializeNPC(trial.modelPath, trial.modelBehavior, true);
        // Got to set the following:
        // Tracking space - place the player at the desired location
        // Agent - instantiate a model to follow a path with an associated behavior
        // NPCs - how congested should the NPCs be?
        // Traffic - how congested should the traffic be?
        // Save a ref to the model agent into our current trial data
        if (trial.startPositionRef != null) PositionPlayerAtStart(trial.startPositionRef);
        if (trial.agent != null) StreetSimAgentManager.AM.AddAgentManually(trial.agent, trial.modelPathIndex, trial.modelBehavior, true);
        StreetSimAgentManager.AM.SetCongestionStatus(trial.NPCCongestion, false);
        StreetSimCarManager.CM.SetCongestionStatus(trial.trafficCongestion, false);
        TrafficSignalController.current.StartAtSessionIndex(0);
        yield return null;
    }

    private void PositionPlayerAtStart(Transform start) {
        xrTrackingSpace.transform.position = start.position;
        // xrTrackingSpace.transform.rotation = start.rotation;
    }

    /*
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
    */

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
        // Enable the next cylinder
        m_nextCylinder.gameObject.SetActive(true);
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
            // Set the next cylinder to be deactivated
            m_nextCylinder.gameObject.SetActive(false);
            
            // Save the data
            simulationPayload.duration = m_simulationDuration;
            SaveSimulationData();
        }
    }
    private void StartTrial() {
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot start a trial for a simulation that isn't instantiated yet.");
            return;
        }
        StreetSimAgentManager.AM.DestroyModel();
        // m_trialQueue is a Queue, so we just need to pop from the queue
        m_currentTrial = m_trialQueue.First.Value;
        m_trialQueue.RemoveFirst();
        string agentID = (m_currentTrial.agent != null) ? m_currentTrial.agent.GetComponent<ExperimentID>().id : "No Agent";
        string startRef;
        if (m_currentTrial.direction == StreetSimTrial.TrialDirection.SouthToNorth) {
            m_currentTrial.SetSidewalks(m_southSidewalk, m_northSidewalk);
            m_resetCube.position = m_southResetPoint.position;
            m_nextCylinder.position = m_northNextPoint.position;
            startRef = "South";
        } else {
            m_currentTrial.SetSidewalks(m_northSidewalk, m_southSidewalk);
            m_resetCube.position = m_northResetPoint.position;
            m_nextCylinder.position = m_southNextPoint.position;
            startRef = "North";
        }
        // Set up our trial payload
        trialPayload = new TrialData(
            m_currentTrial.name,
            agentID,
            m_currentTrial.modelBehavior.ToString(),
            m_currentTrial.modelPathIndex,
            startRef
            //m_currentTrial.startPositionRef.GetComponent<ExperimentID>().id
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
        // Reset next trial triggered statuis
        nextTrialTriggered = false;
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
        trialPayload.positionData = StreetSimIDController.ID.payloads;
        trialPayload.duration = m_trialDuration;
        if (SaveTrialData()) {
            StreetSimRaycaster.R.ClearData();
            StreetSimIDController.ID.ClearData();
        }

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
    public void ResetTrial() {
        if (m_currentlyAttempting) {
            // Attempt ended in failure, reset
            m_currentAttempt.endTime = m_trialFrameTimestamp;
            m_currentAttempt.successful = false;
            m_currentAttempt.reason = "Vehicle Collision";
            trialPayload.attempts.Add(m_currentAttempt);
            m_currentlyAttempting = false;
            Debug.Log("Ending Attempt, Got hit by vehicle");
        }
        StreetSimAgentManager.AM.DestroyModel();
        StartCoroutine(InitializeTrial(m_currentTrial));
        xrTrackingSpace.transform.position = new Vector3(xrTrackingSpace.transform.position.x, 0f, xrTrackingSpace.transform.position.z);
    }
    public void TriggerNextTrial() {
        nextTrialTriggered = true;
        EndTrial();
    }

    private void FixedUpdate() {
        switch(m_streetSimStatus) {
            case StreetSimStatus.Tracking:
                // Don't do anything if we triggered the next trial.
                if (nextTrialTriggered) return;
                // Calculate frame timestamp
                m_trialFrameTimestamp = Time.time - m_trialStartTime;
                // Check the current attempt
                RaycastHit hit;
                if (Physics.Raycast(xrTrackingSpace.position, -Vector3.up, out hit, 3f, downwardMask)) {
                    if (Array.IndexOf(roadTransforms, hit.transform) > -1) {
                        if (!m_currentlyAttempting) {
                            // Create a new attempt
                            m_currentAttempt = new TrialAttempt(m_trialFrameTimestamp);
                            m_currentlyAttempting = true;
                        }
                    }
                    else if (hit.transform == m_currentTrial.GetStartSidewalk()) {
                        if (m_currentlyAttempting) {
                            // Attempt ended in failure, reset
                            m_currentAttempt.endTime = m_trialFrameTimestamp;
                            m_currentAttempt.successful = false;
                            m_currentAttempt.reason = "Returned to start sidewalk";
                            trialPayload.attempts.Add(m_currentAttempt);
                            m_currentlyAttempting = false;
                        }
                    }
                    else if (hit.transform == m_currentTrial.GetEndSidewalk()) {
                        if (m_currentlyAttempting) {
                            m_currentAttempt.endTime = m_trialFrameTimestamp;
                            m_currentAttempt.successful = true;
                            trialPayload.attempts.Add(m_currentAttempt);
                            m_currentlyAttempting = false;
                        }
                    }
                }
                // Only track if we've surpassed the frame offset
                if (m_trialFrameTimestamp - m_prevTrialFrameTimestamp >= m_trialFrameOffset) {
                    // save the current timestamp to the previous one
                    m_prevTrialFrameTimestamp = m_trialFrameTimestamp;
                    // advance the frame by 1
                    m_trialFrameIndex += 1;
                    // Track GazeData
                    StreetSimRaycaster.R.CheckRaycast();
                    // Track positional data
                    StreetSimIDController.ID.TrackPositions();
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
    public enum TrialDirection {
        SouthToNorth,
        NorthToSouth,
    }
    public enum ModelBehavior {
        Safe,
        Risky
    }
    [Tooltip("Name of the trial; must be unique from other trials.")] 
    public string name;
    [Tooltip("The agent prefab we'll be instantiating")]
    public StreetSimAgent agent;
    [Tooltip("Which path (via index) should we put the agent on?")]
    public int modelPathIndex;
    public ModelBehavior modelBehavior;
    [Tooltip("Where should the player be at the start of this trial?")]
    public Transform startPositionRef;
    [Tooltip("Which direction (North to South, or South to North) should the player move?")]
    public TrialDirection direction;
    [Tooltip("What should the congestion of the cars be?")]
    public StreetSimCarManager.CarManagerStatus trafficCongestion;
    [Tooltip("What should the the congestion of the pedestrians be?")]
    public StreetSimAgentManager.AgentManagerStatus NPCCongestion;

    private Transform startSidewalk, endSidewalk;
    public void SetSidewalks(Transform start, Transform end) {
        startSidewalk = start;
        endSidewalk = end;
    }
    public Transform GetStartSidewalk() {
        return startSidewalk;
    }
    public Transform GetEndSidewalk() {
        return endSidewalk;
    }
}

[System.Serializable]
public class StreetSimModelPath {
    public StreetSimAgent agent;
    public string pathName;
    public bool reversePath = false, shouldLoop, shouldWarpOnLoop;
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
    public int modelPathIndex;
    public string participantStartPositionID;
    public float duration;
    public List<TrialAttempt> attempts;
    public List<RaycastHitRow> participantGazeData;
    public List<StreetSimTrackablePayload> positionData;
    
    public TrialData(string name, string modelID, string modelBehavior, int modelPathIndex, string participantStartPositionID, float duration, List<RaycastHitRow> participantGazeData, List<StreetSimTrackablePayload> positionData) {
        this.name = name;
        this.modelID = modelID;
        this.modelBehavior = modelBehavior;
        this.modelPathIndex = modelPathIndex;
        this.participantStartPositionID = participantStartPositionID;
        this.duration = duration;
        this.participantGazeData = participantGazeData;
        this.positionData = positionData;
        this.attempts = new List<TrialAttempt>();
    }
    public TrialData(string name, string modelID, string modelBehavior, int modelPathIndex, string participantStartPositionID) {
        this.name = name;
        this.modelID = modelID;
        this.modelBehavior = modelBehavior;
        this.modelPathIndex = modelPathIndex;
        this.participantStartPositionID = participantStartPositionID;
        this.participantGazeData = new List<RaycastHitRow>();
        this.positionData = new List<StreetSimTrackablePayload>();
        this.attempts = new List<TrialAttempt>();
    }
}

[System.Serializable]
public class TrialAttempt {
    public float startTime;
    public float endTime;
    public bool successful;
    public string reason;
    public TrialAttempt(float startTime) {
        this.startTime = startTime;
    }
}