using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private List<StreetSimAgent> agentPrefabs = new List<StreetSimAgent>();
    [SerializeField] private List<StreetSimAgent> activeAgents = new List<StreetSimAgent>();
    [SerializeField] private List<NPCPath> nonModelPaths = new List<NPCPath>();
    [SerializeField] private List<NPCPath> modelPaths = new List<NPCPath>();

    private void Awake() {
        AM = this;
        if (agentParentFolder == null) agentParentFolder = this.transform;
        StartCoroutine(PrintAgents());
    }

    private IEnumerator PrintAgents() {
        while(true) {
            if (activeAgents.Count >= numAgentValues[status]) yield return null;
            else {
                int newAgentIndex = (int)(Random.value * agentPrefabs.Count-1);
                int newPathIndex = (int)(Random.value * nonModelPaths.Count-1);
                NPCPath newPath = nonModelPaths[newPathIndex];
                Transform[] newPathTargets;
                if (Random.value < 0.5f) {
                    newPathTargets = new Transform[newPath.points.Length];
                    for(int i = newPath.points.Length-1; i >= 0; i--) {
                        newPathTargets[(newPath.points.Length-1)-i] = newPath.points[i];
                    }
                } else {
                    newPathTargets = newPath.points;
                }
                StreetSimAgent agent;
                PrintAgent(
                    agentPrefabs[newAgentIndex],
                    newPathTargets,
                    out agent
                );
                yield return new WaitForSeconds(1f);
            }
        }
    }

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

    public StreetSimAgent AddAgentManually(StreetSimAgent agent, int pathIndex, StreetSimTrial.ModelBehavior behavior = StreetSimTrial.ModelBehavior.Safe, bool isModel = false) {
        StreetSimAgent newAgent = default(StreetSimAgent);
        if (isModel) {
            if (pathIndex < 0 && pathIndex > modelPaths.Count-1) {
                Debug.Log("[AGENT MANAGER] ERROR: path index does not exist among model paths");
                return newAgent;
            }
            PrintAgent(agent,modelPaths[pathIndex].points, out newAgent, behavior, false, false, false);
            return newAgent;
        } else {
            if (pathIndex < 0 || pathIndex > nonModelPaths.Count-1) {
                Debug.Log("[AGENT MANAGER] ERROR: path index does not exist among non-model paths");
                return newAgent;
            }
            PrintAgent(agent,nonModelPaths[pathIndex].points, out newAgent);
            return newAgent;
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
}
