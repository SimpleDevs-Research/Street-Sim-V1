using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NPCPath {
    public string name;
    public Transform[] points;
    public Color pathColor;
    public float pathWidth = 5f;
}

[ExecuteInEditMode]
public class TargetPositioningVisualizer : MonoBehaviour
{
    public static TargetPositioningVisualizer current;
    public List<Transform> targets = new List<Transform>();
    public List<NPCPath> templatePaths = new List<NPCPath>();

    void OnDrawGizmosSelected() {
        DrawTargets();
        DrawPaths();
    }
    public void DrawTargets() {
        // For each child transform within this...
        foreach(Transform child in transform) {
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(child.position , child.up, 0.5f);
        }
    }
    public void DrawPaths() {
        foreach(NPCPath path in templatePaths) {
            if (path.points.Length < 2) continue;
            for(int i = 0; i <= path.points.Length-2; i++) {
                UnityEditor.Handles.color = path.pathColor;
                UnityEditor.Handles.DrawLine(path.points[i].position,path.points[i+1].position,path.pathWidth);
            }
        }
    }

    void Awake() {
        current = this;
    }

    void OnGUI() {
        List<Transform> newTargets = new List<Transform>();
        foreach(Transform child in transform) {
            newTargets.Add(child);
            if (child.gameObject.GetComponent<TargetPositioningVisualizerTarget>() == null) {
                child.gameObject.AddComponent<TargetPositioningVisualizerTarget>();
            }

        }
        targets = newTargets;
    }
}
