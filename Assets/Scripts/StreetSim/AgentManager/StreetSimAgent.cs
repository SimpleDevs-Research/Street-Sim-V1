using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

//[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(ThirdPersonCharacter))]
public class StreetSimAgent : MonoBehaviour
{
    public enum AgentType {
        NPC,
        Model,
        Follower
    }

    [SerializeField] private ExperimentID id;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private ThirdPersonCharacter character;
    [SerializeField] private SkinnedMeshRenderer renderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider collider;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private EVRA_Pointer forwardPointer, downwardPointer;
    [SerializeField] private Transform[] targetPositions; // note that the 1st position is the starting position
    private int currentTargetIndex = -1;
    private bool shouldLoop, shouldWarpOnLoop;
    [SerializeField] private Collider m_meshCollider;

    [SerializeField] private float m_crossDelayTime = 5f;
    [SerializeField] private bool m_canCross = false;

    [SerializeField] private StreetSimTrial.ModelBehavior behavior;
    [SerializeField] private StreetSimTrial.TrialDirection direction;

    private bool m_riskyButCrossing = false;
    public bool riskyButCrossing {
        get => m_riskyButCrossing;
        set {}
    }
    private AgentType m_agentType = AgentType.NPC;
    public AgentType agentType {
        get => m_agentType;
        set {}
    }

    private void Awake() {
        if (id == null) id = GetComponent<ExperimentID>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (character == null) character = GetComponent<ThirdPersonCharacter>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(
        Transform[] targets, 
        StreetSimTrial.ModelBehavior behavior, 
        bool shouldLoop, 
        bool shouldWarpOnLoop, 
        StreetSimTrial.TrialDirection direction,
        AgentType s_agentType
    ) {
        targetPositions = targets;
        this.shouldLoop = shouldLoop;
        this.shouldWarpOnLoop = shouldWarpOnLoop;
        this.behavior = behavior;
        this.direction = direction;
        this.m_agentType = s_agentType;
        collider.enabled = true;
        rigidbody.isKinematic = false;
        agent.enabled = true;
        character.enabled = true;
        animator.enabled = true;
        agent.isStopped = false;
        currentTargetIndex = -1;
        m_meshCollider.enabled = true;
        m_riskyButCrossing = false;

        m_canCross = false;
        StartCoroutine(CanCrossCoroutine());
        SetNextTarget();
    }

    private IEnumerator CanCrossCoroutine() {
        m_canCross = false;
        yield return new WaitForSeconds(m_crossDelayTime);
        m_canCross = true;
    }

    private void Update() {
        float dist = 0f, angleDiff;
        if (targetPositions != null && targetPositions.Length != 0) {
            // Check distance betweenn current target and our position
            if (CheckDistanceToCurrentTarget(out dist)) {
                // We've reached our destination; setting new target
                SetNextTarget();
            } else {
                // We haven't reached our target yet, so let's adjust the speed
                // We need to first check if we're normally walking or if we're at a crosswalk
                if (forwardPointer.raycastTarget != null || downwardPointer.raycastTarget != null) {
                    // We're at a crosswalk - we need to worry about the crosswalk signals
                    // This will be entirely dependent on the model's behavior, which we've passed during initialization
                    switch(behavior) {
                        case StreetSimTrial.ModelBehavior.Risky:
                            // This will go no matter what the light signal is, but only if there aren't any incoming cars
                            m_riskyButCrossing = m_riskyButCrossing || TrafficSignalController.current.GetSafety(transform.position.z < 0);
                            if (m_riskyButCrossing && m_canCross) {
                                agent.isStopped = false;
                                character.Move(agent.desiredVelocity,false,false);
                            } else {
                                character.Move(Vector3.zero,false,false);
                                agent.isStopped = true;
                            }
                            break;
                        case StreetSimTrial.ModelBehavior.Safe:
                            // We need to intuite which crosswalk signal to look at. We can use the dot product for that. CLosest to -1 is the most relevant
                            // To get the walking signals, we refer to TrafficSignalController.current
                            TrafficSignal signal = TrafficSignalController.current.GetFacingWalkingSignal(transform.forward, out angleDiff);
                            switch(signal.status) {
                                case TrafficSignal.TrafficSignalStatus.Go:
                                    // GO GO GO
                                    agent.isStopped = false;
                                    character.Move(agent.desiredVelocity,false,false);
                                    break;
                                case TrafficSignal.TrafficSignalStatus.Warning:
                                    // HURRY HURRY HURRY
                                    agent.isStopped = false;
                                    character.Move(agent.desiredVelocity,false,false);
                                    break;
                                case TrafficSignal.TrafficSignalStatus.Stop:
                                    // STOOOOOP... unless you're still on the crosswalk
                                    if (downwardPointer.raycastTarget != null) {
                                        // GET OFF THE CROSSWALK
                                        agent.isStopped = false;
                                        character.Move(agent.desiredVelocity * 2f,false,false);
                                    } else {
                                        character.Move(Vector3.zero,false,false);
                                        agent.isStopped = true;
                                    }
                                    break;
                            }
                            break;
                    }
                    if (m_agentType == AgentType.Model) {
                        // The agent is currently on the crosswalk, so we need to inform the system that the agent is crossing
                        /*
                        if (downwardPointer.raycastTarget != null) StreetSim.S.StartAgentAttempt();
                        else StreetSim.S.EndAgentAttempt();
                        */
                        if (downwardPointer.raycastTarget != null) StreetSim.S.StartAttempt(id, Time.time, direction);
                        else StreetSim.S.EndAttempt(id,Time.time,true);
                    }
                } 
                else {
                    // No worries, we're not at a crosswalk, so we can move at our desired velocity
                    agent.isStopped = false;
                    character.Move(agent.desiredVelocity,false,false);
                }
            }
        }
    }

    private void SetNextTarget() {
        if (currentTargetIndex == targetPositions.Length - 1) {
            // Reached the end, warp back to beginning and loop
            if (shouldLoop) {
                if (shouldWarpOnLoop) agent.Warp(targetPositions[0].position);
                currentTargetIndex = 0;
                agent.SetDestination(targetPositions[currentTargetIndex].position);
            } else {
                // Tell StreetSim to destroy this agent
                character.Move(Vector3.zero,false,false);
                DeactiveAgentManually();
                StreetSimAgentManager.AM.DestroyAgent(this);
            }
        } else {
            currentTargetIndex += 1;
            agent.SetDestination(targetPositions[currentTargetIndex].position);
        }
    }

    private bool CheckDistanceToCurrentTarget(out float distance) {
        distance = Vector3.Distance(transform.position,targetPositions[currentTargetIndex].position);
        return distance <= agent.stoppingDistance;
    }

    public SkinnedMeshRenderer GetRenderer() {
        return renderer;
    }

    public void DeactiveAgentManually() {
        targetPositions = new Transform[0];
        agent.enabled = false;
        character.enabled = false;
        animator.enabled = false;
        collider.enabled = false;
        rigidbody.isKinematic = true;
        m_meshCollider.enabled = false;
        //if (m_agentType == AgentType.Model) StreetSim.S.EndAgentAttempt();
    }
}
