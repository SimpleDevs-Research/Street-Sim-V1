using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SerializableTypes;
using Helpers;
using EVRA.Inputs;

[System.Serializable]
public class TransformToTrack {
    public string trackableId;
    public Transform trackable;
}
[System.Serializable]
public class STransformToTrack {
    
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

public class TransformTrackingController : MonoBehaviour
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

    public bool LoadTrackingData() {
        return false;
    }

}
