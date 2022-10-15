using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

[System.Serializable]
public class RaycastHitRow {
    public int frameIndex;
    public float timestamp;
    public int triangleIndex;
    public string hitID;
    public string agentID;
    public float[] localPositionOfHitPosition;
    public float[] localPositionOfHitTarget;
    public RaycastHitRow(int index, float t, int i, string h, string a, float[] lpp, float[] lpt) {
        this.frameIndex = index;
        this.timestamp = t;
        this.triangleIndex = i;
        this.hitID = h;
        this.agentID = a;
        this.localPositionOfHitPosition = lpp;
        this.localPositionOfHitTarget = lpt;
    }
}

public class StreetSimRaycaster : MonoBehaviour
{
    public static StreetSimRaycaster R;

    [SerializeField] private EVRA_Pointer pointer;
    [SerializeField] private List<RaycastHitRow> m_hits = new List<RaycastHitRow>();
    public List<RaycastHitRow> hits { get { return m_hits; } set{} }

    [SerializeField] private float m_timestamp;
    [SerializeField] private int m_triangleIndex;
    [SerializeField] private string m_hitID;
    [SerializeField] private string m_agentID;
    [SerializeField] private SVector3 m_localPositionOfHitPosition;
    [SerializeField] private SVector3 m_localPositionOfHitTarget;


    private void Awake() {
        R = this;
    }

    // This only runs when `StreetSim` calls it
    public void CheckRaycast() {
        if (pointer == null) return;
        // Get pointer data
        ExperimentID target, closestTarget;
        if (pointer.raycastTarget != null && HelperMethods.HasComponent<ExperimentID>(pointer.raycastTarget, out target)) {
            m_triangleIndex = pointer.raycastHitTriangleIndex;
            m_hitID = GetClosestPoint(pointer.raycastHitPosition, target, out closestTarget);
            m_agentID = target.ref_id;
            m_localPositionOfHitPosition = closestTarget.transform.InverseTransformPoint(pointer.raycastHitPosition);
            m_localPositionOfHitTarget = closestTarget.transform.localPosition;
            m_hits.Add(
                new RaycastHitRow(
                    StreetSim.S.trialFrameIndex,
                    StreetSim.S.trialFrameTimestamp, 
                    m_triangleIndex, 
                    m_hitID, 
                    m_agentID, 
                    new float[3]{
                        m_localPositionOfHitPosition.x,
                        m_localPositionOfHitPosition.y,
                        m_localPositionOfHitPosition.z
                    }, 
                    new float[3]{
                        m_localPositionOfHitTarget.x,
                        m_localPositionOfHitTarget.y,
                        m_localPositionOfHitTarget.z
                    }
                )
            );
        } 
        /*
        else {
            m_timestamp = -1f;
            m_triangleIndex = -1;
            m_hitID = "";
            m_agentID = "";
            localPositionOfHit = default(SVector3);
        }
        */
    }

    private static string GetClosestPoint(Vector3 worldPos, ExperimentID target, out ExperimentID closestTarget) {
        // We expect `target` to have a bunch of children.
        // Among those children, we check which is closest to `worldPos`
        // if we have no children, we just return the target itself
        if (target.children.Count == 0) {
            closestTarget = target;
            return target.ref_id;
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
        return closestTarget.ref_id;
    }
}
