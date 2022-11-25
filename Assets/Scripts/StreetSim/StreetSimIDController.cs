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
    public float localPosition_x, localPosition_y, localPosition_z;
    public SQuaternion localRotation;
    public float localRotation_x, localRotation_y, localRotation_z, localRotation_w;
    public StreetSimTrackable(string id, int frameIndex, float timestamp, Vector3 localPosition, Quaternion localRotation) {
        this.id = id;
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.localPosition = localPosition;
        this.localPosition_x = this.localPosition.x;
        this.localPosition_y = this.localPosition.y;
        this.localPosition_z = this.localPosition.z;
        this.localRotation = localRotation;
        this.localRotation_x = this.localRotation.x;
        this.localRotation_y = this.localRotation.y;
        this.localRotation_z = this.localRotation.z;
        this.localRotation_w = this.localRotation.w;
    }
    public StreetSimTrackable(string id, int frameIndex, float timestamp, Transform t) {
        this.id = id;
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.localPosition = t.localPosition;
        this.localPosition_x = this.localPosition.x;
        this.localPosition_y = this.localPosition.y;
        this.localPosition_z = this.localPosition.z;
        this.localRotation = t.localRotation;
        this.localRotation_x = this.localRotation.x;
        this.localRotation_y = this.localRotation.y;
        this.localRotation_z = this.localRotation.z;
        this.localRotation_w = this.localRotation.w;
    }
    public bool Compare(Transform other) {
        // Returns TRUE if the same or too similar
        return this.localPosition == other.localPosition && this.localRotation == other.localRotation;
    }
    public static List<string> Headers => new List<string> {
        "id",
        "frameIndex",
        "timestamp",
        "localPosition_x",
        "localPosition_y",
        "localPosition_z",
        "localRotation_x",
        "localRotation_y",
        "localRotation_z",
        "localRotation_w",
    };
    /*
    public void InitializeValues(string[] data) {
        int numHeaders = StreetSimTrackable.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
        for(int i = 0; i < tableSize; i++) {
            this.id = data[numHeaders * (i+1)];
            this.frameIndex = Int32.Parse(data[numHeaders * (i+1) + 1]);
            this.timestamp =  float.Parse(data[numHeaders * (i+1) + 2]);
            this.localPosition_x = float.Parse(data[numHeaders * (i+1) + 3]);
            this.localPosition_y = float.Parse((float)data[numHeaders * (i+1) + 4]);
            this.localPosition_z = float.Parse((float)data[numHeaders * (i+1) + 5]);
            this.localRotation_x = float.Parse((float)data[numHeaders * (i+1) + 6]);
            this.localRotation_y = float.Parse((float)data[numHeaders * (i+1) + 7]);
            this.localRotation_z = float.Parse((float)data[numHeaders * (i+1) + 8]);
            this.localRotation_w = float.Parse((float)data[numHeaders * (i+1) + 9]);

            this.localPosition = new Vector3(localPosition_x, localPosition_y, localPosition_z);
            this.localRotation = new Quaternion(localRotation_x, localRotation_y, localRotation_z, localRotation_w);
        }
    }
    */
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
    [SerializeField] private List<ExperimentID> m_trialTrackables = new List<ExperimentID>();
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
            temp.AddRange(m_trialTrackables);
            int count = 0;
            while(temp.Count > 0) {
                ExperimentID id = temp.Dequeue();
                if (!id.shouldTrack) {
                    count++;
                }
                else if (!m_payloads.ContainsKey(id)) {
                    m_payloads.Add(id, new List<StreetSimTrackable>());
                    m_payloads[id].Add(new StreetSimTrackable(id.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,id.transform));
                    count++;
                }
                // Check previous record. If previous record is too similar, we disregard the entry
                else if (!m_payloads[id][^1].Compare(id.transform)) {
                    m_payloads[id].Add(new StreetSimTrackable(id.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,id.transform));
                    count++;
                }
                if (count >= m_numTrackedPerFrame) {
                    yield return null;
                    count = 0;
                }
                if (m_trackChildren && id.children.Count > 0) {
                    foreach(ExperimentID child in id.children) {
                        if (!m_payloads.ContainsKey(child)) {
                            m_payloads.Add(child, new List<StreetSimTrackable>());
                            m_payloads[child].Add(new StreetSimTrackable(child.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,child.transform));
                            count++;
                        }
                        else if (!m_payloads[child][^1].Compare(child.transform)) {
                            m_payloads[child].Add(new StreetSimTrackable(child.id,StreetSim.S.trialFrameIndex,StreetSim.S.trialFrameTimestamp,child.transform));
                            count++;
                        }
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

    public void ClearTrialTrackables() {
        m_trialTrackables = new List<ExperimentID>();
    }
    public void AddTrialTrackable(ExperimentID t) {
        m_trialTrackables.Add(t);
    }

    public void ClearData() {
        m_payloads = new Dictionary<ExperimentID,List<StreetSimTrackable>>();
    }
}
