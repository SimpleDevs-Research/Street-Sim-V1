using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKManager : MonoBehaviour
{
    /*
    public enum LookBehavior {
        ByProximity,
        ByFirstInList,
        ByLastInList
    }
    */
    /*
    public class TargetTransformData {
        public float horizontalAngleToTarget;
        public float verticalAngleToTarget;
        public float angleToTarget;
        public bool visible = false;
        public TargetTransformData(Transform bodyForwardPivot, Transform targetPivot) {
            CalculateAngles(bodyForwardPivot, targetPivot);
        }
        public void CalculateAngles(Transform bodyForwardPivot, Transform targetPivot) {
            // Calculate angles
            this.horizontalAngleToTarget = Vector3.Angle(
                bodyForwardPivot.transform.forward,
                new Vector3(
                    targetPivot.transform.forward.x,
                    bodyForwardPivot.transform.forward.y,
                    targetPivot.transform.forward.z
                )
            );
            this.verticalAngleToTarget = Vector3.Angle(
                bodyForwardPivot.transform.forward,
                new Vector3(
                    bodyForwardPivot.transform.forward.x,
                    targetPivot.transform.forward.y,
                    targetPivot.transform.forward.z
                )
            );
            this.angleToTarget = Vector3.Angle(
                bodyForwardPivot.transform.forward, 
                targetPivot.transform.forward
            );

        }
        public void CheckVisibilityGivenFOV(Vector2 fovAngles) {
            this.visible = (
                this.horizontalAngleToTarget <= fovAngles.x*0.5f 
                && this.verticalAngleToTarget <= fovAngles.y*0.5f
                && this.angleToTarget <= Mathf.Max(fovAngles.x,fovAngles.y)*0.5f
            );
        }
    }
    */
    private Animator animator;
    public bool ikActive = false;
    [SerializeField] private Transform headTransform;

    //[SerializeField] private LookBehavior lookPriority = LookBehavior.ByProximity;
    [SerializeField] private Transform currentTargetTransform;
    //[SerializeField] private List<Transform> targetTransforms = new List<Transform>();
    //private Dictionary<Transform, TargetTransformData> targetData = new Dictionary<Transform, TargetTransformData>();
    //private List<Transform> visibleTargets = new List<Transform>();

    public Vector2 fovAngles = new Vector2(120f,60f);
    private float lookWeight;
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
        /*
        // Reset `visibleTargets`
        visibleTargets = new List<Transform>();
        // We can't even do anything if ikActive is false or if there aren't any targets to look at
        if (!ikActive || targetTransforms.Count == 0) {
            ReduceLookWeight();
            currentTargetTransform = null;
            return;
        }

        // Rotate headToBodyForwardPivot so that it always looks in the same direction of the body
        headToBodyForwardPivot.transform.rotation = transform.rotation; 
        // Look at each transform, update its data into `targetData`
        foreach(Transform tt in targetTransforms) {
            // Rotate headToTargetPivot so that it looks at the target transform in this loop
            headToTargetPivot.transform.LookAt(tt);
            headToBodyForwardPivot.transform.rotation = transform.rotation; 
            if (targetData.ContainsKey(tt)) targetData[tt].CalculateAngles(headToBodyForwardPivot.transform, headToTargetPivot.transform);
            else targetData.Add(tt, new TargetTransformData(headToBodyForwardPivot.transform, headToTargetPivot.transform));
            targetData[tt].CheckVisibilityGivenFOV(fovAngles);
            if (targetData[tt].visible) visibleTargets.Add(tt);
        }

        // If we don't have any visible targets, stop there
        if (visibleTargets.Count == 0) {
            ReduceLookWeight();
            currentTargetTransform = null;
            return;
        }

        // Choose which to look at, based on the settings
        Transform currentTarget = null;
        switch(lookPriority) {
            case LookBehavior.ByFirstInList:
                currentTarget = visibleTargets[0];
                break;
            case LookBehavior.ByLastInList:
                currentTarget = visibleTargets[visibleTargets.Count - 1];
                break;
            case LookBehavior.ByProximity:
                float closestDistance = 0f, currentDistance = 0f;
                foreach(Transform target in visibleTargets) {
                    // Calculate distance
                    currentDistance = Vector3.Distance(headTransform.position,target.position);
                    if (currentTarget == null) {
                        currentTarget = target;
                        closestDistance = currentDistance;
                    }
                    else if (currentDistance < closestDistance) {
                        currentTarget = target;
                        closestDistance = currentDistance;
                    }
                }
                currentTargetTransform = currentClosest;
                break;
        }
        
        // Final check
        if (currentTarget == null) ReduceLookWeight();
        else {
            // We need to check if the current target our system has detected isthe same as the one our head is currently looking at
            if (currentTarget == currentTargetTransform) {
                currentTargetTransform = currenTarget;
                IncreaseLookWeight();
            } else {
                // In this scenario, we need to interpolate between currentTargetTransform and currentTarget
                // We only switch over if the angle is significantly small enough

            }

        }
        */

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
                if (currentTargetTransform != null) {
                    animator.SetLookAtWeight(lookWeight);
                    animator.SetLookAtPosition(currentTargetTransform.position);
                }
            } else {
                animator.SetLookAtWeight(0);
            }
        }
    }

    /*
    public void AddTarget(Transform target) {
        if (!targetTransforms.Contains(target)) targetTransforms.Add(target);
    }
    public void RemoveTarget(Transform target) {
        if (targetTransforms.Contains(target)) targetTransforms.Remove(target);
    }
    */

    public void SetTarget(Transform target) {
        currentTargetTransform = target;
    }
    public void RemoveTarget(Transform target) {
        if (currentTargetTransform == target) currentTargetTransform = null;
    }
}
