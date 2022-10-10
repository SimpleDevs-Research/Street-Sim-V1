using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializableTypes;
using Helpers;

[System.Serializable]
public class SCluster {
    public int clusterId;
    public List<SRaycastTarget2> points;
    public SVector3 center;
    public SVector3 normal;
    public bool calibrated = false;
    public SCluster(int id) {
        this.clusterId = id;
        this.points = new List<SRaycastTarget2>();
    }
    public void Calibrate() {
        calibrated = false;
        SVector3 pseudoCenter = new SVector3(0f,0f,0f);
        SVector3 pseudoNormal = new SVector3(0f,0f,0f);
        foreach(SRaycastTarget2 point in points) {
            pseudoCenter = pseudoCenter + point.localPosition;
            pseudoNormal = pseudoNormal + point.normal;
        }
        center = pseudoCenter / (float)points.Count;
        normal = pseudoNormal / (float)points.Count;
        calibrated = true;
    }
    public int GetPointsCountAtTime(float earliestBirth, float latestDeath, float specificTime, float pointAge) {
        float pointBirthTime, pointDeathTime;
        int pointsCount = 0;
        foreach(SRaycastTarget2 point in points) {
            pointBirthTime = HelperMethods.Map(point.timestamp,earliestBirth,latestDeath,0f,1f);
            pointDeathTime = HelperMethods.Map(point.timestamp+pointAge,earliestBirth,latestDeath,0f,1f);
            if (specificTime >= pointBirthTime && specificTime <= pointDeathTime) pointsCount += 1;
        }
        return pointsCount;
    }
    public int GetPointsCountInTimeRange(float earliestBirth, float latestDeath, float beginning, float ending, float pointAge) {
        float pointBirthTime, pointDeathTime;
        int pointsCount = 0;
        foreach(SRaycastTarget2 point in points) {
            pointBirthTime = HelperMethods.Map(point.timestamp,earliestBirth,latestDeath,0f,1f);
            pointDeathTime = HelperMethods.Map(point.timestamp+pointAge,earliestBirth,latestDeath,0f,1f);
            if ((pointBirthTime >= beginning && pointBirthTime <= ending) || (pointDeathTime <= ending && pointDeathTime >= beginning)) pointsCount += 1;
        }
        return pointsCount;
    }
}

[System.Serializable]
public class SRaycastTarget2 {
    public int index;
    public float timestamp;
    public string parentID;
    public string superParentID;
    public SVector3 localPosition;
    public SVector3 normal;
    public int clusterIndex = -1;
    public SRaycastTarget2(int index, string parentID, string superParentID, float timestamp, Vector3 localPosition, Vector3 normal) {
        this.index = index;
        this.parentID = parentID;
        this.superParentID = superParentID;
        this.timestamp = timestamp;
        this.localPosition = localPosition;
        this.normal = normal;
    }
}

[System.Serializable]
public class GazeDataPayload {
    public float startTime;
    public float endTime;
    public List<GazeDataTargetPayload> allData;
    public GazeDataPayload(float startTime, float endTime) {
        this.startTime = startTime;
        this.endTime = endTime;
        this.allData = new List<GazeDataTargetPayload>();
    }
}
[System.Serializable]
public class GazeDataTargetPayload {
    public string parentID;
    public List<SRaycastTarget2> gazePoints;
    public GazeDataTargetPayload(string parentID, List<SRaycastTarget2> gazePoints) {
        this.parentID = parentID;
        this.gazePoints = gazePoints;
    }
}

public class ExperimentRaycast : MonoBehaviour
{

    public static ExperimentRaycast current;

