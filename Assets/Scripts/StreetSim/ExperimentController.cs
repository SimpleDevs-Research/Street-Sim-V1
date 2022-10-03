using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        s.position = new Vector3Save();
        s.position.x = this.position.x;
        s.position.y = this.position.y;
        s.position.z = this.position.z;
        s.rotation = new QuaternionSave();
        s.rotation.x = this.rotation.x;
        s.rotation.y = this.rotation.y;
        s.rotation.z = this.rotation.z;
        s.rotation.w = this.rotation.w;
        return s;
    }
}

[System.Serializable]
public class TrackedData {
    public string name;
    public Transform transform;
    public List<PositionData> positionData = new List<PositionData>();
    public TrackedData(string name, Transform t) {
        this.name = name;
        this.transform = t;
    }
    public void ResetPositionData() {
        this.positionData = new List<PositionData>();
    }
    public void AddPosition(float t) {
        positionData.Add(new PositionData(t,transform.position, transform.rotation));
    }
    public TrackedDataSave SaveData() {
        TrackedDataSave d = new TrackedDataSave();
        d.name = this.name;
        d.positionData = new List<PositionDataSave>();
        foreach(PositionData dataPoint in this.positionData) {
            d.positionData.Add(dataPoint.SaveData());
        }
        return d;
    }
}

[System.Serializable]
public class TransformKeyValuePair {
    public string key;
    public Transform value;
}

[System.Serializable]
public class PositionDataOfTransforms {
    public List<TrackedDataSave> data;
}
[System.Serializable]
public class TrackedDataSave {
    public string name;
    public List<PositionDataSave> positionData;
}
[System.Serializable]
public class PositionDataSave {
    public float timestamp;
    public Vector3Save position;
    public QuaternionSave rotation;
}
[System.Serializable]
public class Vector3Save {
    public float x, y, z;
}
[System.Serializable]
public class QuaternionSave {
    public float x, y, z, w;
}

public class ExperimentController : MonoBehaviour
{

    [SerializeField] private List<TransformKeyValuePair> trackedTransforms = new List<TransformKeyValuePair>();
    private Dictionary<string, TrackedData> trackedTransformDict = new Dictionary<string, TrackedData>();
    private bool isTracking = false;
    [SerializeField] private float trackIncrement = 0.1f;
    private IEnumerator trackingCoroutine = null;
    [SerializeField] private string savefilename = "Test_Data";

    private void Awake() {
        foreach(TransformKeyValuePair kvp in trackedTransforms) {
            trackedTransformDict.Add(kvp.key, new TrackedData(kvp.key, kvp.value));
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
        Debug.Log(d.data[0].positionData[0].position.x.ToString());
        Debug.Log("Saving tracking data for " + d.data.Count + " transforms into " + savefilename + ".json");
        string json = JsonUtility.ToJson(d, true);
        Debug.Log(json);
        File.WriteAllText(Application.dataPath + "/" + savefilename + ".json", json);
        yield return null;
    }
}
