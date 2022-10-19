using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

[System.Serializable]
public class GazeDataStatistics {
    public Vector3 startPos;
    public Vector3 hitPos;
    public Vector3 rayDir;
    public Vector3 cross;
    public GazeDataStatistics(Vector3 startPos, Vector3 hitPos, Vector3 rayDir, Vector3 cross) {
        this.startPos = startPos;
        this.hitPos = hitPos;
        this.rayDir = rayDir;
        this.cross = cross;
    }
}

public class GazeTest : MonoBehaviour
{
    public Transform gazePointGroup;
    public List<Transform> gazePoints = new List<Transform>();
    [SerializeField] private Queue<Transform> gazePointsForTest;
    private Dictionary<Transform, List<GazeDataStatistics>> gazeData = new Dictionary<Transform, List<GazeDataStatistics>>();
    public Transform cameraRef;
    public Camera leftEye;

    private bool trackCameraY = true;
    private Transform gazeTestTarget = null;
    public LayerMask targetsToDetect;

    public Color resultColor = Color.blue;

    void OnDrawGizmosSelected() {
        Gizmos.color = resultColor;
        foreach(KeyValuePair<Transform, List<GazeDataStatistics>> kvp in gazeData) {
            if (kvp.Key.gameObject.activeInHierarchy) {
                foreach(GazeDataStatistics stats in kvp.Value) {
                    Gizmos.DrawRay(stats.startPos,stats.rayDir * 20f);
                    Gizmos.DrawWireSphere(stats.hitPos,0.05f);
                    Gizmos.DrawRay(stats.hitPos,stats.cross);
                }
            }
        }
    }

    private void Awake() {
        Debug.Log("HAHAHAHA" + Camera.VerticalToHorizontalFieldOfView(leftEye.fieldOfView, leftEye.aspect));
        Debug.Log("FOV" + leftEye.fieldOfView);
        Debug.Log("ASPECT" + leftEye.aspect);
        gazePointsForTest = new Queue<Transform>(gazePoints.Shuffle());
        foreach(Transform point in gazePoints) {
            if (!gazeData.ContainsKey(point)) gazeData.Add(point, new List<GazeDataStatistics>());
        }
    }

    private void Update() {

        if (trackCameraY) gazePointGroup.position = new Vector3(gazePointGroup.position.x, cameraRef.position.y, gazePointGroup.position.z);
    }

    public void StartTest() {
        Debug.Log("HAHAHAHA" + Camera.VerticalToHorizontalFieldOfView(leftEye.fieldOfView, leftEye.aspect));
        Debug.Log("FOV" + leftEye.fieldOfView);
        Debug.Log("ASPECT" + leftEye.aspect);
        trackCameraY = false;
        foreach(Transform target in gazePoints) {
            target.gameObject.SetActive(false);
        }
        StartCoroutine(TrackGaze());
    }

    private IEnumerator TrackGaze() {
        float startTime;
        RaycastHit hit;
        Vector3 dir;
        while(gazePointsForTest.Count > 0) {
            if (gazeTestTarget != null) gazeTestTarget.gameObject.SetActive(false);
            gazeTestTarget = gazePointsForTest.Dequeue();
            gazeTestTarget.gameObject.SetActive(true);
            startTime = Time.time;
            do {
                dir = cameraRef.forward;
                if(Physics.SphereCast(cameraRef.position,1f,dir,out hit,20f,targetsToDetect)) {
                    // spherecast hit something. Check if it's our target
                    if (hit.transform == gazeTestTarget) {
                        //We should log the vector difference.
                        gazeData[gazeTestTarget].Add(new GazeDataStatistics(
                            cameraRef.position,
                            hit.point,
                            dir,
                            Vector3.Cross(dir, hit.point - cameraRef.position)
                        ));
                    }
                }
                yield return new WaitForSeconds(0.1f);
            } while(Time.time - startTime < 5f);
            yield return null;
            gazeTestTarget.gameObject.SetActive(false);
        }

    }


}
