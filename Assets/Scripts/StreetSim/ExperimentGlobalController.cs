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

public class ExperimentGlobalController : MonoBehaviour
{
    public static ExperimentGlobalController current;

    [Header("IDs in the Experiment")]
    [SerializeField] private List<ExperimentIDRef> IDs = new List<ExperimentIDRef>();
    private Dictionary<string,ExperimentID> IDsDict = new Dictionary<string,ExperimentID>();

    [Header("Experiment Settings")]
    [SerializeField] private float m_startTime, m_endTime;
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
    [SerializeField] private UnityEvent startTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent endTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent saveTrackingEvents = new UnityEvent();
    [SerializeField] private UnityEvent loadTrackingEvents = new UnityEvent();

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
        startTrackingEvents?.Invoke();
    }

    public void EndTrackingEvents() {
        m_isTracking = false;
        m_endTime = Time.time;
        endTrackingEvents?.Invoke();
    }

    public void SaveTrackingEvents() {
        saveTrackingEvents?.Invoke();
    }

    public void LoadTrackingEvents() {
        saveTrackingEvents?.Invoke();
    }
}
