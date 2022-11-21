using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityEditor;

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
    private AgentHeadTurn headTurn;
    [SerializeField] private EVRA_Pointer forwardPointer, downwardPointer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform[] targetPositions; // note that the 1st position is the starting position
    private int currentTargetIndex = -1;
    private bool shouldLoop, shouldWarpOnLoop;
    [SerializeField] private Collider m_meshCollider;

    private float m_originalSpeed = 0.4f;
    [SerializeField] private float m_crossDelayTime = 5f;
    [SerializeField] private float m_canCrossDelayTime = 0f;
    private bool m_canCrossDelayInitialized = false, m_canCrossDelayDone = false;
    [SerializeField] private bool m_canCross = false;

    [SerializeField] private StreetSimTrial.ModelBehavior behavior;
    [SerializeField] private StreetSimTrial.TrialDirection direction;
    [SerializeField] private StreetSimTrial.ModelConfidence confidence;

    private IEnumerator checkCarsCoroutine = null;
    public LayerMask carMask;

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

    public void GetAllChildren() {
        string parentName = id.id;
        Dictionary<string, string> idDict = new Dictionary<string, string>() {
            {"Root","Hips"},
            {"Spine1","Abdomen"},
            {"Spine2","Diaphragm"},
            {"Chest","Chest"},
            {"Clavicle.L","ClavicleLeft"},
            {"Shoulder.L","ShoulderLeft"},
            {"Forearm.L","ElbowLeft"},
            {"Hand.L","HandLeft"},
            {"Clavicle.R","ClavicleRight"},
            {"Shoulder.R","ShoulderRight"},
            {"Forearm.R","ElbowRight"},
            {"Hand.R","HandRight"},
            {"Neck","Neck"},
            {"Head","Head"},
            {"Thigh.L","LeftThigh"},
            {"Shin.L","LeftKnee"},
            {"Foot.L","LeftAnkle"},
            {"Toe.L","LeftToes"},
            {"Thigh.R","RightThigh"},
            {"Shin.R","RightKnee"},
            {"Foot.R","RightAnkle"},
            {"Toe.R","RightToes"}
        };
        Component[] children = GetComponentsInChildren<Transform>();
        ExperimentID childID;
        foreach(Transform child in children) {
            if (idDict.ContainsKey(child.gameObject.name)) {
                childID = child.gameObject.GetComponent<ExperimentID>();
                if(childID==null) childID = child.gameObject.AddComponent<ExperimentID>();
                childID.SetID(parentName+"_"+idDict[child.name]);
                childID.SetParent(id);
            }
        }
    }

    public void GenerateMesh() {
        List<string> meshList = new List<string>() {
            "Root",
            "Spine1",
            "Spine2",
            "Chest",
            "Clavicle.L","Shoulder.L","Forearm.L","Hand.L",
            "Clavicle.R","Shoulder.R","Forearm.R","Hand.R",
            "Neck","Head",
            "Thigh.L","Shin.L","Foot.L","Toe.L",
            "Thigh.R","Shin.R","Foot.R","Toe.R"
        };
        GameObject newMeshObject = Instantiate(this.gameObject,Vector3.zero,Quaternion.identity, transform.parent);
        //PrefabUtility.UnpackPrefabInstance(newMeshObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        DestroyImmediate(newMeshObject.GetComponent<ThirdPersonCharacter>());
        DestroyImmediate(newMeshObject.GetComponent<Rigidbody>());
        DestroyImmediate(newMeshObject.GetComponent<CapsuleCollider>());
        DestroyImmediate(newMeshObject.GetComponent<NavMeshAgent>());
        DestroyImmediate(newMeshObject.GetComponent<StreetSimAgent>());
        DestroyImmediate(newMeshObject.GetComponent<Animator>());
        FollowPosition follower = newMeshObject.AddComponent<FollowPosition>();
        follower.toFollow = this.transform;
        follower.offset = Vector3.up * -20f;
        MeshCollider col = newMeshObject.AddComponent<MeshCollider>();
        m_meshCollider = col;
        SkinnedMeshRendererHelper helper = newMeshObject.AddComponent<SkinnedMeshRendererHelper>();
        helper.meshRenderer = renderer;
        helper.collider= col;
        helper.updateDelay = 0.05f;
        ExperimentID newExpID = newMeshObject.GetComponent<ExperimentID>();
        newExpID.SetRefID(newExpID.id);
        newExpID.SetID(newExpID.id+"Mesh");
        Component[] children = newMeshObject.GetComponentsInChildren<ExperimentID>();
        foreach(ExperimentID child in children) {
            if (meshList.Contains(child.gameObject.name)) {
                child.SetRefID(child.id);
                child.SetID(child.id+"Mesh");
            }
        }
    }

    private void Awake() {
        if (id == null) id = GetComponent<ExperimentID>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (character == null) character = GetComponent<ThirdPersonCharacter>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
        headTurn = GetComponent<AgentHeadTurn>();
    }

    public void Initialize(
        Transform[] targets, 
        StreetSimTrial.ModelBehavior behavior, 
        StreetSimTrial.ModelConfidence confidence,
        float speed,
        float canCrossDelay,
        bool shouldLoop, 
        bool shouldWarpOnLoop, 
        StreetSimTrial.TrialDirection direction,
        AgentType s_agentType
    ) {
        targetPositions = targets;
        this.shouldLoop = shouldLoop;
        this.shouldWarpOnLoop = shouldWarpOnLoop;
        this.behavior = behavior;
        this.confidence = confidence;
        this.direction = direction;
        this.m_agentType = s_agentType;
        collider.enabled = true;
        rigidbody.isKinematic = false;
        agent.enabled = true;
        agent.speed = speed;
        m_originalSpeed = speed;
        character.enabled = true;
        animator.enabled = true;
        agent.isStopped = false;
        currentTargetIndex = -1;
        m_meshCollider.enabled = true;
        m_riskyButCrossing = false;

        m_canCross = false;
        m_canCrossDelayTime = (canCrossDelay == 0f) 
            ? UnityEngine.Random.Range(0f , canCrossDelay+0.05f)
            : UnityEngine.Random.Range(canCrossDelay-0.05f,canCrossDelay+0.05f);
        m_canCrossDelayInitialized = false;
        m_canCrossDelayDone = false;
        StartCoroutine(CanCrossCoroutine());
        StartCoroutine(WalkAnimationStepAudio());
        if (this.confidence == StreetSimTrial.ModelConfidence.NotConfident) {
            StartCoroutine(BeCautious());
        }
        SetNextTarget();
    }

    private IEnumerator WalkAnimationStepAudio() {
        animator.SetBool("Crouch",false);
        float prevFoot = Mathf.Sign(animator.GetFloat("JumpLeg"));
        float curFoot = 0;
        while(agent.enabled) {
            curFoot = Mathf.Sign(animator.GetFloat("JumpLeg"));
            if (curFoot != prevFoot) {
                AudioClip footstep = StreetSimAgentManager.AM.GetRandomFootstep();
                audioSource.PlayOneShot(footstep,1f);
                //audioSource.clip = footstep;
                //audioSource.Play();
            }
            prevFoot = curFoot;
            yield return null;
        }
        yield return null;
    }

    private IEnumerator BeCautious() {
        // We're waiting
        //agent.speed = m_originalSpeed * 0.9f;
        headTurn.currentTargetTransform = (UnityEngine.Random.Range(0f,1f) > 0.5f) 
            ? StreetSimAgentManager.AM.EastLookAtTarget
            : StreetSimAgentManager.AM.WestLookAtTarget;
        while(true) {
            yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f,4f));
            if (m_riskyButCrossing) {
                yield return null;
                break;
            }
            // Loop back and forth between targets
            headTurn.currentTargetTransform = (headTurn.currentTargetTransform == StreetSimAgentManager.AM.WestLookAtTarget) 
                ? StreetSimAgentManager.AM.EastLookAtTarget
                : StreetSimAgentManager.AM.WestLookAtTarget;
        }
        //yield return null;
        while(true) {
            yield return null;
            if (m_canCrossDelayDone) break;
        }
        yield return new WaitForSeconds(1f);
        headTurn.currentTargetTransform = null;
        //agent.speed = m_originalSpeed;
    }

    private IEnumerator CanCrossCoroutine() {
        m_canCross = false;
        yield return new WaitForSeconds(m_crossDelayTime);
        m_canCross = true;
    }

    private IEnumerator CheckCarsOnSide() {
        Vector3 dir;
        RaycastHit hit;
        while(agent.enabled) {
            dir = (transform.position.z < 0) ? Vector3.left : Vector3.right;
            if (Physics.SphereCast(transform.position,1f,dir,out hit,25f,carMask)) {
                agent.speed = m_originalSpeed * 1.5f;
            } else {
                agent.speed = m_originalSpeed;
            }
            yield return new WaitForSeconds(0.05f);
        }
        yield return null;
    }

    private void Update() {
        float dist = 0f, angleDiff;
        bool safe;
        if (targetPositions != null && targetPositions.Length != 0) {
            // Check distance betweenn current target and our position
            if (CheckDistanceToCurrentTarget(out dist)) {
                // We've reached our destination; setting new target
                SetNextTarget();
            } else {
                // We haven't reached our target yet, so let's adjust the speed
                // We need to first check if we're normally walking or if we're at a crosswalk
                // At this point, it's been deemed safe to cross including the cross delay time. We're just waiting for the right moment.
                if (forwardPointer.raycastTarget != null || downwardPointer.raycastTarget != null) {
                    // We're at a crosswalk - we need to worry about the crosswalk signals
                    // If the light is safe, we'll cross no matter what.
                    switch(TrafficSignalController.current.GetFacingWalkingSignal(transform.forward, out angleDiff).status) {
                        case TrafficSignal.TrafficSignalStatus.Go:
                            agent.isStopped = false;
                            character.Move(agent.desiredVelocity,false,false);
                            break;
                        case TrafficSignal.TrafficSignalStatus.Warning:
                            agent.isStopped = false;
                            character.Move(agent.desiredVelocity,false,false);
                            break;
                        default:
                            // We now need to worry based on the participant's behavior
                            switch(behavior) {
                                case StreetSimTrial.ModelBehavior.Risky:
                                    // We need to wait until it's safe to cross
                                    // Firstly, we have a buffer we need to get done with
                                    if (!m_canCross) {
                                        character.Move(Vector3.zero,false,false);
                                        agent.isStopped = true;
                                        break;
                                    }
                                    // At this point, we need to start looking left and right
                                    safe = TrafficSignalController.current.GetSafety(transform.position.z < 0f, agent.speed, m_canCrossDelayTime);
                                    // m_riskyButCrossing becomes and stays true at the moment it calculates that it's safe
                                    m_riskyButCrossing = m_riskyButCrossing || safe;
                                    // If it's NOT riskyButCrossable, we wait still
                                    if (!m_riskyButCrossing) {
                                        character.Move(Vector3.zero,false,false);
                                        agent.isStopped = true;
                                        break;
                                    }
                                    // If it's riskyButCrossable, we need to wait for a tad until the cross delay is completed
                                    if (!m_canCrossDelayInitialized) {
                                        character.Move(Vector3.zero,false,false);
                                        agent.isStopped = true;
                                        m_canCrossDelayInitialized = true;
                                        StartCoroutine(DelayCrossing());
                                        break;
                                    }
                                    if (m_canCrossDelayDone) {
                                        agent.isStopped = false;
                                        character.Move(agent.desiredVelocity,false,false);
                                        break;
                                    }
                                    character.Move(Vector3.zero,false,false);
                                    agent.isStopped = true;
                                    break;
                                default:
                                    // We simply wait until it's time to cross
                                    character.Move(Vector3.zero,false,false);
                                    agent.isStopped = true;
                                    break;
                            }
                            break;
                    }
                    /*
                    if (TrafficSignalController.current.GetFacingWalkingSignal(transform.forward, out angleDiff).status == TrafficSignal.TrafficSignalStatus.Go) {
                    }
                    

                    // This will be entirely dependent on the model's behavior, which we've passed during initialization
                    if (m_riskyButCrossing && m_canCrossDelayDone) {
                        if (m_canCrossDelayDone) {
                            agent.isStopped = false;
                            character.Move(agent.desiredVelocity,false,false);
                        }
                        else if (!m_canCrossDelayInitialized) {
                            Debug.Log("CanCrossDelayInitialized");
                            character.Move(Vector3.zero,false,false);
                            agent.isStopped = true;
                            m_canCrossDelayInitialized = true;
                            StartCoroutine(DelayCrossing());
                        } else {
                            character.Move(Vector3.zero,false,false);
                            agent.isStopped = true;
                        }
                    }
                    switch(behavior) {
                        case StreetSimTrial.ModelBehavior.Risky:
                            // Prevent the model from doing anything if we can't cross just yet because of the initial delay
                            if (!m_canCross) {
                                character.Move(Vector3.zero,false,false);
                                agent.isStopped = true;
                                break;
                            }
                            if (TrafficSignalController.current.GetFacingWalkingSignal(transform.forward, out angleDiff).status == TrafficSignal.TrafficSignalStatus.Go) {
                                m_riskyButCrossing = true;
                                safe = true;
                                agent.isStopped = false;
                                character.Move(agent.desiredVelocity,false,false);
                                break;
                            }
                            // Calculate safety of crosswalk traversal, including the delay time
                            safe = TrafficSignalController.current.GetSafety(transform.position.z < 0f, agent.speed, m_canCrossDelayTime);
                            // m_riskyButCrossing becomes and stays true at the moment it calculates that it's safe
                            m_riskyButCrossing = m_riskyButCrossing || safe;
                            break;
                        case StreetSimTrial.ModelBehavior.Safe:
                            // We need to intuite which crosswalk signal to look at. We can use the dot product for that. CLosest to -1 is the most relevant
                            // To get the walking signals, we refer to TrafficSignalController.current
                            TrafficSignal signal = TrafficSignalController.current.GetFacingWalkingSignal(transform.forward, out angleDiff);
                            switch(signal.status) {
                                case TrafficSignal.TrafficSignalStatus.Go:
                                    // GO GO GO
                                    m_riskyButCrossing = true;
                                    agent.isStopped = false;
                                    character.Move(agent.desiredVelocity,false,false);
                                    break;
                                case TrafficSignal.TrafficSignalStatus.Warning:
                                    // HURRY HURRY HURRY
                                    m_riskyButCrossing = true;
                                    agent.isStopped = false;
                                    character.Move(agent.desiredVelocity,false,false);
                                    break;
                                case TrafficSignal.TrafficSignalStatus.Stop:
                                    // STOOOOOP... unless you're still on the crosswalk
                                    if (downwardPointer.raycastTarget != null) {
                                        // GET OFF THE CROSSWALK
                                        m_riskyButCrossing = true;
                                        agent.isStopped = false;
                                        character.Move(agent.desiredVelocity * 2f,false,false);
                                    } else {
                                        m_riskyButCrossing = false;
                                        character.Move(Vector3.zero,false,false);
                                        agent.isStopped = true;
                                    }
                                    break;
                            }
                            break;
                    }
                    */
                } 
                else {
                    // No worries, we're not at a crosswalk, so we can move at our desired velocity
                    agent.isStopped = false;
                    character.Move(agent.desiredVelocity,false,false);
                }
            }
            if (m_agentType == AgentType.Model) {
                // The agent is currently on the crosswalk, so we need to inform the system that the agent is crossing
                if (downwardPointer.raycastTarget != null) StreetSim.S.StartAttempt(id, StreetSim.S.trialFrameTimestamp, direction);
                else StreetSim.S.EndAttempt(id,StreetSim.S.trialFrameTimestamp,true);
            }
        }
    }

    private IEnumerator DelayCrossing() {
        yield return new WaitForSeconds(m_canCrossDelayTime);
        m_canCrossDelayDone = true;
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
        if (m_agentType == AgentType.Model) StreetSim.S.EndAttempt(id, StreetSim.S.trialFrameTimestamp, true);
    }

    public ExperimentID GetID() {
        return id;
    }
}
