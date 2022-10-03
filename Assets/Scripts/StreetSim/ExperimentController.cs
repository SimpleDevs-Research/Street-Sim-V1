using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SerializableTypes;
using Helpers;
using EVRA.Inputs;

/*
[System.Serializable]
public class PositionData {
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    public PositionData(float timestamp, Vector3 position, Quaternion rotation) {
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
    }
    public PositionDataSave SaveData() {
        PositionDataSave s = new PositionDataSave();
        s.timestamp = this.timestamp;
        s.position = this.position;
        s.rotation = this.rotation;
        return s;
    }
}

[System.Serializable]
public class TrackedData {
    public ExperimentTrackable trackable;
    public List<PositionData> transformData = new List<PositionData>();
    public TrackedData(ExperimentTrackable t) {
        this.trackable = t;
    }
    public void ResetPositionData() {
        this.transformData = new List<PositionData>();
    }
    public void AddPosition(float t) {
        transformData.Add(new PositionData(t,trackable.transform.position, trackable.transform.rotation));
    }
    public TrackedDataSave SaveData() {
        TrackedDataSave d = new TrackedDataSave();
        d.objectId = this.trackable.experimentId;
        d.transformData = new List<PositionDataSave>();
        foreach(PositionData dataPoint in this.transformData) {
            d.transformData.Add(dataPoint.SaveData());
        }
        return d;
    }
}

[System.Serializable]
public class PositionDataOfTransforms {
    public List<TrackedDataSave> data;
}
[System.Serializable]
public class TrackedDataSave {
    public string objectId;
    public List<PositionDataSave> transformData;
}
[System.Serializable]
public class PositionDataSave {
    public float timestamp;
    public SVector3 position;
    public SQuaternion rotation;
}
*/

[System.Serializable]
public class TransformToTrack {
    public string trackableId;
    public Transform trackable;
}
[System.Serializable]
public class ExperimentDataSavePayload {
    public List<TransformToTrackSavePayload> trackedTransforms;
}
[System.Serializable]
public class TransformToTrackSavePayload {
    public string trackableId;
    public List<STrackableData> trackedData;
}

public class ExperimentController : MonoBehaviour
{

    [SerializeField] private List<TransformToTrack> trackedTransforms = new List<TransformToTrack>();
    
    [Header("File Saving and Uploading")]
    [SerializeField] private string m_destinationFolder = "";
    [SerializeField] private string m_destinationFilename = "test";


    private void Awake() {
        ExperimentTrackable et = null;
        foreach(TransformToTrack ttt in trackedTransforms) {
            if (HelperMethods.HasComponent<ExperimentTrackable>(ttt.trackable, out et)) {
                et.Initialize(this,ttt.trackableId);
            } else {
                et = ttt.trackable.gameObject.AddComponent<ExperimentTrackable>() as ExperimentTrackable;
                et.Initialize(this,ttt.trackableId);
            }
        }
    }

    public bool StartTracking() {
        foreach(TransformToTrack ttt in trackedTransforms) {
            ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().StartTracking();
        }
        return true;
    }
    public bool StartTracking(InputEventDataPackage p) { return StartTracking(); }
    public bool EndTracking() {
        foreach(TransformToTrack ttt in trackedTransforms) {
            ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().EndTracking();
        }
        return true;
    }
    public bool EndTracking(InputEventDataPackage p) { return EndTracking(); }
    public bool StartReplay() {
        foreach(TransformToTrack ttt in trackedTransforms) {
            ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().StartReplay();
        }
        return true;
    }
    public bool StartReplay(InputEventDataPackage p) { return StartReplay(); }
    public bool EndReplay() {
        foreach(TransformToTrack ttt in trackedTransforms) {
            ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().EndReplay();
        }
        return true;
    }
    public bool EndReplay(InputEventDataPackage p) { return EndReplay(); }
    public bool ClearData() {
        foreach(TransformToTrack ttt in trackedTransforms) {
            ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().ClearData();
        }
        return true;
    }
    public bool ClearData(InputEventDataPackage p) { return ClearData(); }


