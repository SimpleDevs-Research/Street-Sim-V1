using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
    public Dictionary<string, NPCPath> pathDict = new Dictionary<string,NPCPath>();

    #if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        DrawTargets();
        DrawPaths();
    }
    public void DrawTargets() {
        // For each child transform within this...
        foreach(Transform child in transform) {
            Handles.color = Color.green;
            Handles.DrawWireDisc(child.position , child.up, 0.1f);
        }
    }
    public void DrawPaths() {
        foreach(NPCPath path in templatePaths) {
            if (path.points.Length < 2) continue;
            TargetPositioningVisualizer.DrawPath(path);
        }
    }
    public static void DrawPath(NPCPath path) {
        for(int i = 0; i <= path.points.Length-2; i++) {
            Handles.color = path.pathColor;
            Handles.DrawLine(path.points[i].position,path.points[i+1].position,path.pathWidth);
        }
    }
    #endif

    void Awake() {
        current = this;
        foreach(NPCPath template in templatePaths) {
            pathDict.Add(template.name, template);
        }
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

    public bool GetPathFromName(string name, out NPCPath path) {
        if (pathDict.ContainsKey(name)) {
            path = pathDict[name];
            return true;
        }
        path = default(NPCPath);
        return false;
    }
}
