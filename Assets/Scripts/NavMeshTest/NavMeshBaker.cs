using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBaker : MonoBehaviour
{

    public NavMeshSurface[] surfaces;
    public Transform[] objects;

    // Update is called once per frame
    private void Update()
    {
        for (int i = 0; i < objects.Length; i++) {

        }
        for (int s = 0; s < surfaces.Length; s++) {
            surfaces[s].BuildNavMesh();
        }
    }
}
