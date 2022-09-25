using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerNavMesh : MonoBehaviour
{
    [SerializeField] private Transform moveTransformPosition;

    private NavMeshAgent myNavAgent;

    // Start is called before the first frame update
    void Start()
    {
        myNavAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (moveTransformPosition != null) myNavAgent.destination = moveTransformPosition.position;
    }
}
