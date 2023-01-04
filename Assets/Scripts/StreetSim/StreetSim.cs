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
    public enum StreetSimVersion {
        Version1,
        Version2,
        Version3
    }

    // References to the participant's components
    public Transform xrTrackingSpace;
    public Transform xrCamera;
    public ExperimentID xrExperimentID;
    public Collider xrCollider;
    public Transform GazeBox;
    public Transform replayCamera;
    public LayerMask replayGazeMask;

    [SerializeField] private bool m_startSimulationOnRun = true;
    public bool startSimulationOnRun { get=>m_startSimulationOnRun; set{} }
    private bool m_initialized = false;
    public bool initialized { get=>m_initialized; set{} }
    private bool m_isRunning = false;
    public bool isRunning { get=>m_isRunning; set{} }
    [SerializeField] private bool m_endGameOnSimulationEnd = true; 

    [SerializeField] private AudioSource m_successAudioSource;
    [SerializeField] private Transform m_agentParent, m_agentMeshParent;
    public Transform agentParent { get { return m_agentParent; } set{} }
    public Transform agentMeshParent { get { return m_agentMeshParent; } set{} }

    [SerializeField, Tooltip("The folder where the simulation will save/load user tracking data. Omit the ending '/'")] private string m_sourceDirectory;
    public string sourceDirectory { get{ return m_sourceDirectory; } set{} }
    [SerializeField, Tooltip("The name of the participant. Format doesn't matter, but it must be one single string w/out spaces.")] private string m_participantName;
    [SerializeField] private int m_trialGroupToTest = 0;
    public string participantName { get{ return m_participantName; } set{}}
    public StreetSimVersion streetSimVersion = StreetSimVersion.Version3;
    public string saveDirectory { get{ return m_sourceDirectory + "/" + m_participantName + "/"; } set{} }
    public string resourcesDirectory { get=>"Resources/"+m_participantName+"/"; set{} }
    private string simulationDirToSaveIn, attemptsDirToSaveIn, positionsDirToSaveIn, trialDirToSaveIn;

    [SerializeField] private StreetSimTrial[] m_initialSetups;
    [SerializeField] private bool m_includeInitialSetup = true;
    [SerializeField] private List<StreetSimTrial> m_trials = new List<StreetSimTrial>();
    [SerializeField] private List<StreetSimTrial> m_trials2 = new List<StreetSimTrial>();
    private LinkedList<StreetSimTrial> m_trialQueue;
    [SerializeField] private TrialRotation m_trialRotation = TrialRotation.Randomized;
    public TrialRotation trialRotation { get{ return m_trialRotation; } set{} }

    [SerializeField] private int numTrialsPerGroups = 10;
    [SerializeField] private List<StreetSimTrialGroup> m_trialGroups = new List<StreetSimTrialGroup>();
    public List<StreetSimTrialGroup> trialGroups { get=>m_trialGroups; set{} }

    [SerializeField] private StreetSimStatus m_streetSimStatus = StreetSimStatus.Idle;
    public StreetSimStatus streetSimStatus { get { return m_streetSimStatus; } set {} }
    [SerializeField] private float m_simulationStartTime = 0f, m_simulationEndTime = 0f, m_simulationDuration = 0f;
    [SerializeField] private float m_trialEndTime = 0f, m_trialDuration = 0f;
    [SerializeField] private int m_trialFrameIndex = -1;
    public int trialFrameIndex { get { return m_trialFrameIndex; } set {} }
    [SerializeField] private float m_trialFrameTimestamp = -1f;
    private float m_prevTrialFrameTimestamp = -1f;
    public float trialFrameTimestamp { get{ return m_trialFrameTimestamp; } set {} }
    [SerializeField] private float m_trialFrameOffset = 0.01f;
    [SerializeField] private StreetSimTrial m_currentTrial;
    private bool m_currentTrialActive = false;

    // All the payloads that will be saved
    private SimulationData simulationPayload;   // Simulation payload itself
    private TrialData trialPayload;             // payload for the current trial
    private Dictionary<ExperimentID, List<TrialAttempt>> trialAttempts = new Dictionary<ExperimentID, List<TrialAttempt>>();        // All attempts performed by both participant and models in each trial
    private List<RaycastHitRow> participantGazeData;        // The participant's gaze data
    // Temporary data
    private Dictionary<ExperimentID, TrialAttempt> m_currentAttempts = new Dictionary<ExperimentID, TrialAttempt>();    // List of all current attempts of both the particiapnt and any models in the trial

    private IEnumerator m_customUpdateCoroutine = null;
    //[SerializeField] private TrialAttempt m_modelCurrentAttempt;
    //private bool m_modelCurrentlyAttempting = false;
    
    // References to things in the virtual simulation
    [SerializeField] private LayerMask downwardMask;
    [SerializeField] private Transform[] roadTransforms;
    [SerializeField] private Transform m_southSidewalk,m_northSidewalk;
    [SerializeField] private Transform m_resetCube, m_nextCylinder;
    [SerializeField] private Transform m_southResetPoint, m_northResetPoint, m_southNextPoint, m_northNextPoint;
    private int layerCrosswalk;
    
    // Trial Shenangigans
    private bool nextTrialTriggered = false;
    private int trialNumber = -1;
    [SerializeField] private List<LoadedSimulationDataPerTrial> m_loadedTrials = new List<LoadedSimulationDataPerTrial>();
    public List<LoadedSimulationDataPerTrial> loadedTrials { get=>m_loadedTrials; set{} }

    public void GenerateTestGroups() {
        // Generate total list of trials
        //List<StreetSimTrial> roundOne, roundTwo;
        List<StreetSimTrial> roundOne;
        switch(trialRotation) {
            case TrialRotation.Randomized:
                roundOne = m_trials2.Shuffle<StreetSimTrial>();
                //roundTwo = m_trials.Shuffle<StreetSimTrial>();
                break;
            default:
                roundOne = m_trials2; 
                //roundTwo = m_trials;
                break;
        }
        //roundOne.AddRange(roundTwo);
        List<StreetSimTrial> newTrials = new List<StreetSimTrial>(roundOne);

        // Generate groups
        m_trialGroups = new List<StreetSimTrialGroup>();
        int numGroups = (int)Mathf.Ceil((float)newTrials.Count / (float)numTrialsPerGroups);
        for(int i = 0; i < numGroups; i++) {
            m_trialGroups.Add(new StreetSimTrialGroup(i));
        }

        // Now separate based on # of m_numTrialGroups
        for(int i = 0; i < newTrials.Count; i++) {
            int groupIndex = i / numTrialsPerGroups;
            m_trialGroups[groupIndex].trials.Add(newTrials[i]);
        }
    }

    private void Awake() {
        S = this;
        if (m_agentParent == null) m_agentParent = this.transform;
        if (m_agentMeshParent == null) m_agentMeshParent = this.transform;
        layerCrosswalk = LayerMask.NameToLayer("Crosswalk");
    }
    private void Start() {
        m_trialQueue = new LinkedList<StreetSimTrial>(m_trialGroups[m_trialGroupToTest].trials);
        if (m_trialGroupToTest == 0) trialNumber = 0;
        else {
            trialNumber = 0;
            for(int i = 0; i < m_trialGroupToTest; i++) {
                trialNumber += m_trialGroups[i].trials.Count;
                if (m_includeInitialSetup) trialNumber += 3;
            }  
        }
        //trialNumber = m_trialGroups[m_trialGroupToTest].groupNumber * numTrialsPerGroups;
        if (m_trialGroupToTest == 0 || m_includeInitialSetup) {
            for(int i = m_initialSetups.Length-1; i >= 0; i--) {
                StreetSimTrial t = m_initialSetups[i];
                t.name = t.name + "_group" + m_trialGroupToTest;
                m_trialQueue.AddFirst(t);
            }
        }
        m_trialQueue.First.Value.isFirstTrial = true;
        if (m_startSimulationOnRun) StartSimulation();
        m_initialized = true;
    }

    private void InitializeTrial(StreetSimTrial trial) {
        //InitializeNPC(trial.modelPath, trial.modelBehavior, true);
        // Got to set the following:
        // Tracking space - place the player at the desired location
        // Agent - instantiate a model to follow a path with an associated behavior
        // NPCs - how congested should the NPCs be?
        // Traffic - how congested should the traffic be?
        // Tell our trial payload that we're starting another trial
        PositionPlayerAtStart();
        if(!trialAttempts.ContainsKey(xrExperimentID)) trialAttempts.Add(xrExperimentID, new List<TrialAttempt>());
        foreach(StreetSimTrial.StreetSimPrimaryModel model in trial.models) {
            ExperimentID modelID = model.agent.GetComponent<ExperimentID>();
            StreetSimAgentManager.AM.AddAgentManually(model.agent, model.modelPathIndex, model.modelBehavior, model.speed, true, model.direction, trial.modelConfidence);
            if(!trialAttempts.ContainsKey(modelID)) trialAttempts.Add(modelID, new List<TrialAttempt>());
        }
        //if (trial.primaryModel.agent != null) StreetSimAgentManager.AM.AddAgentManually(trial.primaryModel.agent, trial.primaryModel.modelPathIndex, trial.primaryModel.modelBehavior, true);
        StreetSimAgentManager.AM.SetCongestionStatus(trial.npcCongestion, false);
        StreetSimCarManager.CM.SetCongestionStatus(trial.trafficCongestion, false);
        TrafficSignalController.current.SetDurationOfSession(0,trial.carSignalGoTime);
        TrafficSignalController.current.SetDurationOfSession(1,trial.carSignalWarningTime);
        TrafficSignalController.current.StartAtSessionIndex(0);
        m_currentTrial.AddInnerStartTime(m_trialFrameTimestamp);
    }

    private void PositionPlayerAtStart() {
        xrTrackingSpace.position = Vector3.zero;
        AudioListener.volume = 1;
        xrCollider.enabled = true;
    }

    public void StartSimulation() {
        Debug.Log("[STREET SIM] Starting Simulation...");
        // Prepare the payload
        simulationPayload = new SimulationData(
            m_participantName,
            m_trialGroupToTest,
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            streetSimVersion.ToString()
        );
        // Prepare the save folder for the simulation, and only continue if the folder was created
        simulationDirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        if (!SaveSystemMethods.CheckOrCreateDirectory(simulationDirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Save folder based on source folder and/or participant name could not be created. Ending simulation prematurely without saivng.");
            return;
        }

        // Set the start time
        m_simulationStartTime = Time.time;
        // Enable the next cylinder, but only for this first trial. After that, never use it again.
        //                                                                                      m_nextCylinder.gameObject.SetActive(true);
        // Declare that we're running
        m_isRunning = true;
        // Reset the gaze box
        /*
        GazeBox.position = new Vector3(0f, -20f + Cam360.position.y, 0f);
        GazeBox.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");
        foreach(Transform child in GazeBox) {
            child.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");
        }
        */
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
            //                                                                                  m_nextCylinder.gameObject.SetActive(false);
            // Indicate that we're ending
            m_isRunning = false;
            // Save the data
            simulationPayload.duration = m_simulationDuration;
            if (SaveSimulationData()) {
                // End the game if we're indicated by our trusty boolean.
                if (m_endGameOnSimulationEnd) {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                }
            }
        }
    }
    private void StartTrial() {
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot start a trial for a simulation that isn't instantiated yet.");
            return;
        }
        
        // Destroy the current models if they exist
        StreetSimAgentManager.AM.DestroyModels();

        // m_trialQueue is a LinkedList, so we just need to pop from the list
        m_currentTrial = m_trialQueue.First.Value;
        m_trialQueue.RemoveFirst();
        m_nextCylinder.gameObject.SetActive(m_currentTrial.isFirstTrial);
        
        // We don't continue until we create our trial folder
        trialDirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory + m_currentTrial.name + "/");
        if (!SaveSystemMethods.CheckOrCreateDirectory(trialDirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Save folder for trial could not be created for the current trial. Ending simulation prematurely without saving");
            EndSimulation();
            return;
        }
        /*
        // Prepare the save folder for the attempts data, and only continue if the folder was created
        attemptsDirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory + m_currentTrial.name + "/attempts/");
        if (!SaveSystemMethods.CheckOrCreateDirectory(attemptsDirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Save folder for attempts could not be created for the current trial. Ending simulation prematurely without saving.");
            EndSimulation();
            return;
        }
        */
        /*
        // Prepare the save folder for the positions data, and only continue if the folder was created
        positionsDirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory + m_currentTrial.name + "/positions/");
        if (!SaveSystemMethods.CheckOrCreateDirectory(positionsDirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Save folder for positions could not be created. Ending simulation prematurely without saving.");
            EndSimulation();
            return;
        }
        */

        // Determine the positions of important things inside the simulation. Also determine the direction the trial moves in
        if (m_currentTrial.isFirstTrial) {
            // Normally, the first trial should be from NORTH to SOUTH. We'll set that accordingly
            m_currentTrial.startSidewalk = m_northSidewalk;
            m_currentTrial.endSidewalk = m_southSidewalk;
            m_resetCube.position = m_northResetPoint.position;
            m_nextCylinder.position = m_southNextPoint.position;
            m_currentTrial.direction = StreetSimTrial.TrialDirection.NorthToSouth;
        } else if (xrCamera.position.z > 0f) {
            // The player is currently NORTH. So we switch the destination to South. Direction is [N -> S]
            m_currentTrial.startSidewalk = m_northSidewalk;
            m_currentTrial.endSidewalk = m_southSidewalk;
            m_resetCube.position = m_northResetPoint.position;
            //                                                                                                  m_nextCylinder.position = m_southNextPoint.position;
            m_currentTrial.direction = StreetSimTrial.TrialDirection.NorthToSouth;
        } else {
            // The player is currently SOUTH. So we switch the destination to North. Direction is [S -> N]
            m_currentTrial.startSidewalk = m_southSidewalk;
            m_currentTrial.endSidewalk = m_northSidewalk;
            m_resetCube.position = m_southResetPoint.position;
            //                                                                                                  m_nextCylinder.position = m_northNextPoint.position;
            m_currentTrial.direction = StreetSimTrial.TrialDirection.SouthToNorth;
        }
        // Clear `m_trialTrackables` in StreetSimIDController
        StreetSimIDController.ID.ClearTrialTrackables();
        // Set model stuff
        foreach(StreetSimTrial.StreetSimPrimaryModel model in m_currentTrial.models) {
            // Determine model direction
            model.direction = (model.startOnSameSideAsPlayer) 
                ? (m_currentTrial.direction == StreetSimTrial.TrialDirection.NorthToSouth) 
                    ? StreetSimTrial.TrialDirection.NorthToSouth 
                    : StreetSimTrial.TrialDirection.SouthToNorth
                : (m_currentTrial.direction == StreetSimTrial.TrialDirection.NorthToSouth)
                    ? StreetSimTrial.TrialDirection.SouthToNorth
                    : StreetSimTrial.TrialDirection.NorthToSouth;
            // Determine model seed
            model.speed = UnityEngine.Random.Range(0.4f,0.5f);
            // Add model to `m_trialTrackables` in StreetSimIDController
            StreetSimIDController.ID.AddTrialTrackable(model.agent.GetID());
        }

        // Set up the trial number
        trialNumber += 1;
        m_currentTrial.trialNumber = trialNumber;
        
        // Get the start time of the trial
        m_currentTrial.startTime = Time.time;

        // Reset frame index to -1 (it'll be advanecd to 0 at the first frame of recording)
        m_trialFrameIndex = -1;
        // Set the current and previous frame timestamps
        m_trialFrameTimestamp = 0f;
        //m_prevTrialFrameTimestamp = 0f;
        // Set up the trial
        InitializeTrial(m_currentTrial);
        // Set status to "Tracking"
        m_streetSimStatus = StreetSimStatus.Tracking;
        // Reset next trial triggered status
        nextTrialTriggered = false;
        // Finally declare that we're starting the trial
        m_currentTrialActive = true;
        Debug.Log("PERFORMING TRIAL # " + m_currentTrial.trialNumber.ToString());
        if (m_customUpdateCoroutine == null) {
            m_customUpdateCoroutine = CustomUpdate();
            StartCoroutine(m_customUpdateCoroutine);
        }

        // Add the simulation to our simulation payload
        simulationPayload.trials.Add(m_currentTrial.name);
    }
    private void EndTrial() {
        // Do not continue if our data is weird
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot end a trial for a simulation that isn't instantiated yet.");
            return;
        }
        if (!m_currentTrialActive) {
            Debug.Log("[STREET SIM] ERROR: Cannot end a trial that hasn't been started yet...");
            return;
        }

        // Get timings for the trial
        m_currentTrial.endTime = Time.time;
        m_currentTrial.duration = m_currentTrial.endTime - m_currentTrial.startTime;
        m_currentTrial.numFrames = m_trialFrameIndex;

        // End all attempts
        foreach(KeyValuePair<ExperimentID,TrialAttempt> kvp in m_currentAttempts) {
            EndAttempt(kvp.Key, m_trialFrameTimestamp, false, true, "");
        }

        //Debug.Log(trialAttempts);
        // Save the trial data. Upon successful save, we add to our simulation payload to acknowledge the trial was saved.
        if (!SaveTrialData(m_currentTrial, trialAttempts)) {
            Debug.Log("[STREET SIM] ERROR: Could not save trial data. Boo-hoo...");
        }

        // Let's reset all things
        m_currentAttempts = new Dictionary<ExperimentID, TrialAttempt>();
        trialAttempts = new Dictionary<ExperimentID, List<TrialAttempt>>();
        StreetSimRaycaster.R.ClearData();
        StreetSimIDController.ID.ClearData();
        StreetSimCarManager.CM.ClearData();
        StreetSimProximityTracker.PT.ClearData();

        // Let the system know we're no longer having a an active trial
        m_currentTrialActive = false;

        // Set status to "Idle", which will stop tracking. This should also end the coroutine;
        m_streetSimStatus = StreetSimStatus.Idle;
        // End the coroutine in case
        if (m_customUpdateCoroutine != null) {
            StopCoroutine(m_customUpdateCoroutine);
            m_customUpdateCoroutine = null;
        }

        // Play success sound
        m_successAudioSource.Play();

        // We now need to check if we have another trial or if we can end the simulation
        if (m_trialQueue.Count > 0) {
            // Continue to the next trial
            StartTrial();
        } else {
            // End the simulation
            EndSimulation();
        }
    }
    public void FailTrial() {
        // Reposition the player into the fail room
        xrTrackingSpace.position = new Vector3(0f, -6f, 0f);
        // Silence any audio
        AudioListener.volume = 0;
        
        // End the player's attempt
        float endTime = m_trialFrameTimestamp;
        EndAttempt(xrExperimentID, endTime, true, false, "Vehicle Collision");
        if (m_currentAttempts.Count > 0) {
            foreach(ExperimentID key in m_currentAttempts.Keys) {
                EndAttempt(key, endTime, false, false, "Vehicle Collision");
            }
        }
        m_currentAttempts = new Dictionary<ExperimentID, TrialAttempt>();
        Debug.Log("[STREET SIM] FAILED TRIAL...");
    }
    public void ResetTrial() {
        Debug.Log("[STREET SIM] Resetting Trial...");

        float endTime = m_trialFrameTimestamp;
        EndAttempt(xrExperimentID, endTime, true, false, "Vehicle Collision");
        if(m_currentAttempts.Count > 0) {
            foreach(ExperimentID key in m_currentAttempts.Keys) {
                EndAttempt(key, endTime, false, false, "Vehicle Collision");
            }
        }
        m_currentAttempts = new Dictionary<ExperimentID, TrialAttempt>();

        StreetSimAgentManager.AM.DestroyModels();
        InitializeTrial(m_currentTrial);
        //xrCamera.transform.position = new Vector3(xrCamera.transform.position.x, 0f, xrCamera.transform.position.z);
    }
    public IEnumerator TriggerNextTrialCoroutine() {
        nextTrialTriggered = true;
        xrCollider.enabled = false;
        yield return new WaitForSeconds(1f);
        TriggerNextTrial();
    }
    public void TriggerNextTrial() {
        nextTrialTriggered = true;
        EndTrial();
    }

    /*
    public void StartAgentAttempt() {
        if (!m_modelCurrentlyAttempting) {
            m_modelCurrentAttempt = new TrialAttempt(Time.time);
            m_modelCurrentlyAttempting = true;
        }
    }
    public void EndAgentAttempt() {
        if (m_modelCurrentlyAttempting) {
            m_modelCurrentAttempt.endTime = Time.time;
            m_modelCurrentAttempt.successful = true;
            m_modelCurrentAttempt.reason = "";
            trialPayload.modelAttempts.Add(m_modelCurrentAttempt);
            m_modelCurrentlyAttempting = false;
        }
    }
    */

    public void StartAttempt(ExperimentID id, float startTime, StreetSimTrial.TrialDirection direction, bool shouldSetStartingAttempt = true) {
        if (m_currentAttempts.ContainsKey(id)) return;
        m_currentAttempts.Add(id, new TrialAttempt(id.id, direction.ToString(), startTime));
        Debug.Log("STARTING ATTEMPT FOR " + id.id);
    }
    public void EndAttempt(ExperimentID id, float endTime, bool shouldSetEndingAttempt, bool successful = true, string reason = "") {
        if (!m_currentAttempts.ContainsKey(id)) return;
        TrialAttempt cAttempt = m_currentAttempts[id];
        cAttempt.endTime = endTime;
        cAttempt.successful = successful;
        cAttempt.reason = reason;
        trialAttempts[id].Add(cAttempt);
        if (shouldSetEndingAttempt) m_currentAttempts.Remove(id);
        Debug.Log("ENDING ATTEMPT FOR " + id.id);
    }

    private IEnumerator CustomUpdate() {
        // We only track if we've declared taht we're tracking
        while(m_streetSimStatus == StreetSimStatus.Tracking) {
            // If the next trial is triggered, we break early and stop tracking
            if (nextTrialTriggered) break;
            // Calculate frame timestamp
            m_trialFrameTimestamp = Time.time - m_currentTrial.startTime;
            // Check the current attempt
            RaycastHit hit;
            if (Physics.Raycast(xrCamera.position+(Vector3.up*0.1f), -Vector3.up, out hit, 3f, downwardMask)) {
                //if (Array.IndexOf(roadTransforms, hit.transform) > -1) {
                if (hit.transform.gameObject.layer == layerCrosswalk) {
                    // Create a new attempt if it doesn't exist already
                    if (!m_currentAttempts.ContainsKey(xrExperimentID)) StartAttempt(xrExperimentID, m_trialFrameTimestamp, m_currentTrial.direction);
                }
                else if (hit.transform == m_currentTrial.startSidewalk) {
                    // Attempt ended in failure due to returning back to the original sidewalk
                    if (m_currentAttempts.ContainsKey(xrExperimentID)) EndAttempt(xrExperimentID,m_trialFrameTimestamp, true, false, "Returned to start sidewalk"); 
                }
                else if (hit.transform == m_currentTrial.endSidewalk) {
                    // We reached the end successfully! Let's add a successful attempt, then Coroutine for 1 second to the next trial
                    if (m_currentAttempts.ContainsKey(xrExperimentID)) EndAttempt(xrExperimentID,m_trialFrameTimestamp,true,true,"Reached the other sidewalk successfully");
                    if (!nextTrialTriggered && !m_currentTrial.isFirstTrial) StartCoroutine(TriggerNextTrialCoroutine());
                }
            }
            //if (m_trialFrameTimestamp - m_prevTrialFrameTimestamp >= m_trialFrameOffset) {
            // save the current timestamp to the previous one
            //m_prevTrialFrameTimestamp = m_trialFrameTimestamp;
            // advance the frame by 1
            m_trialFrameIndex += 1;
            // Track GazeData
            StreetSimRaycaster.R.CheckRaycast();
            // Track positional data
            StreetSimIDController.ID.TrackPositions();
            // Track Proximity data
            StreetSimProximityTracker.PT.CheckProximity();
            //}
            yield return new WaitForSeconds(m_trialFrameOffset);
        }
        Debug.Log("Ending Tracking");
        yield return null;
    }

    public bool SaveSimulationData() {
        // We can't save if there's no simulation data to begin with...
        if (simulationPayload == null) {
            Debug.Log("[STREET SIM] ERROR: Cannot save if there is no simulation data to begin with.");
            return false;
        }
        // We can't save if we're tracking! Have to end the simulation first
        if (m_streetSimStatus == StreetSimStatus.Tracking) {
            Debug.Log("[STREET SIM] ERROR: Cannot save while in the middle of tracking data.");
            return false;
        }

        // We got our payload during the trial - so let's cut our losses here and save now.
        string dataToSave = SaveSystemMethods.ConvertToJSON<SimulationData>(simulationPayload);
        //string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        //if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
        if (!SaveSystemMethods.CheckOrCreateDirectory(simulationDirToSaveIn)) {
            Debug.Log("[STREET SIM] ERROR: Cannot check if the directory for the simulation data exists or not...");
            return false;
        }
        if (SaveSystemMethods.SaveJSON(simulationDirToSaveIn + "simulationMetadata_" + simulationPayload.simulationGroupNumber, dataToSave)) {
            Debug.Log("[STREET SIM] Simulation Data Saved inside of " + saveDirectory);
            return true;
        } else {
            Debug.Log("[STREET SIM] ERROR: Could not save simulation data into " + saveDirectory);
            return false;
        }
        /*
        if (SaveSystemMethods.CheckOrCreateDirectory(simulationDirToSaveIn)) {
            if (SaveSystemMethods.SaveJSON(simulationDirToSaveIn + "simulationMetadata_" + simulationPayload.simulationGroupNumber, dataToSave)) {
                Debug.Log("[STREET SIM] Simulation Data Saved inside of " + saveDirectory);
            }
        }
        */
    }
    public bool SaveTrialData(StreetSimTrial trial, Dictionary<ExperimentID,List<TrialAttempt>> attempts) {
        if (simulationPayload == null) return false;

        // We need to save several files
        // 1) the trial metadata, which is stored inside of `trialDirToSaveIn`
        // 2) the trial attempt data - each individual model and participant gets their own file. We store them inside of `attemptsDirToSaveIn`
        // 3) the gaze data `gaze.json`, which is stored inside of `trialDirToSaveIn`
        // 4) the positional data - each individual trackedExperimentID gets their own file. We store them inside of `positionsDirToSaveIn`

        // FIRST: The trial metadata
        string dataToSave = SaveSystemMethods.ConvertToJSON<TrialData>(trial.GetTrialData());
        Debug.Log(dataToSave);
        Debug.Log(trialDirToSaveIn);
        if(SaveSystemMethods.SaveJSON(trialDirToSaveIn+"trial",dataToSave)) {
        //if(SaveSystemMethods.SaveJSON(trialDirToSaveIn+"trial",dataToSave)) {
            // SECOND: the trial attempt data
            /*
            foreach(KeyValuePair<ExperimentID,List<TrialAttempt>> kvp in attempts) {
                AttemptsPayload attemptsPayload = new AttemptsPayload(kvp.Key.id,kvp.Value);
                dataToSave = SaveSystemMethods.ConvertToJSON<AttemptsPayload>(attemptsPayload);
                if (!SaveSystemMethods.SaveJSON(attemptsDirToSaveIn+kvp.Key.id,dataToSave)) {
                    Debug.Log("SOMETHING WRONG");
                };
            }
            */
            SaveSystemMethods.SaveCSV<TrialAttempt>(trialDirToSaveIn+"attempts",TrialAttempt.Headers,new List<List<TrialAttempt>>(attempts.Values).Flatten2D<TrialAttempt>());
            // THIRD: The gaze data
            GazePayload gazePayload = new GazePayload(StreetSimRaycaster.R.hits);
            dataToSave = SaveSystemMethods.ConvertToJSON<GazePayload>(gazePayload);
            SaveSystemMethods.SaveJSON(trialDirToSaveIn+"gaze",dataToSave);
            SaveSystemMethods.SaveCSV<RaycastHitRow>(trialDirToSaveIn+"gaze",RaycastHitRow.Headers,StreetSimRaycaster.R.hits);
            // FOURTH: The positional data
            /*
            foreach(KeyValuePair<ExperimentID,List<StreetSimTrackable>> kvp in StreetSimIDController.ID.payloads) {
                PositionsPayload positionsPayload = new PositionsPayload(kvp.Key.id,kvp.Value);
                dataToSave = SaveSystemMethods.ConvertToJSON<PositionsPayload>(positionsPayload);
                SaveSystemMethods.SaveJSON(positionsDirToSaveIn+kvp.Key.id,dataToSave);
                SaveSystemMethods.SaveCSV<StreetSimTrackable>(positionsDirToSaveIn+kvp.Key.id,StreetSimTrackable.Headers,kvp.Value);
            }
            */
            SaveSystemMethods.SaveCSV<StreetSimTrackable>(trialDirToSaveIn+"positions",StreetSimTrackable.Headers,new List<List<StreetSimTrackable>>(StreetSimIDController.ID.payloads.Values).Flatten2D<StreetSimTrackable>());
            // FIFTH: The car history data
            CarsPayload carsPayload = new CarsPayload(StreetSimCarManager.CM.carHistory);
            dataToSave = SaveSystemMethods.ConvertToJSON<CarsPayload>(carsPayload);
            SaveSystemMethods.SaveJSON(trialDirToSaveIn+"cars",dataToSave);
            SaveSystemMethods.SaveCSV<CarRow>(trialDirToSaveIn+"cars",CarRow.Headers,StreetSimCarManager.CM.carHistory);
            // SIXTH: The proximity data
            Debug.Log(StreetSimProximityTracker.PT.proximityData);
            SaveSystemMethods.SaveCSV<ProximityData>(trialDirToSaveIn+"proximity",ProximityData.Headers,StreetSimProximityTracker.PT.proximityData.Flatten2D<ProximityData>());
        } else {
            Debug.Log("[STREET SIM] ERROR: Could not save json trial data");
        }

        /*
        //string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        //if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
        if (SaveSystemMethods.CheckOrCreateDirectory(simulationDirToSaveIn + trialPayload.name)) {
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
        */
        return true;
    }
    /*
    public void LoadSimulationData() {
        string p = SaveSystemMethods.GetSaveLoadDirectory(saveDirectory);
        string ap = "Assets/"+saveDirectory;
        Debug.Log("[STREET SIM] Loading data from: \"" + p + "\"");
        if (!SaveSystemMethods.CheckDirectoryExists(p)) {
            Debug.Log("[STREET SIM] ERROR: Designated simulation folder does not exist.");
            return;
        }
        if (SaveSystemMethods.CheckDirectoryExists(p+participantName+"/")) {
            ap = "Assets/"+saveDirectory+m_participantName+"/";
            p = p+participantName+"/";
        }
        List<LoadedSimulationDataPerTrial> tempLoadedTrials = new List<LoadedSimulationDataPerTrial>();
        for(int i = 0; i < m_trialGroups.Count; i++) {
            string pathToData = p+"simulationMetadata_"+i.ToString()+".json";
            Debug.Log("[STREET SIM] Attempting to load \""+pathToData+"\"");
            if (!SaveSystemMethods.CheckFileExists(pathToData)) {
                Debug.Log("[STREET SIM] ERROR: Simulation Metadata #" + i.ToString() + " does not appear to exist");
                continue;
            }
            SimulationData simData;
            if (!SaveSystemMethods.LoadJSON<SimulationData>(pathToData, out simData)) {
                Debug.Log("[STREET SIM] ERROR: Unable to read json data of Simulation Metadata #"+i.ToString());
                continue;
            }
            Debug.Log("[STREET SIM] Loaded Simulation Data #"+simData.simulationGroupNumber.ToString());
            foreach(string trialName in simData.trials) {
                LoadedSimulationDataPerTrial newLoadedTrial = new LoadedSimulationDataPerTrial(trialName, ap+trialName);
                TrialData trialData;
                if (LoadTrialData(p+trialName+"/trial.json", out trialData)) {
                    newLoadedTrial.trialData = trialData;
                }
                if (StreetSimIDController.ID.LoadDataPath(newLoadedTrial, out LoadedPositionData newPositionData)) {
                    newLoadedTrial.positionData = newPositionData;
                }
                tempLoadedTrials.Add(newLoadedTrial);
            }
        }
        Debug.Log("We have " + tempLoadedTrials.Count.ToString() + " trials available for parsing");
        m_loadedTrials = tempLoadedTrials;
        //StreetSimIDController.ID.LoadDataPaths(tempLoadedTrials);
    }

    public bool LoadTrialData(string path, out TrialData trial) {
        if (!SaveSystemMethods.CheckFileExists(path)) {
            Debug.Log("[STREET SIM] ERROR: Unable to find trial file \""+path+"\"");
            trial = default(TrialData);
            return false;
        }
        if (!SaveSystemMethods.LoadJSON<TrialData>(path, out trial)) {
            Debug.Log("[STREET SIM] ERROR: Unable to load json file \""+path+"\"...");
            return false;
        }
        return true;
    }
    */

    public float GetTimeFromStart(float cTime) {
        return cTime - m_currentTrial.startTime;
    }

    public string GetTrialDir() {
        return trialDirToSaveIn;
    }
    public string GetSimulationDir() {
        return simulationDirToSaveIn;
    }

    public List<string> GetActiveTrialsByName() {
        List<string> names = new List<string>();
        foreach(StreetSimTrialGroup group in m_trialGroups) {
            foreach(StreetSimTrial trial in group.trials) names.Add(trial.name);
        }
        return names;
    }
}

