using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlowFieldManager flowFieldManager;
    [SerializeField] private CellController cellController;
    [SerializeField] private GameObject agentPrefab;

    [Header("Settings")]
    [SerializeField, Range(0,100)] private int numAgents = 1;
    [SerializeField] List<Agent> agents = new List<Agent>();
    
    // This is called when the flow field has finished generating the flow fields
    public void Start() {
        StartCoroutine(CreateAgents());
    }

    private IEnumerator CreateAgents() {
        GameObject newAgentObject;
        Agent newAgent;
        Vector3 pos;
        for(int i = 0; i < numAgents; i++) {
            // We have to instantiate the agent into the world
            // Most is self-explanatory, except for `position`. 
            // 1. We get random coords from GridController
            // 2. We get the cell corresponding to those coods
            // 3,. We get the position from that cell's transform.
            pos = cellController.GetCellFromCoordinates(cellController.GetRandomCoordinates()).worldPos;
            newAgentObject = Instantiate(
                agentPrefab, 
                pos,
                Quaternion.identity,
                this.transform
            );
            newAgent = newAgentObject.GetComponent<Agent>();
            if (newAgent.Initialize(i, flowFieldManager, this, cellController)) {
                // With the agent successfully instantiated, we can now add our agent to our list of `agents`
                agents.Add(newAgent);
            }
            yield return null;
        }
    }
}
