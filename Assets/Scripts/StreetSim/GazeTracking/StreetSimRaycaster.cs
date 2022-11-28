using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;
using UnityEditor;

[System.Serializable]
public class RaycastHitRow {
    public int frameIndex;
    public float timestamp;
    public int triangleIndex;
    public string hitID;
    public string agentID;
    public float[] localPositionOfHitPosition;
    public float localPositionOfHitPosition_x, localPositionOfHitPosition_y, localPositionOfHitPosition_z;
    public float[] localPositionOfHitTarget;
    public float localPositionOfHitTarget_x, localPositionOfHitTarget_y, localPositionOfHitTarget_z;
    public float[] localPosition;
    public float localPosition_x, localPosition_y, localPosition_z;
    public float[] raycastDirection;
    public float raycastDirection_x, raycastDirection_y, raycastDirection_z;
    public RaycastHitRow(int index, float t, int i, string h, string a, float[] lpp, float[] lpt, float[] lp, float[] rd) {
        this.frameIndex = index;
        this.timestamp = t;
        this.triangleIndex = i;
        this.hitID = h;
        this.agentID = a;
        this.localPositionOfHitPosition = lpp;
        this.localPositionOfHitPosition_x = this.localPositionOfHitPosition[0];
        this.localPositionOfHitPosition_y = this.localPositionOfHitPosition[1];
        this.localPositionOfHitPosition_z = this.localPositionOfHitPosition[2];
        this.localPositionOfHitTarget = lpt;
        this.localPositionOfHitTarget_x = this.localPositionOfHitTarget[0];
        this.localPositionOfHitTarget_y = this.localPositionOfHitTarget[1];
        this.localPositionOfHitTarget_z = this.localPositionOfHitTarget[2];
        this.localPosition = lp;
        this.localPosition_x = this.localPosition[0];
        this.localPosition_y = this.localPosition[1];
        this.localPosition_z = this.localPosition[2];
        this.raycastDirection = rd;
        this.raycastDirection_x = this.raycastDirection[0];
        this.raycastDirection_y = this.raycastDirection[1];
        this.raycastDirection_z = this.raycastDirection[2];
    }
    public RaycastHitRow(string[] data) {
        this.frameIndex = Int32.Parse(data[0]);
        this.timestamp = float.Parse(data[1]);
        this.triangleIndex = Int32.Parse(data[2]);
        this.hitID = data[3];
        this.agentID = data[4];
        this.localPositionOfHitPosition_x = float.Parse(data[5]);
        this.localPositionOfHitPosition_y = float.Parse(data[6]);
        this.localPositionOfHitPosition_z = float.Parse(data[7]);
        this.localPositionOfHitPosition = new float[3]{
            this.localPositionOfHitPosition_x,
            this.localPositionOfHitPosition_y,
            this.localPositionOfHitPosition_z
        };
        this.localPositionOfHitTarget_x = float.Parse(data[8]);
        this.localPositionOfHitTarget_y = float.Parse(data[9]);
        this.localPositionOfHitTarget_z = float.Parse(data[10]);
        this.localPositionOfHitTarget = new float[3]{
            this.localPositionOfHitTarget_x,
            this.localPositionOfHitTarget_y,
            this.localPositionOfHitTarget_z
        };
        this.localPosition_x = float.Parse(data[11]);
        this.localPosition_y = float.Parse(data[12]);
        this.localPosition_z = float.Parse(data[13]);
        this.localPosition = new float[3]{
            this.localPosition_x, 
            this.localPosition_y, 
            this.localPosition_z
        };
        this.raycastDirection_x = float.Parse(data[14]);
        this.raycastDirection_y = float.Parse(data[15]);
        this.raycastDirection_z = float.Parse(data[16]);
        this.raycastDirection = new float[3]{
            this.raycastDirection_x,
            this.raycastDirection_y,
            this.raycastDirection_z
        };
    }
    public static List<string> Headers => new List<string> {
        "frameIndex",
        "timestamp",
        "triangleIndex",
        "hitID",
        "agentID",
        "localPositionOfHitPosition_x",
        "localPositionOfHitPosition_y",
        "localPositionOfHitPosition_z",
        "localPositionOfHitTarget_x",
        "localPositionOfHitTarget_y",
        "localPositionOfHitTarget_z",
        "localPosition_x",
        "localPosition_y",
        "localPosition_z",
        "raycastDirection_x",
        "raycastDirection_y",
        "raycastDirection_z",
    };
    public string ToString() {
        return 
            this.hitID + "-" + 
            this.frameIndex.ToString() + "-" +
            this.timestamp.ToString() + "-" +
            this.localPosition.ToString();
    }
}

