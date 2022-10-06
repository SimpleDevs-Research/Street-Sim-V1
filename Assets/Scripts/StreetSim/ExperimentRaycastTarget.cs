using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

[RequireComponent(typeof(ExperimentID))]
public class ExperimentRaycastTarget : MonoBehaviour
{
    private ExperimentID experimentIDComp;
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

    private void Awake() {
        experimentIDComp = GetComponent<ExperimentID>();
    }

    public string GetID() {
        return experimentIDComp.id;
    }
    public Vector3 GetLocalPosition(Vector3 worldPosition) {
        return transform.InverseTransformPoint(worldPosition);
    }

    public void AddHit(SRaycastTarget2 newHit) {
        m_hits.Add(newHit);
    }

    public void SetHits(List<SRaycastTarget2> newHits) {
        m_hits = newHits;
    }
    public void SetClusters(Dictionary<int,SCluster> newClusters) {
        m_clusters = newClusters;
        Debug.Log(m_clusters.Count);
    }

    public void DrawAllClusters(float minClusterSize, float maxClusterSize, float maxClusterRadius) {
        foreach(SCluster cluster in m_clusters.Values) {
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.points.Count, minClusterSize, maxClusterSize, 0.1f, 1f) * maxClusterRadius;
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
