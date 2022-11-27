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
    private List<GameObject> gazeObjects = new List<GameObject>();
    [SerializeField] private GameObject gazePrefab;

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


        StreetSim.S.GazeBox.position = StreetSim.S.Cam360.position;
        if (trial.trialData.direction == "NorthToSouth") StreetSim.S.GazeBox.rotation = Quaternion.AngleAxis(180, Vector3.up);
        //StreetSim.S.GazeBox.rotation = StreetSim.S.Cam360.rotation;
        StreetSim.S.GazeBox.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in StreetSim.S.GazeBox) {
            child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        }
        StreetSim.S.replayCamera.gameObject.SetActive(true);
        float directionScale = 1f;
        /*
        if (trial.trialData.direction == "NorthToSouth") {
            StreetSim.S.replayCamera.localScale = new Vector3(-1f,1f,-1f);
            directionScale = -1f;
        }
        */
 
        List<int> order = new List<int>(trial.positionData.indexTimeMap.Keys);
        order.Sort((a,b) => a.CompareTo(b));
        int count = order.Count;
        int index = -1;
        float prevTimestamp = 0f;

        List<RaycastHitRow> gazes = new List<RaycastHitRow>();
        gazeObjects = new List<GameObject>();

        ExperimentID userID = StreetSimIDController.ID.FindIDFromName("User");
        if (userID == null) {
            Debug.Log("[RAYCASTER] ERROR: Cannot find user's ExperimentID");
            yield break;
        }

        /*
        Vector3 positionScale = (trial.trialData.direction == "NorthToSouth")
            ? new Vector3(-1f,1f,-1f)
            : Vector3.one;
        */

        while(index < count-1) {
            index++;
            int frameIndex = order[index];
            float timestamp = trial.positionData.indexTimeMap[frameIndex];
            //StreetSim.S.replayCamera.localPosition = Vector3.Scale(trial.positionData.positionDataByTimestamp[timestamp][userID].localPosition, positionScale);
            StreetSim.S.replayCamera.localPosition = trial.positionData.positionDataByTimestamp[timestamp][userID].localPosition;
            StreetSim.S.replayCamera.localRotation = trial.positionData.positionDataByTimestamp[timestamp][userID].localRotation;
            RaycastHitRow row;
                if (CheckRaycastManual(StreetSim.S.replayCamera.position, StreetSim.S.replayCamera.forward*directionScale, StreetSim.S.replayGazeMask, frameIndex, timestamp, out row)) {
                    gazes.Add(row);
                    GameObject newGazeObject = Instantiate(gazePrefab, StreetSim.S.Cam360, false);
                    newGazeObject.transform.localPosition = new Vector3(row.localPosition[0],row.localPosition[1],row.localPosition[2]);
                    newGazeObject.transform.localRotation = Quaternion.identity;
                    gazeObjects.Add(newGazeObject);
                }
            yield return null;
        }

    }
    public void ResetReplay() {
        StreetSim.S.GazeBox.position = new Vector3(0f, -18.5f, 0f);
        StreetSim.S.GazeBox.rotation = Quaternion.identity;
        StreetSim.S.GazeBox.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        foreach(Transform child in StreetSim.S.GazeBox) {
            child.gameObject.layer = LayerMask.NameToLayer("PostProcessGaze");
        }
        StreetSim.S.replayCamera.position = StreetSim.S.Cam360.position;
        //StreetSim.S.replayCamera.localScale = Vector3.one;
        StreetSim.S.replayCamera.gameObject.SetActive(false);
        while(gazeObjects.Count > 0) {
            GameObject g = gazeObjects[0];
            gazeObjects.RemoveAt(0);
            Destroy(g);
        }
        gazeObjects = new List<GameObject>();
    }
}