[System.Serializable]
public class RaycastHitReplayRow {
    public int frameIndex;
    public float timestamp;
    public string hitID;
    public string agentID;
    public Vector3 worldPosition;
    public RaycastHitReplayRow(int index, float t, string h, string a, Vector3 p) {
        this.frameIndex = index;
        this.timestamp = t;
        this.hitID = h;
        this.agentID = a;
        this.worldPosition = p;
    }
    public static List<string> Headers => new List<string> {
        "frameIndex",
        "timestamp",
        "hitID",
        "agentID",
        "worldPosition"
    };
}

[System.Serializable]
public class LoadedGazeData {
    public string trialName;
    public TextAsset textAsset;
    public Dictionary<int, float> indexTimeMap;
    public Dictionary<float, List<RaycastHitRow>> gazeDataByTimestamp;
    public LoadedGazeData(
        string trialName, 
        TextAsset textAsset, 
        List<RaycastHitRow> gazes
    ) {
        this.trialName = trialName;
        this.textAsset = textAsset;

        this.indexTimeMap = new Dictionary<int, float>();
        this.gazeDataByTimestamp = new Dictionary<float, List<RaycastHitRow>>();

        foreach(RaycastHitRow gaze in gazes) {
            // Find the experiment ID that matches
            ExperimentID hitID = StreetSimIDController.ID.FindIDFromName(gaze.hitID);
            ExperimentID agentID = StreetSimIDController.ID.FindIDFromName(gaze.agentID);
            if (hitID == null || agentID == null) {
                Debug.Log("[RAYCASTER] Error: Could not find an ExperimentID that matches either the hitID or agentID...");
                Debug.Log(gaze.ToString());
                continue;
            }
            // trackable has access to frameIndex + timestamp, Let's add them
            if (!this.indexTimeMap.ContainsKey(gaze.frameIndex)) this.indexTimeMap.Add(gaze.frameIndex, gaze.timestamp);
            // Add this to positionDatabyTimestamp
            if (!this.gazeDataByTimestamp.ContainsKey(gaze.timestamp)) this.gazeDataByTimestamp.Add(gaze.timestamp, new List<RaycastHitRow>());
            if (!this.gazeDataByTimestamp[gaze.timestamp].Contains(gaze)) this.gazeDataByTimestamp[gaze.timestamp].Add(gaze);
        }
    }
}

public class StreetSimRaycaster : MonoBehaviour
{
    public static StreetSimRaycaster R;
    private bool m_initialized = false;
    public bool initialized { get=>m_initialized; set{} }

    [SerializeField] private EVRA_Pointer pointer;
    [SerializeField] private List<RaycastHitRow> m_hits = new List<RaycastHitRow>();
    public List<RaycastHitRow> hits { get { return m_hits; } set{} }

    [SerializeField] private float m_timestamp;
    [SerializeField] private int m_triangleIndex;
    [SerializeField] private string m_hitID;
    [SerializeField] private string m_agentID;
    [SerializeField] private SVector3 m_localPositionOfHitPosition;
    [SerializeField] private SVector3 m_localPositionOfHitTarget;
    [SerializeField] private SVector3 m_localPosition;

    [SerializeField] private LayerMask layerMask;
    [SerializeField] private ExperimentID currentTarget;
    [SerializeField] private bool debugIndependently;

    private IEnumerator replayCoroutine = null;
    private Dictionary<float, List<GazePoint>> cubeGazeObjects = new Dictionary<float, List<GazePoint>>();
    private Dictionary<float, List<GazePoint>> rectGazeObjects = new Dictionary<float, List<GazePoint>>();
    private Dictionary<float, List<GazePoint>> sphereGazeObjects = new Dictionary<float, List<GazePoint>>();
    public Dictionary<float, bool> discretizationToggles = new Dictionary<float, bool>();
    private bool m_showCubeGaze = true, m_showRectGaze = true, m_showSphereGaze = true;
    public bool showCubeGaze { get=>m_showCubeGaze; set{} }
    public bool showRectGaze { get=>m_showRectGaze; set{} }
    public bool showSphereGaze { get=>m_showSphereGaze; set{} }

    private List<GazePoint> gazeGazeObjects = new List<GazePoint>();