    public string GetSaveDirectory() {
        return (m_destinationFolder.Length > 0) ? Application.dataPath + "/" + m_destinationFolder + "/" : Application.dataPath + "/";
    }
    public bool SaveTrackingData() {
        // Create JSON
        ExperimentDataSavePayload payload = new ExperimentDataSavePayload();
        payload.trackedTransforms = new List<TransformToTrackSavePayload>();
        foreach(TransformToTrack ttt in trackedTransforms) {
            TransformToTrackSavePayload newItemInPayload = new TransformToTrackSavePayload();
            newItemInPayload.trackableId = ttt.trackableId;
            newItemInPayload.trackedData = ttt.trackable.gameObject.GetComponent<ExperimentTrackable>().data;
            payload.trackedTransforms.Add(newItemInPayload);
        }
        string dataToSave = SaveSystemMethods.ConvertToJSON<ExperimentDataSavePayload>(payload);
        // Create Save Directory
        string dirToSaveIn = GetSaveDirectory();
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            SaveSystemMethods.SaveJSON(dirToSaveIn + m_destinationFilename, dataToSave);
        }
        return true;
    }
    public bool SaveTrackingData(InputEventDataPackage p) { return SaveTrackingData(); }

    /*
    [SerializeField] private List<TransformKeyValuePair> trackedTransforms = new List<TransformKeyValuePair>();
    private Dictionary<string, TrackedData> trackedTransformDict = new Dictionary<string, TrackedData>();
    private bool isTracking = false;
    [SerializeField] private float trackIncrement = 0.1f;
    private IEnumerator trackingCoroutine = null;
    [SerializeField] private string savefilename = "Test_Data";
    [SerializeField] private string savefilefolder = "";

    private void Awake() {
        foreach(TransformKeyValuePair kvp in trackedTransforms) {
            if (kvp.trackable == null) continue;
            kvp.trackable.Initialize(this, kvp.trackableId);
            trackedTransformDict.Add(kvp.trackableId, new TrackedData(kvp.trackable));
        }
    }

    public void ResetTracking(string[] keys) {
        foreach(string key in keys) {
            if (trackedTransformDict.ContainsKey(key)) trackedTransformDict[key].ResetPositionData();
        }
    }
    public void ResetTracking() {
        foreach(TrackedData d in trackedTransformDict.Values) {
            d.ResetPositionData();
        }
    }

    public bool StartTracking() {
        Debug.Log("Starting tracking...");
        isTracking = true;
        if (trackingCoroutine == null) {
            trackingCoroutine = TrackingUpdate();
            StartCoroutine(trackingCoroutine);
        }
        return true;
    }
    
    public bool EndTracking() {
        Debug.Log("Ending Tracking!");
        isTracking = false;
        return true;
    }

    private IEnumerator TrackingUpdate() {
        float timestamp;
        while (isTracking) {
            timestamp = Time.time;
            foreach(TrackedData d in trackedTransformDict.Values) {
                d.AddPosition(timestamp);
                yield return null;
            }
            yield return new WaitForSeconds(trackIncrement);
        }
        // Do something after tracking
        trackingCoroutine = null;
        yield return null;
    }

    public void SaveTrackingData() {
        // Get copies of data to save, so that new values don't mess up the data at this stage.
        Dictionary<string, TrackedData> copy = new Dictionary<string, TrackedData>(trackedTransformDict);
        // create the save container
        PositionDataOfTransforms d = new PositionDataOfTransforms();
        d.data = new List<TrackedDataSave>();
        foreach(TrackedData value in copy.Values) {
            d.data.Add(value.SaveData());
        }
        Debug.Log(d.data.Count);
        // Run the coroutine
        StartCoroutine(SaveData(d));
    }

    public IEnumerator SaveData(PositionDataOfTransforms d) {
        //Debug.Log(d.data[0].positionData[0].position.x.ToString());
        Debug.Log("Saving tracking data for " + d.data.Count + " transforms into " + savefilename + ".json");
        string json = JsonUtility.ToJson(d, true);
        //Debug.Log(json);
        string dir = (savefilefolder.Length > 0) ? Application.dataPath + "/" + savefilefolder + "/" : Application.dataPath + "/";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(dir + savefilename + ".json", json);
        yield return null;
    }
    */
}
