using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableTypes;
using System.Linq;

[System.Serializable]
public class STrackingData {
    public int index;
    public float timestamp;
    public SVector3 position;
    public SQuaternion rotation;
    public SVector3 localScale;
    public STrackingData(int index, float timestamp, Vector3 position, Quaternion rotation, Vector3 localScale) {
        this.index = index;
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
        this.localScale = localScale;
    }
    public STrackingData(int index, float timestamp, SVector3 position, SQuaternion rotation, SVector3 localScale) {
        this.index = index;
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
        this.localScale = localScale;
    }
}

[System.Serializable]
public class STransformTrackingTarget {
    public string id;
    public bool position, rotation, localScale;
    public List<STrackingData> data;
    public STransformTrackingTarget(string id, bool pos, bool rot, bool scale, List<STrackingData> data) {
        this.id = id;
        this.position = pos;
        this.rotation = rot;
        this.localScale = scale;
        this.data = data;
    }
}

[RequireComponent(typeof(ExperimentID))]
public class TransformTrackingTarget : MonoBehaviour
{
    private ExperimentID experimentID;
    private string id;
    public bool trackPosition = true, trackRotation = true, trackLocalScale = true;
    private Dictionary<int,STrackingData> dataDict = new Dictionary<int,STrackingData>();
    public List<STrackingData> dataList = new List<STrackingData>();

    private int currentIndex;
    private SVector3 pos, locScale;
    private SQuaternion rot;

    private void Awake() {
        experimentID = GetComponent<ExperimentID>();
        experimentID.onConfirmedID += this.SetID;
    }

    private void Start() {
        if (TransformTrackingController.current == null) return;
        TransformTrackingController.current.AddTarget(this);
    }

    private void SetID() {
        id = experimentID.id;
    }

    public void AddDataPoint() {
        if (ExperimentGlobalController.current == null || TransformTrackingController.current == null) return;
        pos = (trackPosition) ? transform.position : Vector3.zero;
        rot = (trackRotation) ? transform.rotation : Quaternion.identity;
        locScale = (trackLocalScale) ? transform.localScale : Vector3.zero;
        dataDict.Add(
            ExperimentGlobalController.current.currentIndex,
            new STrackingData(
                ExperimentGlobalController.current.currentIndex,
                ExperimentGlobalController.current.currentTime,
                pos, rot, locScale
            )
        );
    }

    public STransformTrackingTarget SaveData() {
        return new STransformTrackingTarget(
            id,
            trackPosition, trackRotation, trackLocalScale,
            dataDict.Values.ToList()
        );
    }
    
    public void LoadData(STransformTrackingTarget payload) {
        dataDict = new Dictionary<int,STrackingData>();
        trackPosition = payload.position;
        trackRotation = payload.rotation;
        trackLocalScale = payload.localScale;
        foreach(STrackingData d in payload.data) {
            dataDict.Add(d.index, d);
        }
        dataList = payload.data;
    }

}
