using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using Helpers;

[System.Serializable]
public class ExperimentIDRef {
    public string id;
    public ExperimentID experimentObject;
    public ExperimentIDRef(string id, ExperimentID experimentObject) {
        this.id = id;
        this.experimentObject = experimentObject;
    }
}

[System.Serializable]
public class ExperimentDetailsPayload {
    public string name;
    public string trialNumber;
    public float startTime;
    public float endTime;
    public int maxFrames;
    public ExperimentDetailsPayload(string name, string trialNumber, float startTime, float endTime, int maxFrames) {
        this.name = name;
        this.trialNumber = trialNumber;
        this.startTime = startTime;
        this.endTime = endTime;
        this.maxFrames = maxFrames;
    }

}

public class ExperimentGlobalController : MonoBehaviour
{
    public static ExperimentGlobalController current;

    [Header("Directory & Files")]
    [SerializeField] private string m_sourceDirectory = "GazeData";
    public string sourceDirectory {
        get { return m_sourceDirectory; }
        set {}
    }
    [SerializeField] private string m_participantName = "RyanKim";
    public string participantName {
        get { return m_participantName; }
        set {}
    }
    [SerializeField] private string m_trialNumber = "0";
    public string trialNumber {
        get { return m_trialNumber; }
        set {}
    }
    public string directoryPath {
        get { return m_sourceDirectory + "/" + m_participantName + "/" + m_trialNumber + "/"; }
        set {}
    }

    [Header("IDs in the Experiment")]
    [SerializeField] private List<ExperimentIDRef> IDs = new List<ExperimentIDRef>();
    private Dictionary<string,ExperimentID> IDsDict = new Dictionary<string,ExperimentID>();

    [Header("Experiment Settings")]
    [SerializeField] private float m_timeDelay = 0.05f;
    private float m_currentTimeDelay = 0f, m_previousTimeDelay = 0f;
    private float m_currentTime, m_startTime, m_endTime;
    public float currentTime {
        get { return m_currentTime; }
        set {}
    }
    public float startTime {
        get { return m_startTime; }
        set {}
    }
    public float endTime {
        get { return m_endTime; }
        set {}
    }
    public enum GlobalStatus {
        Waiting,
        Tracking,
        PreparingReplay,
        Replaying
    }
    private GlobalStatus m_status = GlobalStatus.Waiting;
    public GlobalStatus status {
        get { return m_status; }
        set {}
    }
    /*
    private bool m_isTracking = false;
    public bool isTracking {
        get { return m_isTracking; }
        set {}
    }
    */
    [SerializeField] private int m_currentIndex = -1;
    public int currentIndex {
        get { return m_currentIndex; }
        set {}
    }
    private int m_maxFrames = 0;
    public int maxFrames {
        get { return m_maxFrames; }
        set {}
    }

    [Header("Experiment Events")]
    [SerializeField] private UnityEvent startTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent eventsToTrack = new UnityEvent();
    [SerializeField] private UnityEvent endTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent saveTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent loadTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent prepareReplayEvents = new UnityEvent();
    [SerializeField] private UnityEvent onReplayEvents = new UnityEvent();
    [SerializeField] private UnityEvent endReplayEvents = new UnityEvent();

    public enum PlaybackType {
        TrialDuration,
        RangedDuration,
        Timestamp,
    }
    [Header("Trial Playback Controls")]
    [SerializeField, Tooltip("0 = beginning, 1 = ending"), Range(0f,1f)]
    private float timestamp;

    private void Awake() {
        current = this;
    }

    public bool AddID(ExperimentID newID, out string finalID) {
        string id = newID.id;
        while(IDsDict.ContainsKey(id)) {
            // Keep finding alternatives until we find no match
            Match m = Regex.Match(id, @"\d+$");
            if (m.Success) {
                // There is a number... so we modify that number
                int endInt;
                int.TryParse(m.Value, out endInt);
                id = id.Substring(0,m.Index) + (endInt+1);
            } else {
                // No number - so we add one
                id += "1";
            }
        }
        finalID = id;
        IDs.Add(new ExperimentIDRef(finalID, newID));
        IDsDict.Add(finalID, newID);
        return true;
    }

    public bool FindID<T>(string queryID, out T outComponent) {
        if (IDsDict.ContainsKey(queryID)) {
            T comp = default(T);
            bool found = HelperMethods.HasComponent<T>(IDsDict[queryID].gameObject, out comp);
            outComponent = comp;
            return found;
        } else {
            outComponent = default(T);
            return false;
        }
    }

    public void StartTrackingEvents() {
        // m_isTracking = true;
        m_status = GlobalStatus.Tracking;
        m_startTime = Time.time;
        m_currentIndex = -1;
        m_maxFrames = 0;
        m_previousTimeDelay = 0;
        startTrackingEvents?.Invoke();
    }

