using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableTypes;
using Helpers;

[System.Serializable]
public class SCluster {
    public int clusterId;
    public List<SRaycastTarget> points;
    public SVector3 center;
    public SVector3 normal;
    public bool calibrated = false;
    public SCluster(int id) {
        this.clusterId = id;
        this.points = new List<SRaycastTarget>();
    }
    public void Calibrate() {
        calibrated = false;
        SVector3 pseudoCenter = new SVector3(0f,0f,0f);
        SVector3 pseudoNormal = new SVector3(0f,0f,0f);
        foreach(SRaycastTarget point in points) {
            pseudoCenter = pseudoCenter + point.worldPosition;
            pseudoNormal = pseudoNormal + point.normal;
        }
        center = pseudoCenter / (float)points.Count;
        normal = pseudoNormal / (float)points.Count;
        calibrated = true;
    }
    public int GetPointsCountAtTime(float earliestBirth, float latestDeath, float specificTime, float pointAge) {
        float pointBirthTime, pointDeathTime;
        int pointsCount = 0;
        foreach(SRaycastTarget point in points) {
            pointBirthTime = HelperMethods.Map(point.timestamp,earliestBirth,latestDeath,0f,1f);
            pointDeathTime = HelperMethods.Map(point.timestamp+pointAge,earliestBirth,latestDeath,0f,1f);
            if (specificTime >= pointBirthTime && specificTime <= pointDeathTime) pointsCount += 1;
        }
        return pointsCount;
    }
    public int GetPointsCountInTimeRange(float earliestBirth, float latestDeath, float beginning, float ending, float pointAge) {
        float pointBirthTime, pointDeathTime;
        int pointsCount = 0;
        foreach(SRaycastTarget point in points) {
            pointBirthTime = HelperMethods.Map(point.timestamp,earliestBirth,latestDeath,0f,1f);
            pointDeathTime = HelperMethods.Map(point.timestamp+pointAge,earliestBirth,latestDeath,0f,1f);
            if ((pointBirthTime >= beginning && pointBirthTime <= ending) || (pointDeathTime <= ending && pointDeathTime >= beginning)) pointsCount += 1;
        }
        return pointsCount;
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
    public float startTime;
    public float endTime;
    public List<SRaycastTarget> gazePoints;
}

[RequireComponent(typeof(EVRA_Pointer))]
public class ExperimentRaycast : MonoBehaviour
{

    private EVRA_Pointer pointer;
    [SerializeField] private Transform target = null;
    [SerializeField] private Queue<SRaycastTarget> activeHits = new Queue<SRaycastTarget>();
    [SerializeField] private List<SRaycastTarget> allHits = new List<SRaycastTarget>();
    [SerializeField] private Dictionary<int,SCluster> clusters = new Dictionary<int,SCluster>();
    [SerializeField] private Queue<SRaycastTarget> findClusterQueue = new Queue<SRaycastTarget>();
    [SerializeField] private float distanceThreshold = 0.025f;
    [SerializeField] private float targetCheckDelay = 0.1f;
    [SerializeField] private float hitLifespan = 3f;
    private bool m_casting = false;
    private IEnumerator checkCoroutine = null, 
                        calculateCoroutine = null,
                        removeHitAfterDelayCoroutine = null;
    [SerializeField] private float maxClusterRadius = 0.1f;
    private int minClusterSize, maxClusterSize;
    private float startTime, endTime;

    [Header("File Save/Load System")]
    [SerializeField] private string m_dataFolder = "GazeData";
    [SerializeField] private string m_saveFilename = "gazeData_raw";
    [SerializeField] private string m_loadFilename = "gazeData_processed";

    public enum VisualizationTimeRange {
        All,
        SpecificTime,
        TimeFrame
    }
    [Header("Visualization Controls")]
    [SerializeField] private VisualizationTimeRange m_showType = VisualizationTimeRange.All;
    private float earliestPointBirthday, latestPointDeath;
    [SerializeField, Range(0f,1f)] private float minTimeRange = 0f;
    [SerializeField, Range(0f,1f)] private float maxTimeRange = 1f; 
    [SerializeField, Range(0f,1f)] private float pinpointTime = 0f;

