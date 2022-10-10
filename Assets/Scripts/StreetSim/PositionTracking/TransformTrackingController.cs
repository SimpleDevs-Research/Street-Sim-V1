using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using SerializableTypes;
using Helpers;
using EVRA.Inputs;

[System.Serializable]
public class TransformToTrack {
    public ExperimentID trackableID;
    public Dictionary<int,STransformTrackPackage> trackedData = new Dictionary<int,STransformTrackPackage>();
    public bool trackPosition;
    public bool trackRotation;
    public bool trackLocalScale;
    public TransformToTrack(ExperimentID trackableID, bool trackPosition, bool trackRotation, bool trackLocalScale) {
        this.trackableID = trackableID;
        this.trackPosition = trackPosition;
        this.trackRotation = trackRotation;
        this.trackLocalScale = trackLocalScale;
    }
}
[System.Serializable]
public class STransformToTrack {
    public string trackableID;
    public List<STransformTrackPackage> trackedData;
    public bool trackPosition;
    public bool trackRotation;
    public bool trackLocalScale;
    public STransformToTrack(string id, List<STransformTrackPackage> trackedData, bool trackPosition, bool trackRotation, bool trackLocalScale) {
        this.trackableID = id;
        this.trackedData = trackedData;
        this.trackPosition = trackPosition;
        this.trackRotation = trackRotation;
        this.trackLocalScale = trackLocalScale;
    }
}
[System.Serializable]
public class STransformTrackPackage {
    public int index;
    public float timestamp;
    public SVector3 position;
    public SQuaternion rotation;
    public SVector3 localScale;
    public STransformTrackPackage(int index, float timestamp, Vector3 pos, Quaternion rot, Vector3 scal) {
        this.index = index;
        this.timestamp = timestamp;
        this.position = pos;
        this.rotation = rot;
        this.localScale = scal;
    }
}

/*
[System.Serializable]
public class ExperimentDataSavePayload {
    public List<TransformToTrackSavePayload> trackedTransforms;
}
*/
[System.Serializable]
public class TransformToTrackSavePayload {
    //public List<STransformToTrack> data = new List<STransformToTrack>();
    public List<STransformTrackingTarget> data = new List<STransformTrackingTarget>();
}

public class TransformTrackingController : MonoBehaviour
{

    public static TransformTrackingController current;
    //[SerializeField] private List<TransformToTrack> m_trackedTransforms = new List<TransformToTrack>();
    private bool m_isTracking = false;

    [SerializeField] private string m_saveFilename = "TRACKEDDATA";
    public string saveFilename {
        get { return m_saveFilename; }
        set {}
    }
    [SerializeField] private string m_loadFilename = "TRACKEDDATA";
    public string loadFilename {
        get { return m_loadFilename; }
        set {}
    }

    [SerializeField] private List<TransformTrackingTarget> m_targets = new List<TransformTrackingTarget>();
    public delegate void MyTrackDelegate(); 
    public MyTrackDelegate onTrackEvent, onPrepareReplayEvent, onReplayEvent, onEndReplayEvent;

    private void Awake() {
        current = this;
    }

    public void StartTracking() {
        m_isTracking = true;
    }
    public void EndTracking() {
        m_isTracking = false;
    }

    public void UpdateTrackers() {
        if (m_isTracking && m_targets.Count > 0) {
            onTrackEvent?.Invoke();
        }
    }

    public void AddTarget(TransformTrackingTarget target) {
        if(!m_targets.Contains(target)) {
            m_targets.Add(target);
            onTrackEvent += target.AddDataPoint;
            onPrepareReplayEvent += target.PrepareForReplay;
            onReplayEvent += target.ReplayAtIndex;
            onEndReplayEvent += target.EndReplay;
        }
    }

    public void SaveTrackingData() {
        if (m_isTracking) {
            Debug.Log("[Transform Tracking] ERROR: Cannot save while tracking.");
            return;
        }
        // Generate JSON by creating a new payload
        TransformToTrackSavePayload payload = new TransformToTrackSavePayload();
        foreach(TransformTrackingTarget target in m_targets) {
            payload.data.Add(target.SaveData());
        }
        string dataToSave = SaveSystemMethods.ConvertToJSON<TransformToTrackSavePayload>(payload);
         // Create Save Directory
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(ExperimentGlobalController.current.directoryPath);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            SaveSystemMethods.SaveJSON(dirToSaveIn + m_saveFilename, dataToSave);
        }
    }
    public void LoadTrackingData() {
        // We can only load if we're running the game...
        if (ExperimentGlobalController.current == null) {
            Debug.Log("[Transform Tracking] ERROR - Must be running the game before loading");
            return;
        }
        if (m_isTracking) {
            Debug.Log("[Transform Tracking] ERRROR: Cannot load while tracking");
            return;
        }
        // Get Directory
        TransformToTrackSavePayload payload;
        string filenameToLoad = SaveSystemMethods.GetSaveLoadDirectory(ExperimentGlobalController.current.directoryPath) + m_loadFilename + ".json";
        Debug.Log("[Transform Tracking] Loading " + filenameToLoad + " ...");
        if (!SaveSystemMethods.CheckFileExists(filenameToLoad)) {
            Debug.Log("[Transform Tracking] ERROR: LOAD FILE DOES NOT EXIST");
            return;
        }
        if (!SaveSystemMethods.LoadJSON<TransformToTrackSavePayload>(filenameToLoad, out payload)) {
            Debug.Log("[Transform Tracking] ERROR: COULD NOT LOAD FILE");
            return;
        }

        // Have to match each STransformTrackingTarget with their respective TransformTrackingTarget, if it exists
        TransformTrackingTarget potentialTarget;
        foreach(STransformTrackingTarget package in payload.data) {
            if (ExperimentGlobalController.current.FindID<TransformTrackingTarget>(package.id, out potentialTarget)) {
                potentialTarget.LoadData(package);
            }
        }


        /*
        // Process loaded data
        ExperimentID reference;
        TransformToTrack tempT;
        foreach(STransformToTrack t in payload.data) {
            if (!ExperimentGlobalController.current.FindID<ExperimentID>(t.trackableID, out reference)) {
                Debug.Log("[Transform Tracking] ERROR: Could not find experiment ID of " + t.trackableID);
                continue;
            }
            tempT = new TransformToTrack(
                reference,
                t.trackPosition,
                t.trackRotation,
                t.trackLocalScale
            );
            foreach(STransformTrackPackage point in t.trackedData) {
                tempT.trackedData.Add(point.index, point);
            }
            m_trackedTransforms.Add(tempT);
        } 
        */
    }

    public void PrepareForReplay() {
        onPrepareReplayEvent?.Invoke();
    }

    public void ReplayDataAtIndex() {
        onReplayEvent?.Invoke();
    }

    public void EndReplay() {
        onEndReplayEvent?.Invoke();
    }
    
    /*
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

    public bool LoadTrackingData() {
        return false;
    }
    */
}
