using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeCluster : MonoBehaviour
{
    /*
    [SerializeField] private float distanceThreshold = 0.01f;
    [SerializeField] private List<Vector3> points = new List<Vector3>();

    private void Initialize(float distanceThreshold, Vector3 initialPoint) {
        this.distanceThreshold = distanceThreshold;
        transform.localScale = new Vector3(distanceThreshold, distanceThreshold, distanceThreshold);
        points.Add(initialPoint);
        CalculateCenter();
    }

    private void CalculateCenter() {
        Vector3 pseudoCenter = Vector3.zero;
        foreach(Vector3 point in points) {
            pseudoCenter += point;
        }
        transform.position = pseudoCenter / points.Count;

    }

    public bool CheckAndAddPoint(Vector3 point, out float distance) {
        distance = Vector3.Distance(transform.position, point);
        return distance <= distanceThreshold;
    }

    public bool AddPoint(Vector3 point) {
        points.Add(point);
        CalculateCenter();
    }
    */
}