    [SerializeField] private EVRA_Pointer targetPointer, transformPointer;
    [SerializeField] private Transform targetPointerResult = null, transformPointerResult = null;
    [SerializeField] private List<ExperimentRaycastTarget> allTargets = new List<ExperimentRaycastTarget>();
    //[SerializeField] private Queue<SRaycastTarget> activeHits = new Queue<SRaycastTarget>();
    [SerializeField] private Dictionary<int,SRaycastTarget2> allHits = new Dictionary<int,SRaycastTarget2>();
    //[SerializeField] private Dictionary<int,SCluster> clusters = new Dictionary<int,SCluster>();
    //[SerializeField] private Queue<SRaycastTarget> findClusterQueue = new Queue<SRaycastTarget>();
    //[SerializeField] private float distanceThreshold = 0.025f;
    [SerializeField] private float targetCheckDelay = 0.1f;
    [SerializeField] private float hitLifespan = 3f;
    private bool m_casting = false;
    private IEnumerator checkCoroutine = null;
    private IEnumerator calculateCoroutine = null, removeHitAfterDelayCoroutine = null;
    [SerializeField] private float maxClusterRadius = 0.1f;
    private int minClusterSize, maxClusterSize;

    [Header("File Save/Load System")]
    [SerializeField] private string m_saveFilename = "gazeData_raw";
    public string saveFilename {
        get { return m_saveFilename; }
        set {}
    }
    [SerializeField] private string m_loadFilename = "gazeData_processed";
    public string loadFilename {
        get { return m_loadFilename; }
        set {}
    }

    public enum VisualizationLocality {
        Globally,
        Locally
    }
    public enum VisualizationTimeRange {
        All,
        SpecificTime,
        TimeFrame
    }
    [Header("Visualization Controls")]
    [SerializeField] private VisualizationLocality m_showLocality = VisualizationLocality.Globally;
    [SerializeField] private VisualizationTimeRange m_showType = VisualizationTimeRange.All;
    private float earliestPointBirthday, latestPointDeath;
    [SerializeField, Range(0f,1f)] private float minTimeRange = 0f;
    [SerializeField, Range(0f,1f)] private float maxTimeRange = 1f; 
    [SerializeField, Range(0f,1f)] private float pinpointTime = 0f;

    private void OnDrawGizmos() {
        if (allTargets.Count == 0) return;
        switch(m_showType) {
            case VisualizationTimeRange.All:
                switch(m_showLocality) {
                    case VisualizationLocality.Globally:
                        foreach(ExperimentRaycastTarget t in allTargets) {
                            t.DrawAllClusters((float)minClusterSize,(float)maxClusterSize,maxClusterRadius);
                        }
                        break;
                    case VisualizationLocality.Locally:
                        foreach(ExperimentRaycastTarget t in allTargets) {
                            t.DrawAllClusters(maxClusterRadius);
                        }
                        break;
                }
                break;
            /*
            case VisualizationTimeRange.SpecificTime:
                foreach(ExperimentRaycastTarget t in allTargets) {
                    t.DrawClustersAtTme((float)minClusterSize,(float)maxClusterSize,maxClusterRadius,hitLifespan);
                }
                break;
            */
            /*
            case VisualizationTimeRange.TimeFrame:
                DrawClustersInTimeRange();
                break;
            */
        }
    }
    /*
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
    */

    private void Awake() {
        current = this;
    }

    private ExperimentRaycastTarget targetComp, transformComp;
    string parentID, superParentID;
    Vector3 parentLocalPos;

    public void CheckTargetHit() {
        if (!m_casting) return;
        targetPointerResult = targetPointer.raycastTarget;
        transformPointerResult = transformPointer.raycastTarget;

        if (
            targetPointerResult != null 
            && HelperMethods.HasComponent<ExperimentRaycastTarget>(targetPointerResult.gameObject, out targetComp)
            && transformPointerResult != null 
            && HelperMethods.HasComponent<ExperimentRaycastTarget>(transformPointerResult.gameObject, out transformComp)
        ) {
            if (transformComp.GetParentID() != targetComp.GetID()) return;
            if (!allTargets.Contains(transformComp)) allTargets.Add(transformComp);
            parentID = transformComp.GetRefID();
            superParentID = transformComp.GetParentIDOfRef();
            parentLocalPos = transformComp.GetLocalPosition(targetPointer.raycastHitPosition);
            SRaycastTarget2 point = new SRaycastTarget2(
                ExperimentGlobalController.current.currentIndex,
                parentID, 
                superParentID,
                ExperimentGlobalController.current.currentTime - ExperimentGlobalController.current.startTime, 
                parentLocalPos, 
                targetPointer.raycastHitNormal
            );
            allHits.Add(ExperimentGlobalController.current.currentIndex,point);
            transformComp.AddHit(point);
        }
    }

