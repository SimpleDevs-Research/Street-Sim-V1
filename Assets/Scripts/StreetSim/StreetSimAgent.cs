using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(ThirdPersonCharacter))]
public class StreetSimAgent : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ThirdPersonCharacter character;
    [SerializeField] private Transform[] targetPositions; // note that the 1st position is the starting position
    private int currentTargetIndex = -1;
    [SerializeField] private float currentSpeed = 0f;
    private bool shouldLoop, shouldWarpOnLoop;

    private void Awake() {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (character == null) character = GetComponent<ThirdPersonCharacter>();
    }

    public void Initialize(Transform[] targets, bool shouldLoop, bool shouldWarpOnLoop) {
        targetPositions = targets;
        this.shouldLoop = shouldLoop;
        this.shouldWarpOnLoop = shouldWarpOnLoop;
        SetNextTarget();
        StartCoroutine(UpdatePosition());
    }

    private IEnumerator UpdatePosition() {
        float dist;
        while(targetPositions != null && targetPositions.Length != 0) {
            // Check distance betweenn current target and our position
            if (CheckDistanceToCurrentTarget(out dist)) {
                // We've reached our destination; setting new target
                SetNextTarget();
            } else {
                // We haven't reached our target yet, so let's adjust the speed
                character.Move(agent.desiredVelocity,false,false);
                //character.Move(Vector3.zero,false,false);
            }
            yield return null;
        }
    }

    private void SetNextTarget() {
        if (currentTargetIndex == targetPositions.Length - 1) {
            // Reached the end, warp back to beginning and loop
            if (shouldLoop) {
                if (shouldWarpOnLoop) agent.Warp(targetPositions[0].position);
                currentTargetIndex = 0;
            } else {
                // Tell StreetSim to destroy this agent
                character.Move(Vector3.zero,false,false);
                // ...Which we'll implement later
            }
        } else {
            currentTargetIndex += 1;
        }
        agent.SetDestination(targetPositions[currentTargetIndex].position);
    }

    private bool CheckDistanceToCurrentTarget(out float distance) {
        distance = Vector3.Distance(transform.position,targetPositions[currentTargetIndex].position);
        return distance <= agent.stoppingDistance;
    }
}
