using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

[System.Serializable]
public class ProximityData {
    public string id;
    public int frameIndex;
    public float timestamp;
    public float distance;
    public float direction_x, direction_y, direction_z;
    public ProximityData(string id, int frameIndex, float timestamp, float minDistance, Vector3 directionTo) {
        this.id = id;
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.distance = minDistance;
        this.direction_x = directionTo.x;
        this.direction_y = directionTo.y;
        this.direction_z = directionTo.z;
    }
    public static List<string> Headers => new List<string> {
        "id",
        "frameIndex",
        "timestamp",
        "distance",
        "direction_x",
        "direction_y",
        "direction_z"

    };
}

public class StreetSimProximityTracker : MonoBehaviour
{
    public static StreetSimProximityTracker PT;

    [SerializeField] private Transform toTrack = null;
    public float radius = 2f;
    private IEnumerator ProximityCoroutine = null;
    [SerializeField] private LayerMask layerMask;

    [SerializeField] private List<List<ProximityData>> m_proximityData = new List<List<ProximityData>>();
    public List<List<ProximityData>> proximityData { get=>m_proximityData; set{} }

    private void Awake() {
        PT = this;
    }

    public void CheckProximity() {
        if (toTrack == null) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, layerMask);
        if (hitColliders.Length > 0) StartCoroutine(ProximityCalculation(hitColliders));
    }

    private IEnumerator ProximityCalculation(Collider[] colliders) {
        Collider col = null;
        Queue<Collider> toCheck = new Queue<Collider>(colliders);
        float timestamp = StreetSim.S.trialFrameTimestamp;
        int frameIndex = StreetSim.S.trialFrameIndex;
        Dictionary<ExperimentID,Vector3> minDistances = new Dictionary<ExperimentID,Vector3>();
        Vector3 directionTo = Vector3.zero;
        int count = 0;
        ExperimentID otherID = null;
        while(toTrack != null && toCheck.Count > 0) {
            count++;
            col = toCheck.Dequeue();
            if (HelperMethods.HasComponent<ExperimentID>(col.gameObject, out otherID)) {
                directionTo = otherID.transform.position - toTrack.position;  // distance is the magnitude of this value
                if (!minDistances.ContainsKey(otherID)) minDistances.Add(otherID,directionTo);
                else if (directionTo.magnitude < minDistances[otherID].magnitude) minDistances[otherID] = directionTo;
            }
            if (count >= 20) {
                yield return null;
                count = 0;
            }
        }
        count = 0;
        List<ProximityData> currentData = new List<ProximityData>();
        foreach(KeyValuePair<ExperimentID,Vector3> kvp in minDistances) {
            count++;
            currentData.Add(new ProximityData(
                kvp.Key.id,
                frameIndex,
                timestamp,
                kvp.Value.magnitude,
                kvp.Value.normalized
            ));
            if (count >= 20) {
                yield return null;
                count = 0;
            }
        }
        m_proximityData.Add(currentData);
    }

    public void ClearData() {
        m_proximityData = new List<List<ProximityData>>();
    }
}
