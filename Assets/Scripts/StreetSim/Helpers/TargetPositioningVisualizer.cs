using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TargetPositioningVisualizer : MonoBehaviour
{
    public static TargetPositioningVisualizer current;
    public List<Transform> targets = new List<Transform>();

    void OnDrawGizmosSelected() {
        DrawTargets();
    }
    public void DrawTargets() {
        // For each child transform within this...
        foreach(Transform child in transform) {
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(child.position , child.up, 0.5f);
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
