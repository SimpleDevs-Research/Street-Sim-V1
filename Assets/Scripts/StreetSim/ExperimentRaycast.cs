using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableTypes;
using Helpers;

[System.Serializable]
public class SCluster {
    public List<SRaycastTarget> points;
    public SVector3 center;
    private float distanceThreshold;
    public SCluster(float distanceThreshold, SRaycastTarget initialPoint) {
        this.distanceThreshold = distanceThreshold;
        this.points = new List<SRaycastTarget>();
        this.points.Add(initialPoint);
        CalculateCenter();
    }
    public SCluster(float distanceThreshold, List<SRaycastTarget> initialPoints) {
        this.distanceThreshold = distanceThreshold;
        this.points = initialPoints;
        CalculateCenter();
    } 
    private void CalculateCenter() {
        SVector3 pseudoCenter = new SVector3(0f,0f,0f);
        foreach(SRaycastTarget point in points) {
            pseudoCenter = pseudoCenter + point.worldPosition;
        }
        center = pseudoCenter / points.Count;
    }
    public bool CheckPointInRange(SRaycastTarget point, out float distance) {
        distance = (center - point.worldPosition).magnitude;
        return distance <= distanceThreshold;
    }
    public void AddPoint(SRaycastTarget point) {
        points.Add(point);
        CalculateCenter();
    }
}

[System.Serializable]
public class SRaycastTarget {
    public float timestamp;
    public SVector3 worldPosition;
    public SRaycastTarget(float timestamp, Vector3 worldPosition) {
        this.timestamp = timestamp;
        this.worldPosition = worldPosition;
    }
}

[RequireComponent(typeof(EVRA_Pointer))]
public class ExperimentRaycast : MonoBehaviour
{

    private EVRA_Pointer pointer;
    [SerializeField] private Transform target = null;
    [SerializeField] private List<SRaycastTarget> targetHits = new List<SRaycastTarget>();
    [SerializeField] private List<SCluster> clusters = new List<SCluster>();
    [SerializeField] private Queue<SRaycastTarget> findClusterQueue = new Queue<SRaycastTarget>();
    [SerializeField] private float distanceThreshold = 0.025f;
    [SerializeField] private float targetCheckDelay = 0.1f;

    private void Awake() {
        if (!HelperMethods.HasComponent<EVRA_Pointer>(this.gameObject, out pointer)) {
            pointer = this.gameObject.AddComponent<EVRA_Pointer>();
        }
        StartCoroutine(CheckTarget());
        StartCoroutine(CalculateClusters());
    }

    private IEnumerator CheckTarget() {
        while(true) {
            target = pointer.raycastTarget;
            if (target != null) {
                SRaycastTarget point = new SRaycastTarget(Time.time, pointer.raycastHitPosition);
                targetHits.Add(point);
                findClusterQueue.Enqueue(point);
            }
            yield return new WaitForSeconds(targetCheckDelay);
        }
    }

    private IEnumerator CalculateClusters() {
        while(true) {
            if (findClusterQueue.Count == 0) yield return null;
            else {
                SRaycastTarget point = findClusterQueue.Dequeue();
                if (clusters.Count == 0) {
                    // Create a cluster
                    SCluster newCluster = new SCluster(distanceThreshold, point);
                    clusters.Add(newCluster);
                    yield return null;
                } else {
                    // Check clusters
                    SCluster closestCluster = null;
                    float closestClusterDistance = 0f, currentClusterDistance = 0f;
                    foreach(SCluster cluster in clusters) {
                        if (cluster.CheckPointInRange(point, out currentClusterDistance)) {
                            if (closestCluster == null) {
                                closestCluster = cluster;
                                closestClusterDistance = currentClusterDistance;
                            } else if (currentClusterDistance < closestClusterDistance) {
                                closestCluster = cluster;
                                closestClusterDistance = currentClusterDistance;
                            }
                        }
                        yield return null;
                    }
                    if (closestCluster != null) {
                        closestCluster.AddPoint(point);
                    } else {
                        closestCluster = new SCluster(distanceThreshold, point);
                        clusters.Add(closestCluster);
                    }
                    yield return null;
                }
            }
        }
    }

    public void PrintClusters() {
        
    }

}
