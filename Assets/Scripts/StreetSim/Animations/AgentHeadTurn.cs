using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentHeadTurn : MonoBehaviour
{
    [SerializeField] private Transform headTransform;
    private Animator animator;

    public Transform currentTargetTransform = null;
    [SerializeField] private float lookWeight = 0f;

    private Transform headToTargetPivot;
    private Vector3 headToTargetVelocity = Vector3.zero;
    private float headToTargetSmoothTime = 1F;

    private void Awake() {
        animator = GetComponent<Animator>();
        GameObject headToTargetPivotGameObject = new GameObject("Head To Target Pivot");
        headToTargetPivot = headToTargetPivotGameObject.transform;
        headToTargetPivot.parent = headTransform;
        headToTargetPivot.localPosition = Vector3.zero;
    }

    // Update is called once per frame
    void Update() {
        if (currentTargetTransform == null) {
            headToTargetPivot.position = Vector3.SmoothDamp(headToTargetPivot.position, headTransform.position, ref headToTargetVelocity, headToTargetSmoothTime);
            ReduceLookWeight();
        } else {
            headToTargetPivot.position = Vector3.SmoothDamp(headToTargetPivot.position, currentTargetTransform.position, ref headToTargetVelocity, headToTargetSmoothTime);
            IncreaseLookWeight();
        }
        
    }

    private void ReduceLookWeight() {
        lookWeight = Mathf.Lerp(lookWeight, 0, Time.deltaTime * 2.5f);
    }
    private void IncreaseLookWeight() {
        lookWeight = Mathf.Lerp(lookWeight, 1, Time.deltaTime * 2.5f);
    }

    private void OnAnimatorIK() {
        if (animator == null) return;
        if (currentTargetTransform != null) {
            animator.SetLookAtWeight(lookWeight);
            animator.SetLookAtPosition(headToTargetPivot.position);
        } else {
            animator.SetLookAtWeight(0f);
        }
    }
}