[System.Serializable]
public class StreetSimTrialGroup {
    public int groupNumber;
    public List<StreetSimTrial> trials;
    public StreetSimTrialGroup(int groupNumber) {
        this.groupNumber = groupNumber;
        trials = new List<StreetSimTrial>();
    }
}

[System.Serializable]
public class StreetSimTrial {
    [System.Serializable]
    public class StreetSimPrimaryModel {
        [Tooltip("The agent prefab we'll be instantiating")]
        public StreetSimAgent agent;
        [Tooltip("Should the model be safe or risky?")]
        public ModelBehavior modelBehavior;
        [Tooltip("Should the model start on the same side as the user or the opposite?")]
        public bool startOnSameSideAsPlayer;
        [Tooltip("Which path (out of all the model paths) should the model follow?")]
        public int modelPathIndex;

        private TrialDirection m_direction;
        public TrialDirection direction { get=>m_direction; set{m_direction=value;} }

        private float m_speed;
        public float speed { get=>m_speed; set{m_speed=value;} }
    }

    [System.Serializable]
    public class StreetSimFollowerModel {
        [Tooltip("The agent prefab we'll be instantiating")]
        public StreetSimAgent agent;
        [Tooltip("Should the model start on the same or opposite direction as the player?")]
        public bool modelStartOnSameSide;
    }

