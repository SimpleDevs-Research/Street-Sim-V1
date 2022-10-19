using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class GazeDetection : MonoBehaviour
{
    public enum DetectionType {
        Raycast,
        SphereCast,
        SphereCastAll
    }
    public enum DistanceCalculationType {
        ScreenPoint,
        VectorCross
    }
    public DetectionType detectionType = DetectionType.SphereCastAll;
    public Camera cam;
    private Renderer m_currentGazeTarget = null;
    public Renderer currentGazeTarget {
        get { return m_currentGazeTarget; }
        set {
            if (m_currentGazeTarget != null && m_currentGazeTarget != value) {
                m_currentGazeTarget.materials[0].SetColor("_Color",Color.yellow);
            }
            m_currentGazeTarget = value;
            if (m_currentGazeTarget != null) {
                m_currentGazeTarget.materials[0].SetColor("_Color",Color.blue);
            }
        }
    }
    public LayerMask layerMask;
    public DistanceCalculationType calculationType = DistanceCalculationType.VectorCross;
    private Vector2 cameraPixelCenter;


    private void Awake() {
        cameraPixelCenter = new Vector2(cam.pixelWidth,cam.pixelHeight) * 0.5f;
    }

    // Update is called once per frame
    void Update() {
        RaycastHit hit;
        switch(detectionType) {
            case DetectionType.Raycast:
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 20f, layerMask)) {
                    Renderer renderer;
                    if (HelperMethods.HasComponent<Renderer>(hit.transform.gameObject, out renderer)) {
                        currentGazeTarget = renderer;
                    }
                }
                break;
            case DetectionType.SphereCast:
                if (Physics.SphereCast(cam.transform.position, 0.5f, cam.transform.forward, out hit, 20f, layerMask)) {
                    Renderer renderer;
                    if (HelperMethods.HasComponent<Renderer>(hit.transform.gameObject, out renderer)) {
                        currentGazeTarget = renderer;
                    }
                }
                break;
            case DetectionType.SphereCastAll:
                RaycastHit[] potentials = Physics.SphereCastAll(cam.transform.position, 0.5f, cam.transform.forward, 20f, layerMask);
                if (potentials.Length > 0) {
                    CalculateClosestTarget(potentials, cam.transform.position, cam.transform.forward);
                }
                break;
        }
    }

    private void CalculateClosestTarget(RaycastHit[] potentials, Vector3 pos, Vector3 dir) {
        Renderer closest = null, potentialClosest;
        float closestDistance = Mathf.Infinity, currentDistance = 0f;
        foreach(RaycastHit hit in potentials) {
            if (HelperMethods.HasComponent<Renderer>(hit.transform.gameObject,out potentialClosest)) {
                switch(calculationType) {
                    case DistanceCalculationType.ScreenPoint:
                        Vector3 screenPos = cam.WorldToScreenPoint(hit.point);
                        currentDistance = (new Vector2(screenPos.x,screenPos.y) - cameraPixelCenter).magnitude;
                        break;
                    case DistanceCalculationType.VectorCross:
                        currentDistance = (Vector3.Cross(dir, hit.point - pos)).magnitude;
                        break;
                }
                if (closest == null || currentDistance < closestDistance) {
                    closest = potentialClosest;
                    closestDistance = currentDistance;
                }
            }
        }
        currentGazeTarget = closest;
    }
}
