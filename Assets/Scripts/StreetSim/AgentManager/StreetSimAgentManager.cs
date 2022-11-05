using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;

public class StreetSimAgentManager : MonoBehaviour
{
    public static StreetSimAgentManager AM;
    public enum AgentManagerStatus {
        Off,
        NoCongestion,
        MinimalCongestion,
        SomeCongestion,
        Congestion
    }
    public Dictionary<AgentManagerStatus, int> numAgentValues = new Dictionary<AgentManagerStatus, int> {
        { AgentManagerStatus.Off, 0 },
        { AgentManagerStatus.NoCongestion, 5 },
        { AgentManagerStatus.MinimalCongestion, 10},
        { AgentManagerStatus.SomeCongestion, 15 },
        { AgentManagerStatus.Congestion, 20 }
    };
    public AgentManagerStatus status = AgentManagerStatus.Off;

    [SerializeField] private Transform agentParentFolder;
    [SerializeField] private Transform idleTargetRef;

    [SerializeField] private List<StreetSimAgent> m_agents = new List<StreetSimAgent>();
    [SerializeField] private Queue<StreetSimAgent> m_inactiveAgents = new Queue<StreetSimAgent>();
    [SerializeField] private Queue<StreetSimAgent> m_waitingAgents = new Queue<StreetSimAgent>();
    [SerializeField] private List<StreetSimAgent> m_activeAgents = new List<StreetSimAgent>();

    [SerializeField] private List<StreetSimAgent> agentPrefabs = new List<StreetSimAgent>();
    [SerializeField] private List<StreetSimAgent> activeAgents = new List<StreetSimAgent>();
    [SerializeField] private List<NPCPath> nonModelPaths = new List<NPCPath>();
    [SerializeField] private List<NPCPath> modelPaths = new List<NPCPath>();

    private StreetSimAgent m_currentModel = null;

    private void Awake() {
        AM = this;
        if (agentParentFolder == null) agentParentFolder = this.transform;
        m_inactiveAgents = new Queue<StreetSimAgent>(m_agents.Shuffle());
        StartCoroutine(PrintAgents());
    }

    private void Update() {
        if (m_activeAgents.Count + m_waitingAgents.Count < numAgentValues[status]) QueueNextAgent();
    }
    public void QueueNextAgent() {
        if (m_inactiveAgents.Count == 0) return;
        StreetSimAgent nextAgent = m_inactiveAgents.Dequeue();
        m_waitingAgents.Enqueue(nextAgent);
    }

    private IEnumerator PrintAgents() {
        while(true) {
            // Return early if 1) we're off, 2) there are more than enough agents already active, or 3) there aren't any waiting agents anymore
            if (status == AgentManagerStatus.Off) {
                yield return null;
                continue;
            }
            if (m_activeAgents.Count >= numAgentValues[status]) {
                yield return null;
                continue;
            }
            if (m_waitingAgents.Count == 0) {
                yield return null;
                continue;
            }

            // Get the agent that we want from our queue
            StreetSimAgent agent = m_waitingAgents.Dequeue();
            //int newAgentIndex = (int)(Random.value * agentPrefabs.Count-1);

            // Randomly assign a path to this particular agent
            int newPathIndex = (int)(Random.value * nonModelPaths.Count-1);
            NPCPath newPath = nonModelPaths[newPathIndex];

            // Should the model move in the prescribed direction, or the oppoosite path? We randomly decide.
            Transform[] newPathTargets;
            if (Random.value < 0.5f) {
                newPathTargets = new Transform[newPath.points.Length];
                for(int i = newPath.points.Length-1; i >= 0; i--) {
                    newPathTargets[(newPath.points.Length-1)-i] = newPath.points[i];
                }
            } else {
                newPathTargets = newPath.points;
            }

            // We initialize the agent
            InitializeAgent(agent, newPathTargets);

            /*
            StreetSimAgent agent;
            PrintAgent(
                agentPrefabs[newAgentIndex],
                newPathTargets,
                out agent
            );
            */

            yield return new WaitForSeconds(1f);
        }
    }