    public enum TrialDirection {
        SouthToNorth,
        NorthToSouth
    }
    public enum ModelStartOrientation {
        West,
        East,
        Middle,
        Random
    }
    public enum ModelBehavior {
        Safe,
        Risky
    }
    public enum ModelConfidence {
        NotConfident,
        Confident
    }
    public enum ModelStartSide {
        Same,
        Opposite,
        Mixed
    }

    [Tooltip("Name of the trial; must be unique from other trials.")] 
    public string name;
    [Tooltip("Primary model agent behavior")]
    public StreetSimPrimaryModel primaryModel;
    [Tooltip("Follower agent(s) behavior, if any")]
    public List<StreetSimPrimaryModel> models = new List<StreetSimPrimaryModel>();
    [Tooltip("Should the model start on the same or opposite direction as the player? THIS IS PURELY AESTHETIC AND DOES NOT ACTUALLY HAVE ANY FUNCTIONALITY IN UNITY")]
    public ModelStartSide modelStartOnSameSide;
    [Tooltip("Should the model be not confident or confident?")]
    public ModelConfidence modelConfidence;
    [Tooltip("What should the congestion of the cars be?")]
    public StreetSimCarManager.CarManagerStatus trafficCongestion;
    [Tooltip("What should the the congestion of the pedestrians be?")]
    public StreetSimAgentManager.AgentManagerStatus npcCongestion;
    [Tooltip("How long should the car signals stay on the \"Go\" signal?")]
    public float carSignalGoTime = 30f;
    [Tooltip("How long should the car signals stay on the \"Warning\" signal?")]
    public float carSignalWarningTime = 3f;

