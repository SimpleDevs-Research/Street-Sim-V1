using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Helpers;

[System.Serializable]
public class GazeDataStatistics {
    public float startPos_x, startPos_y, startPos_z;
    public Vector3 startPos;
    public float hitPos_x, hitPos_y, hitPos_z;
    public Vector3 hitPos;
    public float rayDir_x,rayDir_y, rayDir_z;
    public Vector3 rayDir;
    public float cross_x, cross_y, cross_z;
    public Vector3 cross;
    public GazeDataStatistics(Vector3 startPos, Vector3 hitPos, Vector3 rayDir, Vector3 cross) {
        this.startPos_x = startPos.x;
        this.startPos_y = startPos.y;
        this.startPos_z = startPos.z;
        this.startPos = startPos;
        this.hitPos_x = hitPos.x;
        this.hitPos_y = hitPos.y;
        this.hitPos_z = hitPos.z;
        this.hitPos = hitPos;
        this.rayDir_x = rayDir.x;
        this.rayDir_y = rayDir.y;
        this.rayDir_z = rayDir.z;
        this.rayDir = rayDir;
        this.cross_x = cross.x;
        this.cross_y = cross.y;
        this.cross_z = cross.z;
        this.cross = cross;
    }
    public GazeDataStatistics(string[] data) {
        this.startPos_x = float.Parse(data[0]);
        this.startPos_y = float.Parse(data[1]);
        this.startPos_z = float.Parse(data[2]);
        this.startPos = new Vector3(this.startPos_x, this.startPos_y, this.startPos_z);

        this.hitPos_x = float.Parse(data[3]);
        this.hitPos_y = float.Parse(data[4]);
        this.hitPos_z = float.Parse(data[5]);
        this.hitPos = new Vector3(this.hitPos_x, this.hitPos_y, this.hitPos_z);

        this.rayDir_x = float.Parse(data[6]);
        this.rayDir_y = float.Parse(data[7]);
        this.rayDir_z = float.Parse(data[8]);
        this.rayDir = new Vector3(this.rayDir_x, this.rayDir_y, this.rayDir_z);

        this.cross_x = float.Parse(data[9]);
        this.cross_y = float.Parse(data[10]);
        this.cross_z = float.Parse(data[11]);
        this.cross = new Vector3(this.cross_x, this.cross_y, this.cross_z);
    }
    public static List<string> Headers = new List<string> {
        "startPos_x", "startPos_y", "startPos_z",
        "hitPos_z", "hitPos_y", "hitPos_z",
        "rayDir_x", "rayDir_y", "rayDir_z",
        "cross_x", "cross_y", "cross_z"
    };
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

    private bool testing = false;
    public string participantName;
    private string fileToSaveTo;
    private List<GazeDataStatistics> aggregateGazeData = new List<GazeDataStatistics>();
    public void SetAggregatedData(List<GazeDataStatistics> newData) {
        aggregateGazeData = newData;
    }
    public TextAsset loadedFile;

    void OnDrawGizmosSelected() {
        Gizmos.color = resultColor;
        if (aggregateGazeData.Count == 0) return;
        foreach(GazeDataStatistics stats in aggregateGazeData) {
            Gizmos.DrawRay(stats.startPos,stats.rayDir * 20f);
            Gizmos.DrawWireSphere(stats.hitPos,0.05f);
            Gizmos.DrawRay(stats.hitPos,stats.cross);
        }
    }

    private string DateTimeString() {
        System.DateTime theTime = System.DateTime.Now;
        string datetime = theTime.ToString("yyyy-MM-dd_HH-mm-ss");
        return datetime;
    }

    private void Awake() {
        Debug.Log("HAHAHAHA" + Camera.VerticalToHorizontalFieldOfView(leftEye.fieldOfView, leftEye.aspect));
        Debug.Log("FOV" + leftEye.fieldOfView);
        Debug.Log("ASPECT" + leftEye.aspect);
        gazePointsForTest = new Queue<Transform>(gazePoints.Shuffle());
        foreach(Transform point in gazePoints) {
            if (!gazeData.ContainsKey(point)) gazeData.Add(point, new List<GazeDataStatistics>());
        }
        fileToSaveTo = Path.Combine(Application.persistentDataPath, participantName + "-"+DateTimeString());
        SaveSystemMethods.SaveCSV<GazeDataStatistics>(fileToSaveTo,GazeDataStatistics.Headers,aggregateGazeData);
    }

    private void Start() {
        foreach(Transform target in gazePoints) {
            target.gameObject.SetActive(false);
        }
        StartCoroutine(StartTestCoroutine());
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
        SaveSystemMethods.SaveCSV<GazeDataStatistics>(fileToSaveTo,GazeDataStatistics.Headers,aggregateGazeData);
        StartCoroutine(TrackGaze());
    }
    public IEnumerator StartTestCoroutine() {
        testing = true;
        StartCoroutine(TrackGaze());
        while(testing) {
            yield return null;
        }
        // Save data
        SaveSystemMethods.SaveCSV<GazeDataStatistics>(fileToSaveTo,GazeDataStatistics.Headers,aggregateGazeData);
        Application.Quit();
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
                        GazeDataStatistics newStat = new GazeDataStatistics(
                            cameraRef.position,
                            hit.point,
                            dir,
                            Vector3.Cross(dir, hit.point - cameraRef.position)
                        );
                        gazeData[gazeTestTarget].Add(newStat);
                        aggregateGazeData.Add(newStat);
                    }
                }
                yield return new WaitForSeconds(0.1f);
            } while(Time.time - startTime < 5f);
            yield return null;
            gazeTestTarget.gameObject.SetActive(false);
        }

        // End test
        testing = false;
    }
}
