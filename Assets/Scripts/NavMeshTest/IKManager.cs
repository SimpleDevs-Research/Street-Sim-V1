using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKManager : MonoBehaviour
{

    private Animator animator;
    public bool ikActive = false;
    public Transform targetTransform;
    public Transform headTransform;

    public Vector2 fovAngles = new Vector2(120f,60f);
    public float lookWeight;
    // Pivots
    private GameObject headToTargetPivot, headToBodyForwardPivot;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Getting our pivots
        // 1. headToTargetPivot
        headToTargetPivot = new GameObject("Head To Target Pivot");
        headToTargetPivot.transform.parent = headTransform;
        headToTargetPivot.transform.localPosition = Vector3.zero;
        // 2. headToBodyForwardPivot
        headToBodyForwardPivot = new GameObject("Head To Body Forward Pivot");
        headToBodyForwardPivot.transform.parent = headTransform;
        headToBodyForwardPivot.transform.localPosition = Vector3.zero;
    }

    private void Update() {
        // We can't even do anything if ikActive is false or if targetTransform is null
        if (!ikActive || targetTransform == null) {
            ReduceLookWeight();
            return;
        }

        // Rotate headToTargetPivot so that it always looks at the target transform
        headToTargetPivot.transform.LookAt(targetTransform);
        // Rotate headToBodyForwardPivot so that it always looks in the same direction of the body
        headToBodyForwardPivot.transform.rotation = transform.rotation;  

        // To get FOV angle correct, we need to consider 3 factors:
        // 1. X-axis difference
        float horizontalAngleToTarget = Vector3.Angle(
            headToBodyForwardPivot.transform.forward,
            new Vector3(
                headToTargetPivot.transform.forward.x,
                headToBodyForwardPivot.transform.forward.y,
                headToTargetPivot.transform.forward.z
            )
        );
        // 2. Y-axis difference
        float verticalAngleToTarget = Vector3.Angle(
            headToBodyForwardPivot.transform.forward,
            new Vector3(
                headToBodyForwardPivot.transform.forward.x,
                headToTargetPivot.transform.forward.y,
                headToTargetPivot.transform.forward.z
            )
        );
        // 3. Actual angle between the forward of the head (matching the body's forward) and the vector towards the target
        float angleToTarget = Vector3.Angle(headToBodyForwardPivot.transform.forward, headToTargetPivot.transform.forward);

        // We'll now adjust the weight of the lookat based on our fovAngles
        // We only look at the target if 1) the angle to the target fits within the FOV angles, and 2) the actual angle doesn't exceed the max of either FOV angle
        if (
            horizontalAngleToTarget <= fovAngles.x*0.5f 
            && verticalAngleToTarget <= fovAngles.y*0.5f
            && angleToTarget <= Mathf.Max(fovAngles.x,fovAngles.y)*0.5f
        ) {
            IncreaseLookWeight();
        }
        else {
            ReduceLookWeight();
        }
    }

    private void ReduceLookWeight() {
        lookWeight = Mathf.Lerp(lookWeight, 0, Time.deltaTime * 2.5f);
    }
    private void IncreaseLookWeight() {
        lookWeight = Mathf.Lerp(lookWeight, 1, Time.deltaTime * 2.5f);
    }

    private void OnAnimatorIK() {
        if (animator) {
            if(ikActive) {
                if (targetTransform != null) {
                    animator.SetLookAtWeight(lookWeight);
                    animator.SetLookAtPosition(targetTransform.position);
                }
            } else {
                animator.SetLookAtWeight(0);
            }
        }
    }
}