    /*
    [Tooltip("The agent prefab we'll be instantiating")]
    public StreetSimAgent agent;
    [Tooltip("Should the model be safe or risky?")]
    public ModelBehavior modelBehavior;
    [Tooltip("Should the model start on the same or opposite direction as the player?")]
    public bool modelStartOnSameSide;
    [Tooltip("Should the model start on the left or the right side of the player?")]
    public ModelStartOrientation modelStartOrientation;
    */
    /*
    [Tooltip("Where should the player be at the start of this trial?")]
    public Transform startPositionRef;
    */
    
    // These ones are manually set outside of the constructor and during trials, generally.
    [Tooltip("Is this the first trial? Manually set in Start()")]
    private bool m_isFirstTrial = false;
    public bool isFirstTrial { get=>m_isFirstTrial; set {m_isFirstTrial=value;} }
    [Tooltip("What's the direction (N->S or S->N) of the trial?")]
    private TrialDirection m_direction;
    public TrialDirection direction { get=>m_direction; set{m_direction=value;} }
    [Tooltip("What's this trials' current number?")]
    private int m_trialNumber;
    public int trialNumber { get=>m_trialNumber; set{m_trialNumber=value;} }
    [Tooltip("When did the trial start?")]
    private float m_startTime;
    public float startTime { get=>m_startTime; set{m_startTime=value;} }
    [Tooltip("When did the trial end?")]
    private float m_endTime;
    public float endTime { get=>m_endTime; set{m_endTime=value;} }
    [Tooltip("How long did the trial last?")]
    private float m_duration;
    public float duration { get=>m_duration; set{m_duration=value;} }
    [Tooltip("How many frames does the trial last?")]
    private int m_numFrames;
    public int numFrames { get=>m_numFrames; set{m_numFrames=value;} }
    [Tooltip("What timestamps did the player start (or restart) the trial?")]
    private List<float> m_innerStartTimes = new List<float>();
    [Tooltip("The start and ending sidewalks")]
    [SerializeField] private Transform m_startSidewalk, m_endSidewalk;
    public Transform startSidewalk { get=>m_startSidewalk; set{m_startSidewalk=value;} }
    public Transform endSidewalk { get=>m_endSidewalk; set{m_endSidewalk=value;} }
    public float crossWaitTime { get=>carSignalGoTime+carSignalWarningTime; set{} }

