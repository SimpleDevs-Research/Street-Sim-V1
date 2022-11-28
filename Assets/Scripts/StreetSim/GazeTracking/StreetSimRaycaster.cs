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
    }

    private void Update() {
        if (debugIndependently) CheckRaycast();
    }

    // This only runs when `StreetSim` calls it
    public void CheckRaycast() {
        if (pointer == null) return;
        // Get pointer data
        ExperimentID target, closestTarget;
        RaycastHit[] hits = Physics.SphereCastAll(pointer.transform.position, 0.1f, pointer.transform.forward, 100f, layerMask);
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

        GetPixels();
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
                p.gameObject.SetActive(m_showRectGaze && discretizationToggles[kvp.Key]);
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

    public void GetPixels() {
        StreetSimLoadSim.LS.cam360.GetComponent<BodhiDonselaar.EquiCam>().ConvertWorldToScreen();
    }
}