    private void InitializeAgent(
        StreetSimAgent agent,
        Transform[] path,
        StreetSimTrial.ModelBehavior behavior = StreetSimTrial.ModelBehavior.Safe,
        bool shouldLoop = false,
        bool shouldWarpOnLoop = false,
        bool shouldAddToActive = true
    ) {
        agent.transform.position = path[0].position;
        agent.transform.rotation = path[0].rotation;
        Debug.Log("agent with name " + agent.gameObject.name + " has a position that is now " + agent.transform.position);
        agent.Initialize(path, behavior, shouldLoop, shouldWarpOnLoop);
        if (shouldAddToActive) m_activeAgents.Add(agent);
    }
    public void DestroyAgent(StreetSimAgent agent) {
        if (m_activeAgents.Contains(agent)) m_activeAgents.Remove(agent);
        agent.transform.position = idleTargetRef.position;
    }


    /*
    private bool PrintAgent(
        StreetSimAgent prefab, 
        Transform[] path,
        out StreetSimAgent newAgent,
        StreetSimTrial.ModelBehavior behavior = StreetSimTrial.ModelBehavior.Safe, 
        bool shouldLoop = false, 
        bool shouldWarpOnLoop = false, 
        bool shouldAddToActive = true
    ) {
        newAgent = Instantiate(
            prefab,
            path[0].position,
            path[1].rotation,
            agentParentFolder
        ) as StreetSimAgent;
        newAgent.Initialize(path, behavior, shouldLoop, shouldWarpOnLoop);
        if (shouldAddToActive) activeAgents.Add(newAgent);
        return true;
    }
    public void DestroyAgent(StreetSimAgent agent) {
        if (activeAgents.Contains(agent)) activeAgents.Remove(agent);
        Destroy(agent.gameObject);
    }
    */

    public void AddAgentManually(StreetSimAgent agent, int pathIndex, StreetSimTrial.ModelBehavior behavior = StreetSimTrial.ModelBehavior.Safe, bool isModel = false) {
        //StreetSimAgent newAgent = default(StreetSimAgent);
        if (isModel) {
            if (pathIndex < 0 && pathIndex > modelPaths.Count-1) {
                Debug.Log("[AGENT MANAGER] ERROR: path index does not exist among model paths");
                // return newAgent;
                return;
            }
            //DestroyModel();
            if (m_currentModel != null) DestroyAgent(m_currentModel);
            // PrintAgent(agent,modelPaths[pathIndex].points, out newAgent, behavior, false, false, false);
            InitializeAgent(agent, modelPaths[pathIndex].points, behavior, false, false, false);
            //m_currentModel = newAgent;
            m_currentModel = agent;
            // StreetSimModelMapper.M.MapMeshToModel(m_currentModel);
            //return newAgent;
            return;
        } else {
            if (pathIndex < 0 || pathIndex > nonModelPaths.Count-1) {
                Debug.Log("[AGENT MANAGER] ERROR: path index does not exist among non-model paths");
                return;
            }
            InitializeAgent(agent,nonModelPaths[pathIndex].points);
            // return newAgent;
            return;
        }
    }

    public void SetCongestionStatus(AgentManagerStatus newStatus, bool shouldReset = false) {
        status = newStatus;
        if (shouldReset && activeAgents.Count > 0) {
            Queue<StreetSimAgent> deleteQueue = new Queue<StreetSimAgent>(activeAgents);
            while(deleteQueue.Count > 0) {
                DestroyAgent(deleteQueue.Dequeue());
            }
        }
    }

    public void DestroyModel() {
        if (m_currentModel == null) return;
        m_currentModel.DeactiveAgentManually();
        DestroyAgent(m_currentModel);
        m_currentModel = null;
        /*
        StreetSimModelMapper.M.DestroyMesh();
        if (m_currentModel == null) return;
        DestroyAgent(m_currentModel);
        m_currentModel = null;
        */
    }
}