    private int m_modelPathIndex;
    public int modelPathIndex {
        get { return m_modelPathIndex; }
        set { m_modelPathIndex = value; }
    }
    public void AddInnerStartTime(float t) {
        m_innerStartTimes.Add(t);
    }

    public TrialData GetTrialData() {
        List<string> modelIDs = new List<string>();
        foreach(StreetSimPrimaryModel m in models) {
            modelIDs.Add(m.agent.GetID().id);
        }
        return new TrialData(
            name,
            m_trialNumber,
            m_startTime,
            m_endTime,
            m_duration,
            m_numFrames,
            m_innerStartTimes,
            m_direction.ToString(),
            modelConfidence.ToString(),
            trafficCongestion.ToString(),
            npcCongestion.ToString(),
            modelIDs,
            crossWaitTime
        );
    }
}

[System.Serializable]
public class SimulationData {
    public string participantName;
    public int simulationGroupNumber;
    public string startTime;
    public float duration;
    public List<string> trials;
    public string version;
    
    public SimulationData(string pname, int simNumber, string startTime, string version, float duration, List<string> trials) {
        this.participantName = pname;
        this.simulationGroupNumber = simNumber;
        this.startTime = startTime;
        this.duration = duration;
        this.trials = trials;
        this.version = version;
    }
    public SimulationData(string pname, int simNumber, string startTime, string version) {
        this.participantName = pname;
        this.simulationGroupNumber = simNumber;
        this.startTime = startTime;
        this.trials = new List<string>();
        this.version = version;
    }
}
[System.Serializable]
public class TrialData {

