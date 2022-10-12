using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class TargetPositioningVisualizerTarget : MonoBehaviour
{
    #if UNITY_EDITOR
    void OnDrawGizmosSelected() {
        TargetPositioningVisualizer parent;
        if (HelperMethods.HasComponent<TargetPositioningVisualizer>(transform.parent.gameObject,out parent)) 
            parent.DrawTargets();
    }
    #endif
}