    public void EndTrackingEvents() {
        // m_isTracking = false;
        if (m_status != GlobalStatus.Tracking) {
            Debug.Log("[GLOBAL] ERROR: Cannot end tracking if the system is not tracking to begin with.");
            return;
        }
        m_status = GlobalStatus.Waiting;
        m_endTime = Time.time;
        m_maxFrames = m_currentIndex+1;
        endTrackingEvents?.Invoke();
    }

    public void SaveTrackingEvents() {
        if (m_status == GlobalStatus.Tracking) {
            Debug.Log("[Global] ERROR: Cannot save while in the middle of tracking data.");
            return;
        }

        ExperimentDetailsPayload payload = new ExperimentDetailsPayload(
            m_participantName, m_trialNumber,
            m_startTime, m_endTime,
            m_maxFrames
        );
        string dataToSave = SaveSystemMethods.ConvertToJSON<ExperimentDetailsPayload>(payload);
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(directoryPath);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            if (SaveSystemMethods.SaveJSON(dirToSaveIn + "metadata", dataToSave)) {
                saveTrackingEvents?.Invoke();
            }
        }
    }

    public void LoadTrackingEvents() {
        ExperimentDetailsPayload payload;
        string filenameToLoad = SaveSystemMethods.GetSaveLoadDirectory(directoryPath) + "metadata.json";
        Debug.Log("[GLOBAL] Loading metadata contents...");
        if (!SaveSystemMethods.CheckFileExists(filenameToLoad)) {
            Debug.Log("[GLOBAL] ERROR: metadata file does not exist. Canceling load.");
            return;
        }
        if (!SaveSystemMethods.LoadJSON<ExperimentDetailsPayload>(filenameToLoad, out payload)) {
            Debug.Log("[GLOBAL] ERROR: metadata file could not be loaded. Canceling load.");
            return;
        }
        
        Debug.Log("GLOBAL: metadata loaded SUCCESS");
        m_startTime = payload.startTime;
        m_endTime = payload.endTime;
        m_maxFrames = payload.maxFrames;

        Debug.Log("GLOBAL: loading additional saved data if designated...");
        loadTrackingEvents?.Invoke();
    }

    public void PrepareReplay() {
        if (m_status == GlobalStatus.Tracking) {
            Debug.Log("[GLOBAL] ERROR: Cannot prepare replaying events while the system is tracking data.");
            return;
        }
        if (m_maxFrames <= 0) {
            Debug.Log("[GLOBAL] ERROR: Make sure to load in data prior or that you have data tracked and logged in the system");
            return;
        }
        m_currentIndex = -1;
        m_previousTimeDelay = 0f;
        m_status = GlobalStatus.PreparingReplay;
        prepareReplayEvents?.Invoke();
        Debug.Log("Preparing Replay");
    }
    public void ReplayLoadedEvents() {
        if (m_status == GlobalStatus.Tracking) {
            Debug.Log("[GLOBAL] ERROR: Cannot start replaying events while the system is tracking data.");
            return;
        }
        if (m_maxFrames <= 0) {
            Debug.Log("[GLOBAL] ERROR: Make sure to load in data prior or that you have data tracked and logged in the system");
            return;
        }
        if (m_status == GlobalStatus.Waiting) {
            Debug.Log("[GLOBAL] ERROR: Make sure to click \"Prepare Replay\" before starting the replay");
            return;
        }
        m_startTime = Time.time;
        m_status = GlobalStatus.Replaying;
        Debug.Log("Initializing Replay");
    }
    public void EndReplay() {
        m_status = GlobalStatus.Waiting;
        endReplayEvents?.Invoke();
    }

    private void FixedUpdate() {
        switch(m_status) {
            case GlobalStatus.Tracking:
                m_currentTime = Time.time - m_startTime;
                if (m_timeDelay == 0f || (m_currentTime - m_previousTimeDelay) >= m_timeDelay) {
                    m_previousTimeDelay = m_currentTime;
                    m_currentIndex += 1;
                    eventsToTrack?.Invoke();
                }
                break;
            case GlobalStatus.Replaying:
                Debug.Log("Replaying at index " + m_currentIndex);
                m_currentTime = Time.time - m_startTime;
                if (m_timeDelay == 0f || (m_currentTime - m_previousTimeDelay) >= m_timeDelay) {
                    m_previousTimeDelay = m_currentTime;
                    if (m_currentIndex < m_maxFrames) m_currentIndex+=1;
                    onReplayEvents?.Invoke();
                } else {
                    Debug.Log("Can't increment index");
                }
                break;
        }
    }
}
