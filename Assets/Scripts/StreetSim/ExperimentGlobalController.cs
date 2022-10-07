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
    public ExperimentDetailsPayload(string name, string trialNumber, float startTime, float endTime) {
        this.name = name;
        this.trialNumber = trialNumber;
        this.startTime = startTime;
        this.endTime = endTime;
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
    private bool m_isTracking = false;
    public bool isTracking {
        get { return m_isTracking; }
        set {}
    }

    [Header("Experiment Events")]
    [SerializeField] private UnityEvent startTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent eventsToTrack = new UnityEvent();
    [SerializeField] private UnityEvent endTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent saveTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent loadTrackingEvents = new UnityEvent();

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
        m_isTracking = true;
        m_startTime = Time.time;
        m_previousTimeDelay = m_startTime;
        startTrackingEvents?.Invoke();
    }

    public void EndTrackingEvents() {
        m_isTracking = false;
        m_endTime = Time.time;
        endTrackingEvents?.Invoke();
    }

    public void SaveTrackingEvents() {
        if (m_isTracking) {
            Debug.Log("[Global] ERROR: Cannot save while in the middle of tracking data.");
            return;
        }

        ExperimentDetailsPayload payload = new ExperimentDetailsPayload(
            m_participantName, m_trialNumber,
            m_startTime, m_endTime
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

        Debug.Log("GLOBAL: loading additional saved data if designated...");
        loadTrackingEvents?.Invoke();
    }

    private void FixedUpdate() {
        m_currentTime = Time.time;
        if (m_timeDelay == 0f || (m_currentTime - m_previousTimeDelay) >= m_timeDelay) {
            m_previousTimeDelay = m_currentTime;
            eventsToTrack?.Invoke();
        }
    }
}
