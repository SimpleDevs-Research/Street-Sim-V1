using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPosition : MonoBehaviour
{

    public enum FollowType {
        World,
        Local,
        None
    }
    public Transform toFollow = null;
    public Vector3 offset = Vector3.zero;
    public FollowType followType = FollowType.World;
    public bool followRotation = true;

    // Update is called once per frame
    private void Update() {
        if (toFollow == null) return;
        switch(followType) {
            case FollowType.World:
                this.transform.position = toFollow.position + offset;
                break;
            case FollowType.Local:
                this.transform.localPosition = toFollow.localPosition + offset;
                break;
        }
        if (followRotation) {
            switch(followType) {
                case FollowType.World:
                    this.transform.rotation = toFollow.rotation;
                    break;
                case FollowType.Local:
                    this.transform.localRotation = toFollow.localRotation;
                    break;
            }
        }
    }
}
