using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

[System.Serializable]
public class StreetSimTrackable {
    public float timestamp;
    public SVector3 position;
    public SQuaternion rotation;
    public SVector3 localScale;
    public StreetSimTrackable(float timestamp, Vector3 position, Quaternion rotation, Vector3 localScale) {
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
        this.localScale = localScale;
    }
    public StreetSimTrackable(float timestamp, Transform t) {
        this.timestamp = timestamp;
        this.position = t.position;
        this.rotation = t.rotation;
        this.localScale = t.localScale;
    }
}

public class StreetSimPositionController : MonoBehaviour
{   
    /*
    public static StreetSimPositionController PC;
    private Dictionary<StreetSimPositionTrackable, List<StreetSimTrackable>> trackables = new Dictionary<StreetSimPositionTrackable, List<StreetSimTrackable>>(); 

    

    private void Awake() {
        PC = this;
    }

    public void Track() {

    }
    */
}
