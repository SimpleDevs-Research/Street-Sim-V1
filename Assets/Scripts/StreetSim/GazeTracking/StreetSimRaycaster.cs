using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

public class StreetSimRaycaster : MonoBehaviour
{
    [SerializeField] private EVRA_Pointer pointer;
    [SerializeField] private float m_timestamp;
    [SerializeField] private int m_triangleIndex;
    [SerializeField] private string m_objectID;
    [SerializeField] private string m_agentID;
    [SerializeField] private SVector3 localPositionOfHit;

    private void Awake() {
        if (pointer == null) {
            HelperMethods.HasComponent<EVRA_Pointer>(gameObject, out pointer);
        }
    }

    private void Update() {
        if (pointer == null) return;
        // Get pointer data
        ExperimentID target, closestTarget;
        if (pointer.raycastTarget != null && HelperMethods.HasComponent<ExperimentID>(pointer.raycastTarget, out target)) {
            m_timestamp = Time.time;
            m_triangleIndex = pointer.raycastHitTriangleIndex;
            m_objectID = GetClosestPoint(pointer.raycastHitPosition, target, out closestTarget);
            m_agentID = target.id;
            localPositionOfHit = closestTarget.transform.InverseTransformPoint(pointer.raycastHitPosition);
        } else {
            m_timestamp = -1f;
            m_triangleIndex = -1;
            m_objectID = "";
            m_agentID = "";
            localPositionOfHit = default(SVector3);
        }
    }

    private static string GetClosestPoint(Vector3 worldPos, ExperimentID target, out ExperimentID closestTarget) {
        // We expect `target` to have a bunch of children.
        // Among those children, we check which is closest to `worldPos`
        // if we have no children, we just return the target itself
        if (target.children.Count == 0) {
            closestTarget = target;
            return target.id;
        }
        float closestDist = Vector3.Distance(worldPos, target.transform.position);
        float curDistance;
        closestTarget = target;
        foreach(ExperimentID child in target.children) {
            curDistance = Vector3.Distance(worldPos, child.transform.position);
            if(closestTarget == null || curDistance < closestDist) {
                closestTarget = child;
                closestDist = curDistance;
            }
        }
        return closestTarget.id;
    }
}
