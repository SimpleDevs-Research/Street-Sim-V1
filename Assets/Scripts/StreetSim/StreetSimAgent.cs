using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(ThirdPersonCharacter))]
public class StreetSimAgent : MonoBehaviour
{
    public enum CrosswalkStatus {
        Normal,
        InFrontOfCrosswalk,
        OnCrosswalk
    }

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ThirdPersonCharacter character;
    [SerializeField] private EVRA_Pointer forwardPointer, downwardPointer;
    [SerializeField] private Transform[] targetPositions; // note that the 1st position is the starting position
    private int currentTargetIndex = -1;
    [SerializeField] private float currentSpeed = 0f;
    private bool shouldLoop, shouldWarpOnLoop;
    [SerializeField] private CrosswalkStatus crosswalkStatus = CrosswalkStatus.Normal;
    public bool stoppedMoving = false;

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
                // We need to first check if we're normally walking or if we're at a crosswalk
                if (forwardPointer.raycastTarget != null || downwardPointer.raycastTarget != null) {
                    Debug.Log(forwardPointer.raycastTarget.gameObject.name + " | " + downwardPointer.raycastTarget.gameObject.name);
                    // We're at a crosswalk - we need to worry about the crosswalk signals
                    // We need to intuite which crosswalk signal to look at. We can use the dot product for that. CLosest to -1 is the most relevant
                    // To get the walking signals, we refer to TrafficSignalController.current
                    TrafficSignal signal = TrafficSignalController.current.GetFacingWalkingSignal(transform.forward);
                    switch(signal.status) {
                        case TrafficSignal.TrafficSignalStatus.Go:
                            // GO GO GO
                            Debug.Log("GO GO GO");
                            character.Move(agent.desiredVelocity,false,false);
                            stoppedMoving = false;
                            break;
                        case TrafficSignal.TrafficSignalStatus.Warning:
                            // HURRY HURRY HURRY
                            Debug.Log("HURRY HURRY HURRY");
                            character.Move(agent.desiredVelocity,false,false);
                            stoppedMoving = false;
                            break;
                        case TrafficSignal.TrafficSignalStatus.Stop:
                            // STOOOOOP
                            Debug.Log("STOOOOOOP");
                            if (!stoppedMoving) character.Move(Vector3.zero,false,false);
                            stoppedMoving = true;
                            break;
                    }
                } else {
                    // No worries, we're not at a crosswalk, so we can move at our desired velocity
                    Debug.Log("I CAN MOOOOOVE");
                    character.Move(agent.desiredVelocity,false,false);
                    stoppedMoving = false;
                }
                //character.Move(agent.desiredVelocity,false,false);
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
