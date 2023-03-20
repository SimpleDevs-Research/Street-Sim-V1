using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class DisplayNavMeshPath : MonoBehaviour
{

    private UnityEngine.AI.NavMeshAgent nma;
    private LineRenderer lr; 

    private void Awake() {
        nma = GetComponent<UnityEngine.AI.NavMeshAgent>();
        lr = GetComponent<LineRenderer>();    
    }

    // Update is called once per frame
    void Update() {
        Vector3[] corners = nma.path.corners;
        lr.positionCount = corners.Length;
        for(int i = 0; i < corners.Length; i++) {
            Vector3 pos = new Vector3(corners[i].x, corners[i].y, corners[i].z);
            lr.SetPosition(i,pos);
        }
    }
}
