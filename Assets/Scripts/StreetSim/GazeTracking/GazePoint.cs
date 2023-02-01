using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;
using SerializableTypes;

[System.Serializable]
public class GazePointScreenPoint {
    public float z;
    public Vector3 screenPoint;
    public Transform map;
    public GazePointScreenPoint(float z, Vector3 screenPoint, Transform map) {
        this.z = z;
        this.screenPoint = screenPoint;
        this.map = map;
    }
}
public class GazePoint : MonoBehaviour
{
    private Renderer renderer;
    public Transform camera;
    public Vector3 originPoint;
    public bool autoScale = true;
    public Vector3 localPosition;
    public ExperimentID parent;
    //public List<GazePointScreenPoint> screenPointsByDiscretization = new List<GazePointScreenPoint>();

    private void Awake() {
        renderer = GetComponent<Renderer>();
    }
    private void Update() {
        localPosition = transform.localPosition;
        if (autoScale) transform.localScale = Vector3.one * (0.025f * (transform.position - StreetSimLoadSim.LS.cam360.position).magnitude);
    }
    public void SetColor(Color color) {
        renderer.material.SetColor("_Color",color);
    }
    public void SetScale(bool autoScale = true) {
        this.autoScale = autoScale;
    }
    public void SetScale(float radius) {
        this.autoScale = false;
        transform.localScale = new Vector3(radius,radius,radius);
    }
    /*
    public void CalculateScreenPoint(float z, Camera cam, Transform map) {
        Vector3 screenPos = cam.WorldToViewportPoint(transform.position);
        screenPointsByDiscretization.Add(new GazePointScreenPoint(z,screenPos,map));
        //Debug.Log("target is " + screenPos.x + " pixels from the left");
    }
    */
    /*    
    public Vector3 ConvertWorldToScreen (Vector3 positionIn) {
        RectTransform rectTrans = this.GetComponentInParent<RectTransform>(); //RenderTextHolder
        Vector2 viewPos = otherCam.WorldToViewportPoint (positionIn);
        Vector2 localPos = new Vector2 (viewPos.x * rectTrans.sizeDelta.x, viewPos.y * rectTrans.sizeDelta.y);
        Vector3 worldPos = rectTrans.TransformPoint (localPos);
        float scalerRatio = (1 / this.transform.lossyScale.x) * 2; //Implying all x y z are the same for the lossy scale
        return new Vector3 (worldPos.x - rectTrans.sizeDelta.x / scalerRatio, worldPos.y - rectTrans.sizeDelta.y / scalerRatio, 1f);
    }
    */
}

[System.Serializable]
public class SGazePoint {

    public int frameIndex;
    public float timestamp;

    public float xDiscretization;
    public float zDiscretization;

    public SVector3 gazeOrigin;
    public float gazeOrigin_x;
    public float gazeOrigin_y;
    public float gazeOrigin_z;
    public SVector3 gazeDir;
    public float gazeDir_x;
    public float gazeDir_y;
    public float gazeDir_z;
    public SVector3 fixationOrigin;
    public float fixationOrigin_x;
    public float fixationOrigin_y;
    public float fixationOrigin_z;
    public SVector3 fixationDir;
    public float fixationDir_x;
    public float fixationDir_y;
    public float fixationDir_z;