    void OnDrawGizmosSelected() {
        if (cubeGazeObjects.Count > 0 && m_showCubeGaze) {
            foreach(List<GazePoint> points in cubeGazeObjects.Values) {
                foreach(GazePoint point in points) {
                    if (!point.gameObject.activeInHierarchy) continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(point.originPoint, point.transform.position - point.originPoint);
                }
            }
        }
        if (rectGazeObjects.Count > 0 && m_showRectGaze) {
            foreach(List<GazePoint> points in rectGazeObjects.Values) {
                foreach(GazePoint point in points) {
                    if (!point.gameObject.activeInHierarchy) continue;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(point.originPoint, point.transform.position - point.originPoint);
                }
            }
        }
        if (sphereGazeObjects.Count > 0 && m_showSphereGaze) {
            foreach(List<GazePoint> points in sphereGazeObjects.Values) {
                foreach(GazePoint point in points) {
                    if (!point.gameObject.activeInHierarchy) continue;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(point.originPoint, point.transform.position - point.originPoint);
                }
            }
        }
    }

    private void Awake() {
        R = this;
        m_initialized = true;
    }

    private void Update() {
        if (debugIndependently) CheckRaycast();
    }

    // This only runs when `StreetSim` calls it
    public void CheckRaycast() {
        if (pointer == null) return;
        // Get pointer data
        ExperimentID target, closestTarget;
        //RaycastHit[] hits = Physics.SphereCastAll(pointer.transform.position, 0.1f, pointer.transform.forward, 100f, layerMask);
        RaycastHit[] hits = Physics.RaycastAll(pointer.transform.position, pointer.transform.forward, 100f, layerMask);
        // RaycastHit[] potentials = Physics.SphereCastAll(pointer.transform.position, 1f, pointer.transform.forward, 50f, layerMask);
        //if (potentials.Length > 0 && CalculateClosestTarget(potentials, pointer.transform.position, pointer.transform.forward, out hit, out target)) {
        //RaycastHit hit;
        //if (Physics.SphereCast(pointer.transform.position, 0.1f, pointer.transform.forward, out hit, 100f, layerMask)) {
        if (hits.Length > 0) {
            foreach(RaycastHit hit in hits) {
                if (HelperMethods.HasComponent<ExperimentID>(hit.transform, out target)) {
                    currentTarget = target;
                    m_triangleIndex = hit.triangleIndex;
                    m_hitID = GetClosestPoint(hit.point, target, out closestTarget);
                    m_agentID = target.ref_id;
                    m_localPositionOfHitPosition = closestTarget.transform.InverseTransformPoint(hit.point);
                    m_localPositionOfHitTarget = (closestTarget.parent != null) ? closestTarget.transform.localPosition : Vector3.zero;
                    //m_localPosition = m_localPositionOfHitPosition + m_localPositionOfHitTarget;
                    m_localPosition = (closestTarget.parent != null) 
                        ? closestTarget.parent.transform.InverseTransformPoint(hit.point) 
                        : m_localPositionOfHitPosition;
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
                            },
                            new float[3]{
                                m_localPosition.x,
                                m_localPosition.y,
                                m_localPosition.z
                            },
                            new float[3]{
                                pointer.transform.forward.x,
                                pointer.transform.forward.y,
                                pointer.transform.forward.z
                            }
                        )
                    );
                } else {
                    currentTarget = null;
                }
            }
        } else {
            currentTarget = null;
        }
    }

    public bool CheckRaycastManual(Vector3 startPos, Vector3 dir, LayerMask manualLayerMask, int frameIndex, float timestamp, out RaycastHitRow row) {
        ExperimentID target, closestTarget, parentTarget;
        RaycastHit hit;
        if(Physics.Raycast(startPos, dir, out hit, 100f, manualLayerMask)) {
            if (!HelperMethods.HasComponent<ExperimentID>(hit.transform, out target)) {
                row = default(RaycastHitRow);
                return false;
            }
            m_triangleIndex = hit.triangleIndex;
            m_hitID = GetClosestPoint(hit.point, target, out closestTarget);
            m_agentID = target.ref_id;
            m_localPositionOfHitPosition = closestTarget.transform.InverseTransformPoint(hit.point);
            m_localPositionOfHitTarget = (closestTarget.parent != null) ? closestTarget.transform.localPosition : Vector3.zero;
            //m_localPosition = m_localPositionOfHitPosition + m_localPositionOfHitTarget;
            m_localPosition = (closestTarget.parent != null) 
                ? closestTarget.parent.transform.InverseTransformPoint(hit.point) 
                : m_localPositionOfHitPosition;
            row = new RaycastHitRow(
                frameIndex,
                timestamp,
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
                },
                new float[3]{
                    m_localPosition.x,
                    m_localPosition.y,
                    m_localPosition.z
                },
                new float[3]{
                    dir.x,
                    dir.y,
                    dir.z
                }
            );
            return true;
        }
        row = default(RaycastHitRow);
        return false;
    }

    public bool CheckRaycastManualAll(Vector3 startPos, Vector3 dir, LayerMask manualLayerMask, int frameIndex, float timestamp, out List<RaycastHitReplayRow> rows) {
        rows = new List<RaycastHitReplayRow>();
        ExperimentID target, closestTarget, parentTarget;
        RaycastHitReplayRow row;
        RaycastHit[] hits;
        hits = Physics.RaycastAll(startPos, dir, 100f, manualLayerMask);
        if (hits.Length == 0) return false;
        foreach(RaycastHit hit in hits) {
            if (!HelperMethods.HasComponent<ExperimentID>(hit.transform, out target)) continue;
            m_triangleIndex = hit.triangleIndex;
            m_hitID = GetClosestPoint(hit.point, target, out closestTarget);
            m_agentID = target.ref_id;
            row = new RaycastHitReplayRow(frameIndex, timestamp, m_hitID, m_agentID, hit.point);
            rows.Add(row);
        }
        return rows.Count > 0;
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

    private bool CalculateClosestTarget(RaycastHit[] potentials, Vector3 pos, Vector3 dir, out RaycastHit target, out ExperimentID targetID) {
        RaycastHit closestHit = default(RaycastHit);
        ExperimentID closestID = null, potentialID = null;
        float closestDistance = Mathf.Infinity, currentDistance = 0f;
        foreach(RaycastHit hit in potentials) {
            if (HelperMethods.HasComponent<ExperimentID>(hit.transform.gameObject, out potentialID)) {
                currentDistance = (Vector3.Cross(dir, hit.point - pos)).magnitude;
                if (closestID == null || currentDistance < closestDistance) {
                    closestHit = hit;
                    closestID = potentialID;
                    closestDistance = currentDistance;
                }
            }
        }
        target = closestHit;
        targetID = closestID;
        return (closestID != null);
    }

    public void ClearData() {
        m_hits = new List<RaycastHitRow>();
    }


    public bool LoadGazePath(LoadedSimulationDataPerTrial trial, out LoadedGazeData newData) {
        string assetPath = trial.assetPath+"/gaze.csv";
        if (!SaveSystemMethods.CheckFileExists(assetPath)) {
            Debug.Log("[RAYCASTER] ERROR: Cannot load textasset \""+assetPath+"\"!");
            newData = default(LoadedGazeData);
            return false;
        }
        TextAsset ta = (TextAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
        string[] pr = SaveSystemMethods.ReadCSV(ta);
        List<RaycastHitRow> p = ParseGazeData(pr);
        newData = new LoadedGazeData(trial.trialName, ta, p);
        return true;
    }
    private List<RaycastHitRow> ParseGazeData(string[] data){
        List<RaycastHitRow> dataFormatted = new List<RaycastHitRow>();
        int numHeaders = RaycastHitRow.Headers.Count;
        int tableSize = data.Length/numHeaders - 1;
      
        for(int i = 0; i < tableSize; i++) {
            int rowKey = numHeaders*(i+1);
            string[] row = data.RangeSubset(rowKey,numHeaders);
            dataFormatted.Add(new RaycastHitRow(row));
        }
        return dataFormatted;
    }


    public void ReplayRecord(LoadedSimulationDataPerTrial trial) {
        if (replayCoroutine != null) {
            StopCoroutine(replayCoroutine);
            ResetReplay();
        }
        replayCoroutine = Replay(trial);
        StartCoroutine(replayCoroutine);
    }
    public IEnumerator Replay(LoadedSimulationDataPerTrial trial) {
        StreetSimLoadSim.LS.gazeCube.position = StreetSimLoadSim.LS.cam360.position;
        StreetSimLoadSim.LS.gazeCube.rotation = Quaternion.identity;
        StreetSimLoadSim.LS.gazeCube.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in StreetSimLoadSim.LS.gazeCube) child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");

        StreetSimLoadSim.LS.gazeRect.position = StreetSimLoadSim.LS.cam360.position;
        StreetSimLoadSim.LS.gazeRect.rotation = Quaternion.identity;
        StreetSimLoadSim.LS.gazeRect.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in StreetSimLoadSim.LS.gazeRect) child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");

        Vector3 positionMultiplier = Vector3.one;
        if (trial.trialData.direction == "NorthToSouth") {
            StreetSimLoadSim.LS.gazeCube.rotation = Quaternion.AngleAxis(180, Vector3.up);
            StreetSimLoadSim.LS.gazeRect.rotation = Quaternion.AngleAxis(180, Vector3.up);
            positionMultiplier = new Vector3(-1f,1f,-1f);
        }
        
        StreetSimLoadSim.LS.userImitator.gameObject.SetActive(true);

        List<int> order = new List<int>(trial.positionData.indexTimeMap.Keys);
        order.Sort((a,b) => a.CompareTo(b));
        int count = order.Count;
        int index = -1;
        float prevTimestamp = 0f;

        cubeGazeObjects = new Dictionary<float, List<GazePoint>>();
        rectGazeObjects = new Dictionary<float, List<GazePoint>>();
        sphereGazeObjects = new Dictionary<float, List<GazePoint>>();
        discretizationToggles = new Dictionary<float, bool>();

        ExperimentID userID = StreetSimIDController.ID.FindIDFromName("User");
        if (userID == null) {
            Debug.Log("[RAYCASTER] ERROR: Cannot find user's ExperimentID");
            yield break;
        }

        while(index < count-1) {
            index++;
            int frameIndex = order[index];
            float timestamp = trial.positionData.indexTimeMap[frameIndex];
            
            StreetSimLoadSim.LS.userImitator.localPosition = trial.positionData.positionDataByTimestamp[timestamp][userID].localPosition;
            StreetSimLoadSim.LS.userImitator.localRotation = trial.positionData.positionDataByTimestamp[timestamp][userID].localRotation;
            
            //Vector3 zDiscretizationPosition = Vector3.Scale(StreetSimLoadSim.LS.userImitator.position, positionMultiplier);
            Vector3 zDiscretizationPosition = StreetSimLoadSim.LS.userImitator.position;
            float zDiscretization = Mathf.Round(zDiscretizationPosition.z);

            StreetSimLoadSim.LS.gazeCube.position = new Vector3(StreetSimLoadSim.LS.cam360.position.x, StreetSimLoadSim.LS.cam360.position.y, zDiscretization);
            StreetSimLoadSim.LS.gazeRect.position = new Vector3(StreetSimLoadSim.LS.cam360.position.x, StreetSimLoadSim.LS.cam360.position.y, zDiscretization);

            zDiscretization *= positionMultiplier.z;
            if (!cubeGazeObjects.ContainsKey(zDiscretization)) {
                cubeGazeObjects.Add(zDiscretization, new List<GazePoint>());
            }
            if (!rectGazeObjects.ContainsKey(zDiscretization)) {
                rectGazeObjects.Add(zDiscretization, new List<GazePoint>());
            }
            if (!sphereGazeObjects.ContainsKey(zDiscretization)) {
                sphereGazeObjects.Add(zDiscretization, new List<GazePoint>());
            }
            if (!discretizationToggles.ContainsKey(zDiscretization)) {
                discretizationToggles.Add(zDiscretization, true);
            }
            
            List<RaycastHitReplayRow> rows;
                if (CheckRaycastManualAll(StreetSimLoadSim.LS.userImitator.position,StreetSimLoadSim.LS.userImitator.forward, StreetSimLoadSim.LS.gazeMask, frameIndex, timestamp, out rows)) {
                    GazePoint cubePoint = null;
                    // Instantiate gaze point for both cube and rect
                    foreach(RaycastHitReplayRow row in rows) {
                        GazePoint newGazeObject = Instantiate(StreetSimLoadSim.LS.gazePointPrefab) as GazePoint;
                        newGazeObject.originPoint = Vector3.Scale(StreetSimLoadSim.LS.userImitator.position, positionMultiplier);
                        newGazeObject.transform.position = Vector3.Scale(row.worldPosition,positionMultiplier);
                        newGazeObject.transform.rotation = Quaternion.identity;
                        newGazeObject.transform.localScale = Vector3.one * (0.025f *  (newGazeObject.transform.position-StreetSimLoadSim.LS.cam360.position).magnitude);
                        if (row.agentID == "GazeCube") {
                            newGazeObject.SetColor(Color.red);
                            cubeGazeObjects[zDiscretization].Add(newGazeObject);
                            newGazeObject.gameObject.SetActive(m_showCubeGaze);
                            cubePoint = newGazeObject;
                            newGazeObject.CalculateScreenPoint(zDiscretization, StreetSimLoadSim.LS.cam360.GetComponent<Camera>(),StreetSimLoadSim.LS.gazeCube);
                        } if (row.agentID == "GazeRect") {
                            newGazeObject.SetColor(Color.blue);
                            rectGazeObjects[zDiscretization].Add(newGazeObject);
                            newGazeObject.gameObject.SetActive(m_showRectGaze);
                            newGazeObject.CalculateScreenPoint(zDiscretization, StreetSimLoadSim.LS.cam360.GetComponent<Camera>(),StreetSimLoadSim.LS.gazeRect);
                        }
                    }
                    // Instantiate gaze point for sphere
                    if (cubePoint != null) {
                        GazePoint sphereGazeObject = Instantiate(StreetSimLoadSim.LS.gazePointPrefab) as GazePoint;
                        sphereGazeObject.originPoint = cubePoint.originPoint;
                        sphereGazeObject.transform.position = cubePoint.transform.position;
                        sphereGazeObject.transform.rotation = Quaternion.identity;
                        // alter localposition
                        Vector3 dir = sphereGazeObject.transform.position - sphereGazeObject.originPoint;
                        sphereGazeObject.transform.position = sphereGazeObject.originPoint + (dir.normalized * Mathf.Min(10f, dir.magnitude));
                        sphereGazeObject.transform.localScale = Vector3.one * (0.025f * (sphereGazeObject.transform.position - StreetSimLoadSim.LS.cam360.position).magnitude);
                        sphereGazeObject.SetColor(Color.yellow);
                        sphereGazeObjects[zDiscretization].Add(sphereGazeObject);
                        sphereGazeObject.gameObject.SetActive(m_showSphereGaze);
                        sphereGazeObject.CalculateScreenPoint(zDiscretization, StreetSimLoadSim.LS.cam360.GetComponent<Camera>(),null);
                    }
                }
            yield return null;
        }
    }
    public void ResetReplay() {

        StreetSimLoadSim.LS.cam360.position = new Vector3(0f,1.5f,0f);

        StreetSimLoadSim.LS.gazeCube.position = new Vector3(0f, StreetSimLoadSim.LS.cam360.position.y - 20f, 0f);
        StreetSimLoadSim.LS.gazeCube.rotation = Quaternion.identity;
        StreetSimLoadSim.LS.gazeCube.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");
        foreach(Transform child in StreetSimLoadSim.LS.gazeCube) child.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");

        StreetSimLoadSim.LS.gazeRect.position = new Vector3(0f, StreetSimLoadSim.LS.cam360.position.y - 20f, 0f);
        StreetSimLoadSim.LS.gazeRect.rotation = Quaternion.identity;
        StreetSimLoadSim.LS.gazeRect.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");
        foreach(Transform child in StreetSimLoadSim.LS.gazeRect) child.gameObject.layer = LayerMask.NameToLayer("ExperimentRaycastTarget");

        StreetSimLoadSim.LS.userImitator.position = Vector3.zero;
        StreetSimLoadSim.LS.userImitator.rotation = Quaternion.identity;
        StreetSimLoadSim.LS.userImitator.gameObject.SetActive(false);

        foreach(List<GazePoint> points in cubeGazeObjects.Values) {
            while(points.Count > 0) {
                GazePoint g = points[0];
                points.RemoveAt(0);
                Destroy(g.gameObject);
            }
        }
        cubeGazeObjects = new Dictionary<float, List<GazePoint>>();
        
        foreach(List<GazePoint> points in rectGazeObjects.Values) {
            while(points.Count > 0) {
                GazePoint g = points[0];
                points.RemoveAt(0);
                Destroy(g.gameObject);
            }
        }
        rectGazeObjects = new Dictionary<float, List<GazePoint>>();

        foreach(List<GazePoint> points in sphereGazeObjects.Values) {
            while(points.Count > 0) {
                GazePoint g = points[0];
                points.RemoveAt(0);
                Destroy(g.gameObject);
            }
        }
        sphereGazeObjects = new Dictionary<float,List<GazePoint>>();
    }

    public void ToggleCubeGaze() {
        m_showCubeGaze = !m_showCubeGaze;
        foreach(KeyValuePair<float,List<GazePoint>> kvp in cubeGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showCubeGaze && discretizationToggles[kvp.Key]);
            }
        }
    }
    public void ToggleRectGaze() {
        m_showRectGaze = !m_showRectGaze;
        foreach(KeyValuePair<float, List<GazePoint>> kvp in rectGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showRectGaze && discretizationToggles[kvp.Key]);
            }
        }
    }
    public void ToggleSphereGaze() {
        m_showSphereGaze = !m_showSphereGaze;
        foreach(KeyValuePair<float, List<GazePoint>> kvp in sphereGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showSphereGaze && discretizationToggles[kvp.Key]);
            } 
        }
    }

    public void ToggleDiscretization(float z) {
        discretizationToggles[z] = !discretizationToggles[z];
        foreach(KeyValuePair<float, List<GazePoint>> kvp in cubeGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showCubeGaze && discretizationToggles[kvp.Key]);
            }
        }
        foreach(KeyValuePair<float, List<GazePoint>> kvp in rectGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showRectGaze && discretizationToggles[kvp.Key]);
            }
        }
        foreach(KeyValuePair<float, List<GazePoint>> kvp in sphereGazeObjects) {
            foreach(GazePoint p in kvp.Value) {
                p.gameObject.SetActive(m_showSphereGaze && discretizationToggles[kvp.Key]);
            } 
        }
    }

    public int NumDiscretizations() {
        return new List<float>(discretizationToggles.Keys).Count;
    }
    public float GetDiscretizationFromIndex(int i) {
        return new List<float>(discretizationToggles.Keys)[i];
    }


    public void ReplayGazeHits(LoadedSimulationDataPerTrial trial) {
        if (trial.gazeData.gazeDataByTimestamp.Count == 0) {
            Debug.Log("[RAYCASTER] ERROR: Cannot place gaze hits when there aren't any...");
            return;
        }
        // If we had a replay existing, we should clear it.
        ResetReplay();
        // Resut us too
        ResetGazeReplay();
        // The dreaded directional aspect...


        // it's fine to have gaze data. But we need to instantiate the cars and agents that were there at the time too.
        // Fortunately, we have access to that data via `trial.positionData`, which is of type `LoadedPositionData`
        // `LoadedPositionData` also has a `positionDataByTimestamp` dictionary. The Key is the timestamp (float). The value is another Dictionary. Key = ExpeirmentID, Value = StreetSimTrackable
        // Note that StreetSimTrackable has localPosition and localrotation too.
        float timestamp;
        Vector3 agentOriginalPosition, hitOriginalPosition;
        Quaternion agentOriginalRotation, hitOriginalRotation;
        foreach(KeyValuePair<float, List<RaycastHitRow>> kvp in trial.gazeData.gazeDataByTimestamp) {
            // Key = timestamp. Value = List of hits
            timestamp = kvp.Key;
            foreach(RaycastHitRow row in kvp.Value) {
                // Note that we have a quandry.
                // `row` contains a hitID and agentID. There are times whne they're the same and others when they're different.
                // Furthermore, while we know that both a `hitID` and `agentID` do exist somewhere in the word, they may be static or dynamic objects.
                // `positionData` only contains dynamic objects - positionData stores the IDs that it tracked inside of `idsTracked`.
                // If the hitID and/or agentID are not inside `idsTracked`, then it's probably just a static object in the world. Easy-peasy.
                // Otherwise, that means that either hitID or agentID are dynamic objects. This is an OR and not an AND because of circumstances:
                //  1. What if hitID is moving but agentID isn't?
                //  2. What if agentID is moving but hitID isn't?
                //  3. What if both are moving?
                //  4. What if neither are moving? Both are moving?
                
                // For each row, we grab the agentId. We've verified that this exists.
                ExperimentID agentID = StreetSimIDController.ID.FindIDFromName(row.agentID);
                ExperimentID hitID = StreetSimIDController.ID.FindIDFromName(row.hitID);
                agentOriginalPosition = agentID.transform.position;
                agentOriginalRotation = agentID.transform.rotation;
                hitOriginalPosition = hitID.transform.position;
                hitOriginalRotation = hitID.transform.rotation;
                // Rmeember, if agentId is not in idsTracked, then it's most likely a static object that isn't tracked at all.
                if (!trial.positionData.idsTracked.Contains(agentID)) {
                    // That means that this is an object is static. In this particular simulation, any static parents are probalby not having any moving children. We can assume that the children are static too.
                    // in this situation, all we really can do is plaster a gaze point local to hitID.
                    GazePoint newGazePoint = Instantiate(StreetSimLoadSim.LS.gazePointPrefab, hitID.transform) as GazePoint;
                    newGazePoint.transform.localPosition = new Vector3(row.localPositionOfHitPosition[0], row.localPositionOfHitPosition[1], row.localPositionOfHitPosition[2]);
                    newGazePoint.transform.parent = null;
                    gazeGazeObjects.Add(newGazePoint);
                } else {
                    // We snagged a dynamic object! That means that at some point, this was tracked. It ought to be!
                    // The next question is if we can position the agent at the position it was at at this timestamp... which is much harder.
                    // The best thing we can do is iterate from current frameIndex back to 0 to look for the latest change in position.
                    for(int i = row.frameIndex; i >= 0; i--) {
                        if (!trial.indexTimeMap.ContainsKey(i)) continue;
                        float tempTime = trial.indexTimeMap[i];
                        if (!trial.positionData.positionDataByTimestamp.ContainsKey(tempTime)) continue;
                        if (!trial.positionData.positionDataByTimestamp[tempTime].ContainsKey(agentID)) continue;
                        // If we reach this point, we can safely place the agentID because a record exists
                        agentID.transform.localPosition = trial.positionData.positionDataByTimestamp[tempTime][agentID].localPosition;
                        agentID.transform.localRotation = trial.positionData.positionDataByTimestamp[tempTime][agentID].localRotation;
                        break;
                    }
                    // Now that the agentID is placed, we need to deal with hitID
                    // If hitID and agentID are the same value, then we don't need to do any additional work.
                    if (hitID != agentID) {
                        // Uh oh, the hitID is not the same as agentID. This means this too is a dynamic object... potentially
                        if (!trial.positionData.idsTracked.Contains(hitID)) {
                            // Uh oh, this hitID is not a dynamic object. But that's fine - thta just means we can attach it regradless
                            GazePoint newGazePoint = Instantiate(StreetSimLoadSim.LS.gazePointPrefab, hitID.transform) as GazePoint;
                            newGazePoint.transform.localPosition = new Vector3(row.localPositionOfHitPosition[0], row.localPositionOfHitPosition[1], row.localPositionOfHitPosition[2]);
                            newGazePoint.transform.parent = null;
                            gazeGazeObjects.Add(newGazePoint);
                        } else {
                            // This one is also a dynamic object. Time for traversal again.
                            for(int j = row.frameIndex; j >= 0; j--) {
                                if (!trial.indexTimeMap.ContainsKey(j)) continue;
                                float tempTime2 = trial.indexTimeMap[j];
                                if (!trial.positionData.positionDataByTimestamp.ContainsKey(tempTime2)) continue;
                                if (!trial.positionData.positionDataByTimestamp[tempTime2].ContainsKey(hitID)) continue;
                                // If we reach this point, we can safely place the agentID because a record exists
                                hitID.transform.localPosition = trial.positionData.positionDataByTimestamp[tempTime2][hitID].localPosition;
                                hitID.transform.localRotation = trial.positionData.positionDataByTimestamp[tempTime2][hitID].localRotation;
                            }
                            GazePoint newGazePoint = Instantiate(StreetSimLoadSim.LS.gazePointPrefab, hitID.transform) as GazePoint;
                            newGazePoint.transform.localPosition = new Vector3(row.localPositionOfHitPosition[0], row.localPositionOfHitPosition[1], row.localPositionOfHitPosition[2]);
                            newGazePoint.transform.parent = null;
                            gazeGazeObjects.Add(newGazePoint);
                        }
                    } else {
                        // In this scenario, the hitID is the same as the agentID. So we don't need to worry!
                        GazePoint newGazePoint = Instantiate(StreetSimLoadSim.LS.gazePointPrefab, hitID.transform) as GazePoint;
                        newGazePoint.transform.localPosition = new Vector3(row.localPositionOfHitPosition[0], row.localPositionOfHitPosition[1], row.localPositionOfHitPosition[2]);
                        newGazePoint.transform.parent = null;
                        gazeGazeObjects.Add(newGazePoint);
                    }
                }
                agentID.transform.position = agentOriginalPosition;
                agentID.transform.rotation = agentOriginalRotation;
                hitID.transform.position = hitOriginalPosition;
                hitID.transform.rotation = hitOriginalRotation;
            }
        }
    }
    public void ResetGazeReplay() {
        while(gazeGazeObjects.Count > 0) {
            GazePoint point = gazeGazeObjects[0];
            gazeGazeObjects.RemoveAt(0);
            Destroy(point.gameObject);
        }
        gazeGazeObjects = new List<GazePoint>();
    }
}