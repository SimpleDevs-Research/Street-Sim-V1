using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

[RequireComponent(typeof(ExperimentID))]
public class ExperimentRaycastTarget : MonoBehaviour
{
    private ExperimentID experimentIDComp;
    [SerializeField] private ExperimentRaycastTarget parent;
    
    [SerializeField] private List<SRaycastTarget2> m_hits = new List<SRaycastTarget2>();
    public List<SRaycastTarget2> hits {
        get { return m_hits; }
        set {}
    }
    [SerializeField] private Dictionary<int,SCluster> m_clusters = new Dictionary<int,SCluster>();
    public Dictionary<int,SCluster> clusters {
        get { return m_clusters; }
        set {}
    }
    [SerializeField] private int minClusterSize, maxClusterSize;

    private void Awake() {
        experimentIDComp = GetComponent<ExperimentID>();
        // Check if we have a parent
        if (experimentIDComp.parent != null) {
            HelperMethods.HasComponent<ExperimentRaycastTarget>(experimentIDComp.parent.gameObject, out parent);
        }
    }

    public string GetID() {
        return experimentIDComp.id;
    }
    public string GetRefID() {
        return (experimentIDComp.ref_id.Length > 0) ? experimentIDComp.ref_id : experimentIDComp.id;
    }
    public string GetParentID() {
        return (parent != null) ? parent.GetID() : GetID();
    }
    public string GetParentIDOfRef() {
        if (experimentIDComp.ref_id.Length > 0) {
            ExperimentID parentIDComp;
            if (ExperimentGlobalController.current.FindID<ExperimentID>(experimentIDComp.ref_id, out parentIDComp)) {
                return (parentIDComp.parent != null) ? parentIDComp.parent.id : parentIDComp.id;
            } else {
                return GetParentID();
            }
        } else {
            return GetParentID();
        }
    }
    public Vector3 GetLocalPosition(Vector3 worldPosition) {
        return transform.InverseTransformPoint(worldPosition);
    }

    public void AddHit(SRaycastTarget2 newHit) {
        m_hits.Add(newHit);
        if (parent != null) parent.AddHit(newHit);
    }
    public void AddHits(List<SRaycastTarget2> newHits) {
        m_hits.AddRange(newHits);
        if (parent != null) parent.AddHits(newHits);
    }
    public void SetHits(List<SRaycastTarget2> newHits) {
        m_hits = newHits;
        if (parent != null) parent.AddHits(newHits);
    }

    public void SetClusters(Dictionary<int,SCluster> newClusters) {
        m_clusters = newClusters;
        if (m_clusters.Count == 0) return;
        SetClusterSizeDimensions();
    }
    private void SetClusterSizeDimensions() {
        int min = -1, max = -1;
        foreach(SCluster cluster in m_clusters.Values) {
            if (min == -1 || cluster.points.Count < min) min = cluster.points.Count;
            if (max == -1 || cluster.points.Count > max) max = cluster.points.Count;
        }
        minClusterSize = min;
        maxClusterSize = max;
    }

    public void DrawAllClusters(float maxClusterRadius) {
        foreach(SCluster cluster in m_clusters.Values) {
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.points.Count, (float)minClusterSize, (float)maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint((Vector3)cluster.center), radius);
        }
    }
    public void DrawAllClusters(float min, float max, float maxClusterRadius) {
        foreach(SCluster cluster in m_clusters.Values) {
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.points.Count, min, max, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint((Vector3)cluster.center), radius);
        }
    }
    /*
    public void DrawClustersAtTme(float minClusterSize, float maxClusterSize, float maxClusterRadius, float l) {
        foreach(SCluster cluster in m_clusters.Values) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.GetPointsCountAtTime(minClusterSize, maxClusterSize, l), minClusterSize, maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, radius);
        }
    }
    */
    /*
    public void DrawClustersInTimeRange() {
        foreach(SCluster cluster in clusters.Values) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.GetPointsCountInTimeRange(earliestPointBirthday,latestPointDeath,minTimeRange,maxTimeRange,hitLifespan), (float)minClusterSize, (float)maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, radius);
        }
    }
    */
}
