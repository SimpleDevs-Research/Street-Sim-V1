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
    public bool positionIsLocal, rotationIsLocal;
    public bool position, rotation, localScale;
    public List<STrackingData> data;
    public STransformTrackingTarget(string id, bool posIsLocal, bool rotIsLocal, bool pos, bool rot, bool scale, List<STrackingData> data) {
        this.id = id;
        this.positionIsLocal = posIsLocal;
        this.rotationIsLocal = rotIsLocal;
        this.position = pos;
        this.rotation = rot;
        this.localScale = scale;
        this.data = data;
    }
}

[System.Serializable]
public class ComponentToTrack {
    public Behaviour component;
    public bool originalState;
}

[RequireComponent(typeof(ExperimentID))]
public class TransformTrackingTarget : MonoBehaviour
{
    private Rigidbody rigidbody;
    private bool isKinematic = false;
    private Vector3 originalPosition, originalScale;
    private Quaternion originalRotation;

    [SerializeField] private List<ComponentToTrack> componentsToDisableDuringReplay = new List<ComponentToTrack>();

    private ExperimentID experimentID;
    private string id;
    public bool positionIsLocal = false, rotationIsLocal = false;
    public bool trackPosition = true, trackRotation = true, trackLocalScale = true;
    private Dictionary<int,STrackingData> dataDict = new Dictionary<int,STrackingData>();
    public List<STrackingData> dataList = new List<STrackingData>();

    private int currentIndex;
    private SVector3 pos, locScale;
    private SQuaternion rot;

    private void Awake() {
        experimentID = GetComponent<ExperimentID>();
        experimentID.onConfirmedID += this.SetID;
        rigidbody = GetComponent<Rigidbody>();
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
        pos = (trackPosition) ? (positionIsLocal) ? transform.localPosition : transform.position : Vector3.zero;
        rot = (trackRotation) ? (rotationIsLocal) ? transform.localRotation : transform.rotation : Quaternion.identity;
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
            positionIsLocal, rotationIsLocal,
            trackPosition, trackRotation, trackLocalScale,
            dataDict.Values.ToList()
        );
    }
    
    public void LoadData(STransformTrackingTarget payload) {
        dataDict = new Dictionary<int,STrackingData>();
        positionIsLocal = payload.positionIsLocal;
        rotationIsLocal = payload.rotationIsLocal;
        trackPosition = payload.position;
        trackRotation = payload.rotation;
        trackLocalScale = payload.localScale;
        foreach(STrackingData d in payload.data) {
            dataDict.Add(d.index, d);
        }
        dataList = payload.data;
    }

    public void PrepareForReplay() {
        if (rigidbody != null) {
            isKinematic = rigidbody.isKinematic;
            rigidbody.isKinematic = true;
        }
        if (componentsToDisableDuringReplay.Count > 0) {
            foreach(ComponentToTrack c in componentsToDisableDuringReplay) {
                c.originalState = c.component.enabled;
                c.component.enabled = false;
            }
        }
        originalPosition = (positionIsLocal) ? transform.localPosition : transform.position;
        originalRotation = (rotationIsLocal) ? transform.localRotation : transform.rotation;
        originalScale = transform.localScale;
    }
    public void ReplayAtIndex() {
        int i = ExperimentGlobalController.current.currentIndex;
        if (dataDict.ContainsKey(i)) {
            if (trackPosition) {
                if (positionIsLocal) transform.localPosition = dataDict[i].position;
                else transform.position = dataDict[i].position;
            }
            if (trackRotation) {
                if (rotationIsLocal) transform.localRotation = dataDict[i].rotation;
                else transform.rotation = dataDict[i].rotation;
            }
            if (trackLocalScale) transform.localScale = dataDict[i].localScale;
        }
    }
    public void EndReplay() {
        if (rigidbody != null) rigidbody.isKinematic = isKinematic;
        if (componentsToDisableDuringReplay.Count > 0) {
            foreach(ComponentToTrack c in componentsToDisableDuringReplay) {
                c.component.enabled = c.originalState;
            }
        }
        if (positionIsLocal) transform.localPosition = originalPosition;
        else transform.position = originalPosition;
        if (rotationIsLocal) transform.localRotation = originalRotation;
        else transform.rotation = originalRotation;
        transform.localScale = originalScale;
    }

}
