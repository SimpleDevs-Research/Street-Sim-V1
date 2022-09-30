using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyNavAgentHeadTurn : MonoBehaviour
{
    private Animator animator;
    public bool ikActive = false;
    [SerializeField] private Transform currentTargetTransform;
    public Transform headTransform;

    public Vector2 fovAngles = new Vector2(120f,60f);
    public float lookWeight;
    // Pivots
    private GameObject headToTargetPivot, headToBodyForwardPivot, headRotateToPivot;
    [SerializeField] private float headMovementTime = 0.25f;
    private Vector3 headMovementVelocity = Vector3.zero;

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
        // 3. headRotateToPivot
        headRotateToPivot = new GameObject("Head Rotate To Pivot");
        //headRotateToPivot.transform.parent = headTransform;
        headRotateToPivot.transform.position = headToBodyForwardPivot.transform.position + headToBodyForwardPivot.transform.forward;
    }

    private void Update() {
        // Rotate headToBodyForwardPivot so that it always looks in the same direction of the body
        headToBodyForwardPivot.transform.rotation = transform.rotation;  
        // We can't even do anything if ikActive is false or if currentTargetTransform is null
        if (!ikActive || currentTargetTransform == null) {
            // Lerp headRotateToPivot so that it's in front of the person
            //targetPosition = headToBodyForwardPivot.transform.position + headToBodyForwardPivot.transform.forward;
            //headRotateToPivot.transform.position = Vector3.Lerp(headRotateToPivot.transform.position, targetPosition, Time.deltaTime * 2.5f);
            ReduceLookWeight();
            return;
        }

        // Relocate `headRotateToPivot` to `currentTargetTransform`'s position
        //targetPosition = currentTargetTransform.position;
        //headRotateToPivot.transform.position = Vector3.Lerp(headRotateToPivot.transform.position, targetPosition, Time.deltaTime * 2.5f);
        // Rotate headToTargetPivot so that it always looks at the target transform
        headToTargetPivot.transform.LookAt(headRotateToPivot.transform);

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
    private void LateUpdate() {
        Vector3 targetPosition = (currentTargetTransform != null) ? currentTargetTransform.position : headToBodyForwardPivot.transform.position + headToBodyForwardPivot.transform.forward;
        headRotateToPivot.transform.position = Vector3.SmoothDamp(headRotateToPivot.transform.position, targetPosition, ref headMovementVelocity, headMovementTime);
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
                if (currentTargetTransform != null) {
                    animator.SetLookAtWeight(lookWeight);
                    animator.SetLookAtPosition(headRotateToPivot.transform.position);
                }
            } else {
                animator.SetLookAtWeight(0);
            }
        }
    }

    public void SetTarget(Transform target) {
        if (target == null) return;
        currentTargetTransform = target;
    }
    public void RemoveTarget(Transform target) {
        if (target == null) return;
        if (currentTargetTransform == target) currentTargetTransform = null;
    }
}
