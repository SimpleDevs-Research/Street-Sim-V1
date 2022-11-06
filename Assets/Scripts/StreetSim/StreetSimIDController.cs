using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;
using System.Text.RegularExpressions;

[System.Serializable]
public class StreetSimTrackablePayload {
    public int frameIndex;
    public float timestamp;
    public List<StreetSimTrackable> trackables;
    public StreetSimTrackablePayload(int frameIndex, float timestamp) {
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        trackables = new List<StreetSimTrackable>();
    }
}

[System.Serializable]
public class StreetSimTrackable {
    public string id;
    public SVector3 localPosition;
    public SQuaternion localRotation;
    public SVector3 localScale;
    public StreetSimTrackable(string id, Vector3 localPosition, Quaternion localRotation, Vector3 localScale) {
        this.id = id;
        this.localPosition = localPosition;
        this.localRotation = localRotation;
        this.localScale = localScale;
    }
    public StreetSimTrackable(string id, Transform t) {
        this.id = id;
        this.localPosition = t.localPosition;
        this.localRotation = t.localRotation;
        this.localScale = t.localScale;
    }
}

public class StreetSimIDController : MonoBehaviour
{

    public static StreetSimIDController ID;
    [SerializeField] private List<ExperimentID> ids = new List<ExperimentID>();
    [SerializeField] private List<string> idNames = new List<string>();
    private Dictionary<ExperimentID, Queue<ExperimentID>> parentChildQueue = new Dictionary<ExperimentID, Queue<ExperimentID>>();

    [SerializeField] private List<StreetSimTrackablePayload> payloads = new List<StreetSimTrackablePayload>();

    private void Awake() {
        ID = this;
    }

    public bool AddID(ExperimentID toAdd, out string finalID) {
        string id = toAdd.id;
        if (toAdd.parent != null) {
            if (!ids.Contains(toAdd.parent)) {
                if (!parentChildQueue.ContainsKey(toAdd.parent)) parentChildQueue.Add(toAdd.parent,new Queue<ExperimentID>());
                parentChildQueue[toAdd.parent].Enqueue(toAdd);
                finalID = id;
                return false;
            } else {
                id = toAdd.parent.id + ">" + id;
            }
        }
        if (!ids.Contains(toAdd)) {
            while(idNames.Contains(id)) {
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
            idNames.Add(finalID);
            ids.Add(toAdd);
            /*
            IDs.Add(new ExperimentIDRef(finalID, newID));
            IDsDict.Add(finalID, newID);
            */
        } else {
            finalID = id;
        }
        return true;
    }

    public void AddChildren(ExperimentID parent) {
        if (parentChildQueue.ContainsKey(parent) && parentChildQueue[parent].Count > 0) {
            while(parentChildQueue[parent].Count > 0) {
                ExperimentID child = parentChildQueue[parent].Dequeue();
                child.Initialize();
            }
        }
    }

    public void TrackPositions() {
        StreetSimTrackablePayload payload = new StreetSimTrackablePayload(
            StreetSim.S.trialFrameIndex, 
            StreetSim.S.trialFrameTimestamp
        );
        foreach(ExperimentID id in ids) {
            Debug.Log("Tracking ID WITH " + id.id);
            payload.trackables.Add(new StreetSimTrackable(
                id.id,
                id.transform
            ));
        }
        payloads.Add(payload);
    }

    public void ClearData() {
        payloads = new List<StreetSimTrackablePayload>();
    }
}
