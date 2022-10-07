using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableTypes;
using Helpers;

/// <summary> Serializable version of TrackableData. </summary>
[System.Serializable]
public struct STrackableData {
    public float time;
    public SVector3 position;
    public SQuaternion rotation;
    public string raycastTargetId;
 
    public STrackableData(float time, SVector3 position, SQuaternion rotation) {
        this.time = time;
        this.position = position;
        this.rotation = rotation;
        this.raycastTargetId = null;
    }
    public STrackableData(float time, SVector3 position, SQuaternion rotation, string raycastTargetId) {
        this.time = time;
        this.position = position;
        this.rotation = rotation;
        this.raycastTargetId = raycastTargetId;
    }
 
    public override string ToString()
        => $"Time: {this.time.ToString()}\nPosition: {this.position.ToString()}\nRotation: {this.rotation.ToString()}\nTarget: {this.raycastTargetId}";

}

public class ExperimentTrackable : MonoBehaviour
{

    public enum TrackingType {
        Raw,
        WithRaycastTarget,
    }
    public enum TrackingStatus {
        Off,
        Tracking,
        Replaying,
    }
    
    private TransformTrackingController controller;
    private string m_experimentId;
    public string experimentId {
        get { return m_experimentId; }
        set {}
    }
    private List<STrackableData> m_data = new List<STrackableData>();
    public List<STrackableData> data {
        get { return m_data; }
        set {}
    }

    private Rigidbody rigidbody;
    private bool previousKinematicSetting;

    private TrackingStatus m_status = TrackingStatus.Off;
    private int currentReplayIndex = 0;
    private TrackingType m_trackingType = TrackingType.Raw;
    private Transform raycastTarget = null;

    private void Awake() {
        HelperMethods.HasComponent<Rigidbody>(this.gameObject, out rigidbody);
    }

    public void Initialize(TransformTrackingController controller, string id) {
        this.controller = controller;
        this.m_experimentId = id;
    }

    public bool GetInitializedStatus() {
        return this.controller != null;
    }

    public void StartTracking() {   m_status = TrackingStatus.Tracking;    }
    public void EndTracking() {     m_status = TrackingStatus.Off;         }

    public void StartReplay() {  
        m_status = TrackingStatus.Replaying;
        if (rigidbody != null) {
            previousKinematicSetting = rigidbody.isKinematic;
            rigidbody.isKinematic = false;
        }
        SetTransform(0);
    }
    public void EndReplay() {       
        m_status = TrackingStatus.Off;
        SetTransform(data.Count - 1);
        if (rigidbody != null) {
            rigidbody.isKinematic = previousKinematicSetting;
        }
    }
    public void ClearData() {
        if (m_status != TrackingStatus.Replaying) m_data = new List<STrackableData>();
    }

    private void Update() {
        if (m_trackingType == TrackingType.Raw) return;
        RaycastHit hit;
        if (Physics.Raycast(transform.position,transform.forward,out hit)) {
            raycastTarget = hit.transform;
        }
    }
    private void FixedUpdate() {
        switch(m_status) {
            case TrackingStatus.Tracking:
                switch(m_trackingType) {
                    case TrackingType.Raw:
                        m_data.Add(new STrackableData(Time.time, transform.position, transform.rotation));
                        break;
                }
                break;
            case TrackingStatus.Replaying:
                int nextReplayIndex = currentReplayIndex + 1;
                if (nextReplayIndex <= data.Count-1) SetTransform(nextReplayIndex);
                break;
        }
    }

    public void SetTransform(int index) { 
        currentReplayIndex = index;
        STrackableData sData = m_data[index];
        transform.position = sData.position;
        transform.rotation = sData.rotation;
    }

}
