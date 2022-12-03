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
    //public List<GazePointScreenPoint> screenPointsByDiscretization = new List<GazePointScreenPoint>();

    private void Awake() {
        renderer = GetComponent<Renderer>();
    }
    private void Update() {
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
    public SVector3 originPoint;
    public SVector3 worldPosition;
    public SVector3 postProcessWorldPosition;
    public SVector3 dir;
    public SGazePoint(Vector3 originPoint) {
        this.originPoint = originPoint;
    }
}