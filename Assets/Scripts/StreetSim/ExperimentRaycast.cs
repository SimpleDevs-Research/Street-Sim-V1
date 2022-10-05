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
    public void CalculateCenter() {
        SVector3 pseudoCenter = new SVector3(0f,0f,0f);
        List<SRaycastTarget> onlyActives = new List<SRaycastTarget>();
        foreach(SRaycastTarget point in points) {
            if (!point.active) continue;
            onlyActives.Add(point);
            pseudoCenter = pseudoCenter + point.worldPosition;
        }
        points = onlyActives;
        if (onlyActives.Count == 0) return;
        center = pseudoCenter / (float)onlyActives.Count;
        points = onlyActives;
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
    public SVector3 normal;
    public int clusterIndex = -1;
    public bool active = true;
    public SRaycastTarget(float timestamp, Vector3 worldPosition, Vector3 normal) {
        this.timestamp = timestamp;
        this.worldPosition = worldPosition;
        this.normal = normal;
    }
}

[System.Serializable]
public class GazeDataPayload {
    public List<SRaycastTarget> gazePoints;
}

[RequireComponent(typeof(EVRA_Pointer))]
public class ExperimentRaycast : MonoBehaviour
{

    private EVRA_Pointer pointer;
    [SerializeField] private Transform target = null;
    [SerializeField] private Queue<SRaycastTarget> activeHits = new Queue<SRaycastTarget>();
    [SerializeField] private List<SRaycastTarget> allHits = new List<SRaycastTarget>();
    [SerializeField] private List<SCluster> clusters = new List<SCluster>();
    [SerializeField] private Queue<SRaycastTarget> findClusterQueue = new Queue<SRaycastTarget>();
    [SerializeField] private float distanceThreshold = 0.025f;
    [SerializeField] private float targetCheckDelay = 0.1f;
    [SerializeField] private float hitLifespan = 3f;
    private bool m_casting = false;
    private IEnumerator checkCoroutine = null, 
                        calculateCoroutine = null,
                        removeHitAfterDelayCoroutine = null;
    [SerializeField] private float minClusterSize = 0.025f;
    private float startTime;
    [SerializeField] private string m_dataFolder = "GazeData_ignore";
    [SerializeField] private string m_saveFilename = "gazeData_raw";
    [SerializeField] private string m_loadFilename = "gazeData_processed";

    private void OnDrawGizmos() {
        if (clusters.Count == 0) return;
        foreach(SCluster cluster in clusters) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, minClusterSize);
        }
    }

    private void Awake() {
        if (!HelperMethods.HasComponent<EVRA_Pointer>(this.gameObject, out pointer)) {
            pointer = this.gameObject.AddComponent<EVRA_Pointer>();
        }
    }

    private IEnumerator CheckTarget() {
        while(true) {
            target = pointer.raycastTarget;
            if (target != null) {
                SRaycastTarget point = new SRaycastTarget(Time.time - startTime, pointer.raycastHitPosition, pointer.raycastHitNormal);
                //activeHits.Enqueue(point);
                allHits.Add(point);
                //findClusterQueue.Enqueue(point);
            }
            yield return new WaitForSeconds(targetCheckDelay);
        }
    }

    /*
    private IEnumerator RemovePointAfterDelay() {
        SRaycastTarget point;
        bool restartDelay = true;
        while(true) {
            if (activeHits.Count == 0) {
                restartDelay = true;
                yield return null;
            }
            else {
                if (restartDelay) {
                    restartDelay = false;
                    yield return new WaitForSeconds(hitLifespan);
                }
                else {
                    yield return new WaitForSeconds(targetCheckDelay);
                }
                point = activeHits.Dequeue();
                point.active = false;
                clusters[point.clusterIndex].CalculateCenter();
            }
        }
    }
    */

    /*
    private IEnumerator CalculateClusters() {
        SCluster potentialCluster = null;
        int clusterIndex = -1;
        float closestClusterDistance, currentClusterDistance;
        while(true) {
            if (findClusterQueue.Count == 0) yield return null;
            else {
                SRaycastTarget point = findClusterQueue.Dequeue();
                if (clusters.Count == 0) {
                    // Create a cluster
                    potentialCluster = new SCluster(distanceThreshold, point);
                    clusters.Add(potentialCluster);
                    clusterIndex = clusters.IndexOf(potentialCluster);
                    point.clusterIndex = clusterIndex;
                    yield return null;
                } else {
                    // Check clusters
                    potentialCluster = null;
                    closestClusterDistance = 0f;
                    currentClusterDistance = 0f;
                    foreach(SCluster cluster in clusters) {
                        if (cluster.CheckPointInRange(point, out currentClusterDistance)) {
                            if (potentialCluster == null) {
                                potentialCluster = cluster;
                                closestClusterDistance = currentClusterDistance;
                            } else if (currentClusterDistance < closestClusterDistance) {
                                potentialCluster = cluster;
                                closestClusterDistance = currentClusterDistance;
                            }
                        }
                        yield return null;
                    }
                    if (potentialCluster != null) {
                        potentialCluster.AddPoint(point);
                    } else {
                        potentialCluster = new SCluster(distanceThreshold, point);
                        clusters.Add(potentialCluster);
                    }
                    clusterIndex = clusters.IndexOf(potentialCluster);
                    point.clusterIndex = clusterIndex;
                    yield return null;
                }
            }
        }
    }
    */

    public void StartCasting() {
        m_casting = true;
        startTime = Time.time;
        if (checkCoroutine == null) {
            checkCoroutine = CheckTarget();
            StartCoroutine(checkCoroutine);
        }
        /*
        if (calculateCoroutine == null) {
            calculateCoroutine = CalculateClusters();
            StartCoroutine(calculateCoroutine);
        }
        */
        /*
        if (removeHitAfterDelayCoroutine == null) {
            removeHitAfterDelayCoroutine = RemovePointAfterDelay();
            StartCoroutine(removeHitAfterDelayCoroutine);
        }
        */
    }
    public void EndCasting() {
        m_casting = false;
        if (checkCoroutine != null) {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }
        // Output the results to JSON
        SaveGazeData();
        /*
        if (calculateCoroutine != null) {
            StopCoroutine(calculateCoroutine);
            calculateCoroutine = null;
        }
        */
        /*
        if (removeHitAfterDelayCoroutine != null) {
            StopCoroutine(removeHitAfterDelayCoroutine);
            removeHitAfterDelayCoroutine = null;
        }
        */
    }

    public bool SaveGazeData() {
        // Create JSON
        GazeDataPayload payload = new GazeDataPayload();
        payload.gazePoints = allHits;
        string dataToSave = SaveSystemMethods.ConvertToJSON<GazeDataPayload>(payload);
        // Create Save Directory
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(m_dataFolder);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            SaveSystemMethods.SaveJSON(dirToSaveIn + m_saveFilename, dataToSave);
        }
        return true;
    }

}
