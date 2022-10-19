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
                StreetSimAgent newAgent = Instantiate(
                    agentPrefabs[newAgentIndex],
                    newPathTargets[0].position,
                    newPathTargets[0].rotation,
                    agentParentFolder) as StreetSimAgent;
                newAgent.Initialize(newPathTargets, StreetSimTrial.ModelBehavior.Safe, false, false);
                activeAgents.Add(newAgent);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    public void DestroyAgent(StreetSimAgent agent) {
        if (activeAgents.Contains(agent)) activeAgents.Remove(agent);
        Destroy(agent.gameObject);
    }
}