    private Vector3 FindUp(Vector3 normal) {
        normal.Normalize();
        if (normal == Vector3.zero) return Vector3.up;
        if (normal == Vector3.up) return Vector3.forward;
        float distance = -Vector3.Dot(normal, Vector3.up);
        return (Vector3.up + normal * distance).normalized;
    }

    public void StartCasting() {
        m_casting = true;
    }
    public void EndCasting() {
        m_casting = false;
    }

    public void SaveGazeData() {
        if (m_casting) {
            Debug.Log("CANNOT SAVE - CURRENTLY TRACKING");
            return;
        }
        
        // Create JSON
        GazeDataPayload payload = new GazeDataPayload(
            ExperimentGlobalController.current.startTime,
            ExperimentGlobalController.current.endTime
        );
        foreach(ExperimentRaycastTarget target in allTargets) {
            payload.allData.Add(new GazeDataTargetPayload(target.GetRefID(),target.hits));
        }
        string dataToSave = SaveSystemMethods.ConvertToJSON<GazeDataPayload>(payload);
        
        // Create Save Directory
        string dirToSaveIn = SaveSystemMethods.GetSaveLoadDirectory(ExperimentGlobalController.current.directoryPath);
        if (SaveSystemMethods.CheckOrCreateDirectory(dirToSaveIn)) {
            SaveSystemMethods.SaveJSON(dirToSaveIn + m_saveFilename, dataToSave);
        }
    }
    public void LoadGazeData() {
        // We can only load if we're running the game...
        if (ExperimentGlobalController.current == null) {
            Debug.Log("ERROR - Must be running the game before loading");
            return;
        }
        // Get Directory
        GazeDataPayload payload;
        string filenameToLoad = SaveSystemMethods.GetSaveLoadDirectory(ExperimentGlobalController.current.directoryPath) + m_loadFilename + ".json";
        Debug.Log("Loading " + filenameToLoad + " ...");
        if (!SaveSystemMethods.CheckFileExists(filenameToLoad)) {
            Debug.Log("ERROR - LOAD FILE DOES NOT EXIST");
            return;
        }
        if (!SaveSystemMethods.LoadJSON<GazeDataPayload>(filenameToLoad, out payload)) {
            Debug.Log("ERROR - COULD NOT LOAD FILE");
            return;
        }
        CalibrateClusters(payload.allData); 
    }

    public void CalibrateClusters(List<GazeDataTargetPayload> targetPayloads) {
        allTargets = new List<ExperimentRaycastTarget>();
        allHits = new Dictionary<int,SRaycastTarget2>();
        int min = -1, max = -1;
        ExperimentRaycastTarget possibleTarget;
        List<SRaycastTarget2> targs;
        foreach(GazeDataTargetPayload payload in targetPayloads) {
            // First check if our item exists
            if (ExperimentGlobalController.current.FindID<ExperimentRaycastTarget>(payload.parentID, out possibleTarget)) {
                Dictionary<int,SCluster> tempClusters = new Dictionary<int,SCluster>();
                foreach(SRaycastTarget2 point in payload.gazePoints) {
                    allHits.Add(point.index,point);
                }
                //allHits.AddRange(payload.gazePoints);
                possibleTarget.SetHits(payload.gazePoints);
                foreach(SRaycastTarget2 point in payload.gazePoints) {
                    if (!tempClusters.ContainsKey(point.clusterIndex)) {
                        tempClusters.Add(point.clusterIndex, new SCluster(point.clusterIndex));
                    }
                    tempClusters[point.clusterIndex].points.Add(point);
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
                possibleTarget.SetClusters(tempClusters);
                allTargets.Add(possibleTarget);
            }
        }
        minClusterSize = min;
        maxClusterSize = max;
    }
}
