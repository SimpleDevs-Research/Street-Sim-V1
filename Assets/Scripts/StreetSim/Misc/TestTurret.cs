using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTurret : MonoBehaviour
{
    [SerializeField]
    private bool debug = false;

    [SerializeField]
    private Vector2 angles = new Vector2(-90f,90f);
    private Vector2 originalAngles;
    private Vector2 adjustedAngles;

    [SerializeField]
    private float range = 10f;

    [SerializeField]
    private List<Transform> inRange = new List<Transform>();

    public List<Transform> testObjects = new List<Transform>();

    private void Awake() {
        originalAngles = angles;
    }

    public Plane MinPlane() {
        Vector3 position = transform.position;
        float aMin = adjustedAngles.x;

        Vector3 vMin = new Vector3(Mathf.Sin(aMin * Mathf.Deg2Rad), 0, Mathf.Cos(aMin * Mathf.Deg2Rad)); 
        Vector3 normal =  Vector3.Cross(vMin, Vector3.down);
 
        return new Plane(normal, position);
    }
 
    public Plane MaxPlane() {
        Vector3 position = transform.position;
        float aMax = adjustedAngles.y;
        
        Vector3 vMax = new Vector3(Mathf.Sin(aMax * Mathf.Deg2Rad), 0, Mathf.Cos(aMax * Mathf.Deg2Rad));
        Vector3 normal =  Vector3.Cross(vMax, Vector3.up);
 
        return new Plane(normal, position);
    }

    public void OnDrawGizmos() {
        const int segments = 5;
        List<Vector2> arcPoints = new List<Vector2>();
        
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;

        Gizmos.color = Color.green;
        // Debug.Log("My Forward is: " + t.Forward(transform));
        Gizmos.DrawRay(position, forward * range);

        float angle = adjustedAngles.x;
        float arcLength = adjustedAngles.y - angle;
        for (int i = 0; i <= segments; i++) {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * range;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * range;
            arcPoints.Add(new Vector2(x, y));
            angle += (arcLength / segments);
        }
        
        //TODO Draw the arc
        Gizmos.color = Color.red;
        Vector3 arcPos = new Vector3(position.x + arcPoints[0].x, position.y, position.z + arcPoints[0].y);
        Vector3 prevPos = arcPos;
        Gizmos.DrawLine(position, arcPos);
        for(int i = 0; i < arcPoints.Count; ++i) {
            arcPos = new Vector3(position.x + arcPoints[i].x, position.y, position.z + arcPoints[i].y);
            Gizmos.DrawLine(prevPos, arcPos);
            prevPos = arcPos;
        }
        arcPos = new Vector3(position.x + arcPoints[arcPoints.Count - 1].x, position.y, position.z + arcPoints[arcPoints.Count - 1].y);
        Gizmos.DrawLine(position, arcPos);
    }

    public bool IsInRange(Vector3 point) {
        Vector3 position = transform.position;
        float dist = Vector3.Distance(position, point);
        if(dist <= range) {
            Plane minP = MinPlane();
            Plane maxP = MaxPlane();
            Vector3 direction = point - position;
 
            if(Vector3.Dot(minP.normal, direction) > 0 && Vector3.Dot(maxP.normal, direction) > 0) {
                return true;
            }
        }
        return false;
    }

    public Vector2 AdjustAngle(Vector3 forward) {
        float angle = (transform.eulerAngles.y > 180f) 
            ? Vector3.Angle(-Vector3.forward, forward) + 180f
            : Vector3.Angle(Vector3.forward, forward);
        return new Vector2(angles.x + angle, angles.y + angle);
    }

    // Update is called once per frame
    void Update() {
        inRange = new List<Transform>();
        if (debug) Debug.Log(transform.eulerAngles.y);
        adjustedAngles = AdjustAngle(transform.forward);
        foreach(Transform test in testObjects) {
            if(IsInRange(test.position)) {
                Debug.DrawLine(transform.position, test.position, Color.cyan);
                inRange.Add(test);
            }
        }
    }

    public void SetObjects(List<Transform> newObjects) {
        testObjects = newObjects;
    }
    public bool AnyInRange() {
        return inRange.Count > 0;
    }
    public void SetAngle(float newAngle) {
        angles = new Vector2(-newAngle, newAngle);
    }
    public void ResetAngles() {
        angles = originalAngles;
    }
}
