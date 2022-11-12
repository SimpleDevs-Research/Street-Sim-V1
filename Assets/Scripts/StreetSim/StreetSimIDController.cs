using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;
using System.Text.RegularExpressions;

[System.Serializable]
public class StreetSimTrackable {
    public string id;
    public int frameIndex;
    public float timestamp;
    public SVector3 localPosition;
    public SQuaternion localRotation;
    public StreetSimTrackable(string id, int frameIndex, float timestamp, Vector3 localPosition, Quaternion localRotation) {
        this.id = id;
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.localPosition = localPosition;
        this.localRotation = localRotation;
    }
    public StreetSimTrackable(string id, int frameIndex, float timestamp, Transform t) {
        this.id = id;
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.localPosition = t.localPosition;
        this.localRotation = t.localRotation;
    }
}

public class StreetSimIDController : MonoBehaviour
{

    public static StreetSimIDController ID;
    [SerializeField] private List<ExperimentID> ids = new List<ExperimentID>();
    [SerializeField] private List<string> idNames = new List<string>();
    private Dictionary<ExperimentID, Queue<ExperimentID>> parentChildQueue = new Dictionary<ExperimentID, Queue<ExperimentID>>();

    [SerializeField] private bool m_shouldTrackPositions = true;
    [SerializeField] private bool m_trackChildren = false;
    [SerializeField] private int m_numTrackedPerFrame = 50;
    [SerializeField] private List<ExperimentID> m_trackables = new List<ExperimentID>();
    private Dictionary<ExperimentID,List<StreetSimTrackable>> m_payloads = new Dictionary<ExperimentID,List<StreetSimTrackable>>();
    public Dictionary<ExperimentID,List<StreetSimTrackable>> payloads { get=>m_payloads; set{} }

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
                id = toAdd.parent.id + "|-|" + id;
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
        if (m_trackables.Count == 0) yield return null;
        else {
            Queue<ExperimentID> temp = new Queue<ExperimentID>(m_trackables);
            int count = 0;
            while(temp.Count > 0) {
                ExperimentID id = temp.Dequeue();
                if (!m_payloads.ContainsKey(id)) m_payloads.Add(id, new List<StreetSimTrackable>());
                m_payloads[id].Add(new StreetSimTrackable(id.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,id.transform));
                count++;
                if (count >= m_numTrackedPerFrame) {
                    yield return null;
                    count = 0;
                }
                if (m_trackChildren && id.children.Count > 0) {
                    foreach(ExperimentID child in id.children) {
                        if (!m_payloads.ContainsKey(child)) m_payloads.Add(child, new List<StreetSimTrackable>());
                        m_payloads[child].Add(new StreetSimTrackable(child.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,child.transform));
                        if (count >= m_numTrackedPerFrame) {
                            yield return null;
                            count = 0;
                        }
                    }
                }
            }
        }
        yield return null;
    }

    public void ClearData() {
        m_payloads = new Dictionary<ExperimentID,List<StreetSimTrackable>>();
    }
}
