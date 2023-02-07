using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRangedView : MonoBehaviour
{

    [System.Serializable]
    public class Turret {
        [SerializeField]
        private Vector3 _localPosition;

        [SerializeField]
        private float _angleMin = -90f;

        [SerializeField]
        private float _angleMax = 90f;

        public float Range = 10f;
        public float AngleMin(Transform t) {
            return _angleMin + t.eulerAngles.y;
        }
        public float AngleMax(Transform t) {
            return _angleMax + t.eulerAngles.y;
        }
        public Vector3 Position(Transform t) {
            return t.position + _localPosition;
        }
        public Vector3 Forward(Transform t) {
            var position = Position(t);
            float aMin = AngleMin(t);
            float aMax = AngleMax(t);
 
            Vector3 vMin = new Vector3(Mathf.Sin(aMin * Mathf.Deg2Rad), 0, Mathf.Cos(aMin * Mathf.Deg2Rad));
            Vector3 vMax = new Vector3(Mathf.Sin(aMax * Mathf.Deg2Rad), 0, Mathf.Cos(aMax * Mathf.Deg2Rad));
 
            return ((vMin + vMax) * 0.5f).normalized;
        }
 
        public Plane MinPlane(Transform t) {
            var position = Position(t);
 
            float aMin = AngleMin(t);
            Vector3 vMin = new Vector3(Mathf.Sin(aMin * Mathf.Deg2Rad), 0, Mathf.Cos(aMin * Mathf.Deg2Rad));
 
            Vector3 normal =  Vector3.Cross(vMin, Vector3.down);
 
            return new Plane(normal, position);
        }
 
        public Plane MaxPlane(Transform t) {
            var position = Position(t);
 
            float aMax = AngleMax(t);
            Vector3 vMax = new Vector3(Mathf.Sin(aMax * Mathf.Deg2Rad), 0, Mathf.Cos(aMax * Mathf.Deg2Rad));
 
            Vector3 normal =  Vector3.Cross(vMax, Vector3.up);
 
            return new Plane(normal, position);
        }
 
        public bool IsInRange(Transform t, Vector3 point) {
            var position = Position(t);
            float dist = Vector3.Distance(position, point);
            if(dist <= Range) {
                Plane minP = MinPlane(t);
                Plane maxP = MaxPlane(t);
                Vector3 direction = point - position;
 
                if(Vector3.Dot(minP.normal, direction) > 0 || Vector3.Dot(maxP.normal, direction) > 0) {
                    return true;
                }
            }
            return false;
        }
    }

    public Turret[] turrets;
    public Transform test;

    public void OnDrawGizmos() {
        const int segments = 5;
        List<Vector2> arcPoints = new List<Vector2>();
        float angle;
        float arcLength;
        Vector3 position;
        foreach (Turret t in turrets) {
            position = t.Position(transform);
            Gizmos.color = Color.green;
            Debug.Log("My Forward is: " + t.Forward(transform));
            Gizmos.DrawRay(position, t.Forward(transform) * t.Range);
            angle = t.AngleMin(transform);
            arcLength = t.AngleMax(transform) - angle;
            for (int i = 0; i <= segments; i++) {
                float x = Mathf.Sin(Mathf.Deg2Rad * angle) * t.Range;
                float y = Mathf.Cos(Mathf.Deg2Rad * angle) * t.Range;
                arcPoints.Add(new Vector2(x, y));
                angle += (arcLength / segments);
            }
            //TODO Draw the arc
            Gizmos.color = Color.red;
            Vector3 arcPos = new Vector3(position.x + arcPoints[0].x, position.y, position.z + arcPoints[0].y);
            Vector3 prevPos = arcPos;
            Gizmos.DrawLine(position, arcPos);
            //TODO For
            for(int i = 0; i < arcPoints.Count; ++i) {
                arcPos = new Vector3(position.x + arcPoints[i].x, position.y, position.z + arcPoints[i].y);
                Gizmos.DrawLine(prevPos, arcPos);
                prevPos = arcPos;
            }
            arcPos = new Vector3(position.x + arcPoints[arcPoints.Count - 1].x, position.y, position.z + arcPoints[arcPoints.Count - 1].y);
            Gizmos.DrawLine(position, arcPos);
        }
    }
 
    // Update is called once per frame
    void Update() {
        foreach (Turret t in turrets) {
            if(t.IsInRange(transform, test.position)) {
                Debug.DrawLine(t.Position(transform), test.position, Color.cyan);
            }
        }
    }
}