    public SGazePoint(int frameIndex, float timestamp, Vector3 gazeOrigin, Vector3 gazeDir, Vector3 fixationOrigin, Vector3 fixationDir) {
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.gazeOrigin = gazeOrigin;
        this.gazeOrigin_x = this.gazeOrigin.x;
        this.gazeOrigin_y = this.gazeOrigin.y;
        this.gazeOrigin_z = this.gazeOrigin.z;
        this.gazeDir = gazeDir;
        this.gazeDir_x = this.gazeDir.x;
        this.gazeDir_y = this.gazeDir.y;
        this.gazeDir_z = this.gazeDir.z;
        this.fixationOrigin = fixationOrigin;
        this.fixationOrigin_x = this.fixationOrigin.x;
        this.fixationOrigin_y = this.fixationOrigin.y;
        this.fixationOrigin_z = this.fixationOrigin.z;
        this.fixationDir = fixationDir;
        this.fixationDir_x = this.fixationDir.x;
        this.fixationDir_y = this.fixationDir.y;
        this.fixationDir_z = this.fixationDir.z;
        this.xDiscretization = 0f;
        this.zDiscretization = 0f;
    }
    public SGazePoint(int frameIndex, float timestamp, Vector3 gazeOrigin, Vector3 gazeDir, Vector3 fixationOrigin, Vector3 fixationDir, Vector2 xzDiscretization) {
        this.frameIndex = frameIndex;
        this.timestamp = timestamp;
        this.gazeOrigin = gazeOrigin;
        this.gazeOrigin_x = this.gazeOrigin.x;
        this.gazeOrigin_y = this.gazeOrigin.y;
        this.gazeOrigin_z = this.gazeOrigin.z;
        this.gazeDir = gazeDir;
        this.gazeDir_x = this.gazeDir.x;
        this.gazeDir_y = this.gazeDir.y;
        this.gazeDir_z = this.gazeDir.z;
        this.fixationOrigin = fixationOrigin;
        this.fixationOrigin_x = this.fixationOrigin.x;
        this.fixationOrigin_y = this.fixationOrigin.y;
        this.fixationOrigin_z = this.fixationOrigin.z;
        this.fixationDir = fixationDir;
        this.fixationDir_x = this.fixationDir.x;
        this.fixationDir_y = this.fixationDir.y;
        this.fixationDir_z = this.fixationDir.z;
        this.xDiscretization = xzDiscretization.x;
        this.zDiscretization = xzDiscretization.y;
    }
    public SGazePoint(string[] data) {
        this.frameIndex = int.Parse(data[0]);
        this.timestamp = float.Parse(data[1]);
        this.xDiscretization = float.Parse(data[2]);
        this.zDiscretization = float.Parse(data[3]);
        this.gazeOrigin_x = float.Parse(data[4]);
        this.gazeOrigin_y = float.Parse(data[5]);
        this.gazeOrigin_z = float.Parse(data[6]);
        this.gazeOrigin = new SVector3(this.gazeOrigin_x, this.gazeOrigin_y, this.gazeOrigin_z);
        this.gazeDir_x = float.Parse(data[7]);
        this.gazeDir_y = float.Parse(data[8]);
        this.gazeDir_z = float.Parse(data[9]);
        this.gazeDir = new SVector3(this.gazeDir_x, this.gazeDir_y, this.gazeDir_z);
        this.fixationOrigin_x = float.Parse(data[10]);
        this.fixationOrigin_y = float.Parse(data[11]);
        this.fixationOrigin_z = float.Parse(data[12]);
        this.fixationOrigin = new SVector3(this.fixationOrigin_x, this.fixationOrigin_y, this.fixationOrigin_z);
        this.fixationDir_x = float.Parse(data[13]);
        this.fixationDir_y = float.Parse(data[14]);
        this.fixationDir_z = float.Parse(data[15]);
        this.fixationDir = new SVector3(this.fixationDir_x, this.fixationDir_y, this.fixationDir_z);
    }

    public Vector3 GetWorldPosition(float sphereRadius) {
        return (Vector3)fixationOrigin + ((Vector3)fixationDir).normalized*sphereRadius;
    }
    public static List<string> Headers => new List<string> {
        "frameIndex",
        "timestamp",
        "xDiscretization",
        "zDiscretization",
        "gazeOrigin_x",
        "gazeOrigin_y",
        "gazeOrigin_z",
        "gazeDir_x",
        "gazeDir_y",
        "gazeDir_z",
        "fixationOrigin_x",
        "fixationOrigin_y",
        "fixationOrigin_z",
        "fixationDir_x",
        "fixationDir_y",
        "fixationDir_z"
    };
}