    private void OnDrawGizmos() {
        if (clusters.Count == 0) return;
        switch(m_showType) {
            case VisualizationTimeRange.All:
                DrawAllClusters();
                break;
            case VisualizationTimeRange.SpecificTime:
                DrawClustersAtTme();
                break;
            case VisualizationTimeRange.TimeFrame:
                DrawClustersInTimeRange();
                break;
        }
    }
    private void DrawAllClusters() {
        foreach(SCluster cluster in clusters.Values) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.points.Count, (float)minClusterSize, (float)maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, radius);
        }
    }
    private void DrawClustersAtTme() {
        foreach(SCluster cluster in clusters.Values) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.GetPointsCountAtTime(earliestPointBirthday,latestPointDeath,pinpointTime,hitLifespan), (float)minClusterSize, (float)maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, radius);
        }
    }
    private void DrawClustersInTimeRange() {
        foreach(SCluster cluster in clusters.Values) {
            //float radius = (cluster.points.Count / targetHits.Count) * maxClusterSize;
            if (!cluster.calibrated) continue;
            float radius = HelperMethods.Map((float)cluster.GetPointsCountInTimeRange(earliestPointBirthday,latestPointDeath,minTimeRange,maxTimeRange,hitLifespan), (float)minClusterSize, (float)maxClusterSize, 0.1f, 1f) * maxClusterRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector3)cluster.center, radius);
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
        endTime = Time.time;
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
        if (m_casting) {
            Debug.Log("CANNOT SAVE - CURRENTLY TRACKING");
            return false;
        }
        // Create JSON
        GazeDataPayload payload = new GazeDataPayload();
        payload.gazePoints = allHits;
        payload.startTime = startTime;
        payload.endTime = endTime;
        string dataToSave = SaveSystemMethods.ConvertToJSON<GazeDataPayload>(payload);
        // Create Save Directory
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(m_dataFolder);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            SaveSystemMethods.SaveJSON(dirToSaveIn + m_saveFilename, dataToSave);
        }
        return true;
    }
    public bool LoadGazeData() {
        // Get Directory
        GazeDataPayload payload;
        string filenameToLoad = SaveSystemMethods.GetSaveLoadDirectory(m_dataFolder) + m_loadFilename + ".json";
        Debug.Log("Loading " + filenameToLoad + " ...");
        if (SaveSystemMethods.CheckFileExists(filenameToLoad)) {
            if (SaveSystemMethods.LoadJSON<GazeDataPayload>(filenameToLoad, out payload)) {
                //Debug.Log(payload.gazePoints[0]);
                // Process clusters
                startTime = payload.startTime;
                endTime = payload.endTime;
                return CalibrateClusters(payload.gazePoints);
            } 
            else {
                Debug.Log("ERROR - COULD NOT LOAD FILE");
                return false;
            }
        } 
        else {
            Debug.Log("ERROR - LOAD FILE DOES NOT EXIST");
            return false;
        }
    }

    public bool CalibrateClusters(List<SRaycastTarget> raycastPoints) {
        int min = -1, max = -1;
        //float birth = -1f, death = -1f;
        Dictionary<int,SCluster> tempClusters = new Dictionary<int,SCluster>();
        foreach(SRaycastTarget point in raycastPoints) {
            if (!tempClusters.ContainsKey(point.clusterIndex)) {
                tempClusters.Add(point.clusterIndex, new SCluster(point.clusterIndex));
            }
            tempClusters[point.clusterIndex].points.Add(point);
            /*
            if (birth == -1f || point.timestamp < birth) {
                birth = point.timestamp;
            }
            if (death == -1f || point.timestamp+hitLifespan > death) {
                death = point.timestamp+hitLifespan;
            }
            */
        }
        foreach(SCluster cluster in tempClusters.Values) {
            cluster.Calibrate();
            if (min == -1 || cluster.points.Count < min) {
                min = cluster.points.Count;
            }
            if (max == -1 || cluster.points.Count > max) {
                max = cluster.points.Count;
            }
        }
        clusters = tempClusters;
        minClusterSize = min;
        maxClusterSize = max;
        //earliestPointBirthday = birth;
        //latestPointDeath = death;
        return true;
    }

}
