using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;
using System.Text.RegularExpressions;
using UnityEditor;

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
    public StreetSimTrackable(string[] data) {
        this.id = data[0];
        this.frameIndex = Int32.Parse(data[1]);
        this.timestamp = float.Parse(data[2]);
        this.localPosition_x = float.Parse(data[3]);
        this.localPosition_y = float.Parse(data[4]);
        this.localPosition_z = float.Parse(data[5]);
        this.localPosition = new Vector3(this.localPosition_x, this.localPosition_y, this.localPosition_z);
        this.localRotation_x = float.Parse(data[6]);
        this.localRotation_y = float.Parse(data[7]);
        this.localRotation_z = float.Parse(data[8]);
        this.localRotation_w = float.Parse(data[9]);
        this.localRotation = new Quaternion(this.localRotation_x, this.localRotation_y, this.localRotation_z, this.localRotation_w);
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
    public string ToString() {
        return 
            this.id.ToString() + "-" + 
            this.frameIndex.ToString() + "-" +
            this.timestamp.ToString() + "-" +
            this.localPosition.ToString() + "-" +
            this.localRotation.ToString();
    }
}

[System.Serializable]
public class LoadedPositionData {
    [System.Serializable]
    public class StreetSimTrackableWithID {
        public ExperimentID experimentID;
        public StreetSimTrackable trackable;
        public StreetSimTrackableWithID(ExperimentID experimentID, StreetSimTrackable trackable) {
            this.experimentID = experimentID;
            this.trackable = trackable;
        } 
    }
    public string trialName;
    public TextAsset textAsset;
    public List<ExperimentID> idsTracked;
    public Dictionary<int, float> indexTimeMap;
    //public Dictionary<int, Dictionary<ExperimentID, StreetSimTrackable>> positionDataByFrame;
    public Dictionary<float, Dictionary<ExperimentID, StreetSimTrackable>> positionDataByTimestamp;
    /*
    public Dictionary<ExperimentID, List<StreetSimTrackable>> payloadByID;
    public Dictionary<float, StreetSimTrackableWithID> payloadByTimestamp;
    private List<float> payloadByTimestampOrder;
    */
    public LoadedPositionData(string trialName, TextAsset textAsset, List<StreetSimTrackable> trackables) {
        this.trialName = trialName;
        this.textAsset = textAsset;

        this.idsTracked = new List<ExperimentID>();
        this.indexTimeMap = new Dictionary<int, float>();
        this.positionDataByTimestamp = new Dictionary<float, Dictionary<ExperimentID, StreetSimTrackable>>();

        foreach(StreetSimTrackable trackable in trackables) {
            // Find the experiment ID that matches
            ExperimentID id = StreetSimIDController.ID.FindIDFromName(trackable.id);
            if (id == null) {
                Debug.Log("[ID CONTROLLER] Error: Could not find an ExperimentID that matches the found ID...");
                Debug.Log(trackable.ToString());
                continue;
            }
            // Add this id to `idsTracked`
            if (!this.idsTracked.Contains(id)) this.idsTracked.Add(id);
            // trackable has access to frameIndex + timestamp, Let's add them
            if (!this.indexTimeMap.ContainsKey(trackable.frameIndex)) this.indexTimeMap.Add(trackable.frameIndex, trackable.timestamp);
            // Add this to positionDatabyTimestamp
            if (!this.positionDataByTimestamp.ContainsKey(trackable.timestamp)) this.positionDataByTimestamp.Add(trackable.timestamp, new Dictionary<ExperimentID, StreetSimTrackable>());
            if (!this.positionDataByTimestamp[trackable.timestamp].ContainsKey(id)) this.positionDataByTimestamp[trackable.timestamp].Add(id,trackable);
            //Debug.Log("Trial \""+this.trialName+"\": At time " + trackable.timestamp + ", there are " + this.positionDataByTimestamp[trackable.timestamp].Keys.Count + " unique ExperimentIDs");
        }
        /*
        this.payloadByID = new Dictionary<ExperimentID, List<StreetSimTrackable>>();
        this.payloadByTimestamp = new Dictionary<float, StreetSimTrackableWithID>();
        payloadByTimestampOrder = new List<float>();
        foreach(StreetSimTrackable trackable in trackables) {
            // Find the experiment ID that matches
            ExperimentID id = StreetSimIDController.ID.FindIDFromName(t.id);
            if (id == null) {
                Debug.Log("[ID CONTROLLER] Error: Could not find an ExperimentID that matches the found ID...");
                Debug.Log(t.ToString());
                continue;
            }
            // Add to payloadByID
            if (!this.payloadByID.ContainsKey(id)) this.payloadByID.Add(id, new List<StreetSimTrackable>());
            this.payloadByID[id].Add(t);
            // Add to payloadbyTimestamp
            float timestamp = trackable.timestamp
        }
        */
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
    [SerializeField] private List<ExperimentID> m_trialTrackables = new List<ExperimentID>();
    private Dictionary<ExperimentID,List<StreetSimTrackable>> m_payloads = new Dictionary<ExperimentID,List<StreetSimTrackable>>();
    public Dictionary<ExperimentID,List<StreetSimTrackable>> payloads { get=>m_payloads; set{} }

    [SerializeField] private List<LoadedPositionData> m_loadedAssets = new List<LoadedPositionData>();
    public List<LoadedPositionData> loadedAssets { get=>m_loadedAssets; set{} }
    //[SerializeField] private Dictionary<ExperimentID, List<StreetSimTrackable>> m_loadedPayloadByID = new Dictionary<ExperimentID, List<StreetSimtrackable>>();
    // [SerializeField] private Dictionary<float, List<StreetSimTrackableWithID>> m_loadedPayloadByTime = new Dictionary<float, List<StreetSimTrackable>>();

    [SerializeField] private Transform m_raycasterTransform = null;
    [SerializeField] private LayerMask replayGazeMask;

    private bool m_initialized = false;
    public bool initialized { get=>m_initialized; set{} }
    private IEnumerator replayCoroutine = null;
    private Dictionary<Transform, Vector3> m_replayOriginalPositions = new Dictionary<Transform, Vector3>();
    [SerializeField] private GameObject gazePrefab;
    private List<GameObject> gazeObjects = new List<GameObject>();

    private void Awake() {
        ID = this;
    }

    private void Start() {
        m_initialized = true;
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

    public ExperimentID FindIDFromName(string name) {
        ExperimentID found = null;
        foreach(ExperimentID possible in ids) {
            if (possible.id == name) {
                found = possible;
                break;
            }
        }
        return found;
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

    /*
    public void LoadData(LoadedPositionData data) {
        if (!m_initialized || !StreetSim.S.initialized) return;
        if (data.textAsset == null) {
            Debug.Log("[ID CONTROLLER] ERROR: Cannot load a data without a CSV file...");
            return;
        }
        string[] loadedPositionsRaw = SaveSystemMethods.ReadCSV(data.textAsset);
        List<StreetSimTrackable> loadedPositions = ParseLoadedPositionsData(loadedPositionsRaw);
        
        m_payloads = new Dictionary<ExperimentID, List<StreetSimTrackable>>();
        foreach(StreetSimTrackable t in loadedPositions) {
            // Find the experiment ID that matches
            ExperimentID id = FindIDFromName(t.id);
            if (id == null) {
                Debug.Log("[ID CONTROLLER] Error: Could not find an ExperimentID that matches the found ID...");
                Debug.Log(t.ToString());
                continue;
            }
            if (!m_payloads.ContainsKey(id)) {
                m_payloads.Add(id, new List<StreetSimTrackable>());
            }
            m_payloads[id].Add(t);
        }
        Debug.Log("There are currently " + m_payloads.Count.ToString() + " ExperimentIDs whose data was properly loaded.");
    }
    */

    public void LoadDataPaths(List<LoadedSimulationDataPerTrial> trialPaths) {
        List<LoadedPositionData> interpretedPaths = new List<LoadedPositionData>();
        foreach(LoadedSimulationDataPerTrial t in trialPaths) {
            string assetPath = t.assetPath+"/positions.csv";
            if (!SaveSystemMethods.CheckFileExists(assetPath)) {
                Debug.Log("[STREET SIM] ERROR: Cannot load textasset \""+assetPath+"\"!");
                continue;
            }
            TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
            string[] pr = SaveSystemMethods.ReadCSV(ta);
            List<StreetSimTrackable> p = ParseLoadedPositionsData(pr);
            interpretedPaths.Add(new LoadedPositionData(t.trialName, ta, p));
        }
        m_loadedAssets = interpretedPaths;
    }


    private List<StreetSimTrackable> ParseLoadedPositionsData(string[] data){
        List<StreetSimTrackable> dataFormatted = new List<StreetSimTrackable>();
        int numHeaders = StreetSimTrackable.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
      
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = data.RangeSubset(rowKey,numHeaders);
            dataFormatted.Add(new StreetSimTrackable(row));
        }
        return dataFormatted;
    }

    public void ReplayRecord(LoadedPositionData trial) {
        // trial contains all the data we need. So let's use them.
        // Firstly, reset the simulation if one replay is already playing
        if (replayCoroutine != null) {
            StopCoroutine(replayCoroutine);
            foreach(KeyValuePair<Transform, Vector3> kvp in m_replayOriginalPositions) {
                kvp.Key.localPosition = kvp.Value;
            }
        }
        replayCoroutine = Replay(trial);
        StartCoroutine(replayCoroutine);
    }

    private IEnumerator Replay(LoadedPositionData trial) {
        // The data we're looking for is in trial.indexTimeMap and trial.positionDataByTimestamp
        List<float> order = new List<float>(trial.indexTimeMap.Values);
        order.Sort((a,b) => a.CompareTo(b));
        m_replayOriginalPositions = new Dictionary<Transform, Vector3>();
        foreach(ExperimentID id in trial.idsTracked) {
            if (id.id != "User") m_replayOriginalPositions.Add(id.transform, id.transform.localPosition);
        }
        
        int count = order.Count;
        int index = -1;
        float prevTimestamp = 0f;
        m_raycasterTransform.gameObject.SetActive(true);

        while(index < count-1) {
            index++;
            float waitTime = order[index] - prevTimestamp;
            yield return new WaitForSeconds(waitTime);
            
            foreach(KeyValuePair<ExperimentID, StreetSimTrackable> kvp in trial.positionDataByTimestamp[order[index]]) {
                if (kvp.Key.id == "User") {
                    m_raycasterTransform.localPosition = kvp.Value.localPosition;
                    m_raycasterTransform.localRotation = kvp.Value.localRotation;
                } else {
                    kvp.Key.transform.localPosition = kvp.Value.localPosition;
                    kvp.Key.transform.localRotation = kvp.Value.localRotation;
                }
            }
            
            prevTimestamp = order[index];
        }

        foreach(KeyValuePair<Transform, Vector3> kvp in m_replayOriginalPositions) {
            kvp.Key.localPosition = kvp.Value;
        }
        m_raycasterTransform.position = Vector3.zero;
        m_raycasterTransform.gameObject.SetActive(false);
        yield return null;
    }
    
    /*
    public void ReplayRecord(ExperimentID key, bool trackGaze = false) {
        if (!m_payloads.ContainsKey(key)) {
            Debug.Log("[ID CONTROLLER] Error: Cannot replay something that doesn't exist in our payloads...");
            return;
        }
        if (replayCoroutine != null) {
            StopCoroutine(replayCoroutine);
        }
        replayCoroutine = Replay(key, trackGaze);
        StartCoroutine(replayCoroutine);
    }

    private IEnumerator Replay(ExperimentID key, bool trackGaze) {
        List<StreetSimTrackable> trackables = m_payloads[key];
        List<RaycastHitRow> gazes = new List<RaycastHitRow>();
        while(gazeObjects.Count > 0) {
            GameObject g = gazeObjects[0];
            gazeObjects.RemoveAt(0);
            Destroy(g);
        }
        gazeObjects = new List<GameObject>();

        int count = trackables.Count;
        int index = -1;
        float prevTimestamp = 0f;

        StreetSim.S.GazeBox.position = StreetSim.S.Cam360.position;
        StreetSim.S.GazeBox.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in StreetSim.S.GazeBox) {
            child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        }
        m_raycasterTransform.gameObject.SetActive(true);

        while (index < count-1) {
            index++;
            float waitTime = trackables[index].timestamp - prevTimestamp;
            yield return new WaitForSeconds(waitTime);
            m_raycasterTransform.transform.localPosition = trackables[index].localPosition;
            m_raycasterTransform.transform.localRotation = trackables[index].localRotation;
            
            if (trackGaze) {
                RaycastHitRow row;
                if (StreetSimRaycaster.R.CheckRaycastManual(m_raycasterTransform, replayGazeMask, trackables[index].frameIndex, trackables[index].timestamp, out row)) {
                    gazes.Add(row);
                    GameObject newGazeObject = Instantiate(gazePrefab, StreetSim.S.Cam360, false);
                    newGazeObject.transform.localPosition = new Vector3(row.localPosition[0],row.localPosition[1],row.localPosition[2]);
                    newGazeObject.transform.localRotation = Quaternion.identity;
                    gazeObjects.Add(newGazeObject);
                }
            }
            prevTimestamp = trackables[index].timestamp;
        }

        if (trackGaze && gazes.Count > 0) {
            Debug.Log("[ID CONTROLLER] Saving manually-tracked gaze data...");
            string simulationDirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(StreetSim.S.saveDirectory);
            if (!SaveSystemMethods.CheckOrCreateDirectory(simulationDirToSaveIn)) {
                Debug.Log("[ID CONTROLLER] ERROR: Cannot check if the directory for the simulation data exists or not...");
            } else {
                SaveSystemMethods.SaveCSV<RaycastHitRow>(simulationDirToSaveIn+"gazeManual",RaycastHitRow.Headers,gazes);
                ScreenCapture.CaptureScreenshot("Test.png");
            }
        }

        m_raycasterTransform.gameObject.SetActive(false);
    }
    */
}