    public string name;
    public int trialNumber;
    public float startTime;
    public float endTime;
    public float duration;
    public int numFrames;
    public string direction;
    public string modelConfidence;
    public string trafficCongestion;
    public string npcCongestion;
    public List<float> innerStartTimes;
    public List<string> modelIDs;
    public float pedestrianWaitTime;

    // Uncertain about these now
    /*
    public string modelID;
    public string modelBehavior;
    public int modelPathIndex;
    public List<TrialAttempt> attempts; 
    public List<RaycastHitRow> participantGazeData;
    public List<StreetSimTrackablePayload> positionData;
    public List<TrialAttempt> modelAttempts;
    */
    
    public TrialData(
        string name,
        int trialNumber,
        float startTime,
        float endTime,
        float duration,
        int numFrames,
        List<float> innerStartTimes,
        string direction,
        string modelConfidence,
        string trafficCongestion,
        string npcCongestion,
        List<string> modelIDs,
        float pedestrianWaitTime
    ) {
        this.name = name;
        this.trialNumber = trialNumber;

        this.startTime = startTime;
        this.endTime = endTime;
        this.duration = duration;
        this.numFrames = numFrames;
        this.innerStartTimes = innerStartTimes;

        this.direction = direction;
        
        this.modelConfidence = modelConfidence;
        this.trafficCongestion = trafficCongestion;
        this.npcCongestion = npcCongestion;

        this.modelIDs = modelIDs;
        this.pedestrianWaitTime = pedestrianWaitTime;
    }
}

