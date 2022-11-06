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

    [SerializeField] private bool m_shouldTrackPositions = true;
    [SerializeField] private List<StreetSimTrackablePayload> m_payloads = new List<StreetSimTrackablePayload>();
    public List<StreetSimTrackablePayload> payloads {
        get { return m_payloads; }
        set {}
    }

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
        if (m_shouldTrackPositions) StartCoroutine(TrackPositionsCoroutine());
    }

    public IEnumerator TrackPositionsCoroutine() {
        StreetSimTrackablePayload payload = new StreetSimTrackablePayload(
            StreetSim.S.trialFrameIndex, 
            StreetSim.S.trialFrameTimestamp
        );
        Queue<ExperimentID> temp = new Queue<ExperimentID>(ids);

        int count = 0;
        while(temp.Count > 0) {
            ExperimentID id = temp.Dequeue();
            payload.trackables.Add(new StreetSimTrackable(
                id.id,
                id.transform
            ));
            count++;
            if (count >= 50) {
                yield return null;
                count = 0;
            }
        }

        m_payloads.Add(payload);
        yield return null;
    }

    public void ClearData() {
        m_payloads = new List<StreetSimTrackablePayload>();
    }
}