[System.Serializable]
public class AttemptsPayload {
    public string name;
    public List<TrialAttempt> attempts;
    public AttemptsPayload(string name, List<TrialAttempt> attempts) {
        this.name = name;
        this.attempts = attempts;
    }
}
[System.Serializable]
public class GazePayload {
    public List<RaycastHitRow> gazeData;
    public GazePayload(List<RaycastHitRow> gazeData) {
        this.gazeData = gazeData;
    }
}
[System.Serializable]
public class CarsPayload {
    public List<CarRow> carHistory;
    public CarsPayload(List<CarRow> carHistory) {
        this.carHistory = carHistory;
    }
}
[System.Serializable]
public class PositionsPayload {
    public string id;
    public List<StreetSimTrackable> positionData;
    public PositionsPayload(string id, List<StreetSimTrackable> trackables) {
        this.id = id;
        this.positionData = trackables;
    }
}

[System.Serializable]
public class TrialAttempt {
    public string id;
    public string direction;
    public float startTime;
    public float endTime;
    public bool successful;
    public string reason;
    public TrialAttempt(string id, string direction, float startTime) {
        this.id = id;
        this.direction = direction;
        this.startTime = startTime;
    }
    public TrialAttempt(string[] data) {
        this.id = data[0];
        this.direction = data[1];
        this.startTime = float.Parse(data[2]);
        this.endTime = float.Parse(data[3]);
        this.successful = bool.Parse(data[4]);
        this.reason = data[5];
    }
    public static List<string> Headers => new List<string> {
        "id",
        "direction",
        "startTime",
        "endTime",
        "successful",
        "reason"
    };
}