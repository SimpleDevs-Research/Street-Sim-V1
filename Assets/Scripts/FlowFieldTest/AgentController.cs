using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CellController cellController;
    [SerializeField] private GameObject agentPrefab;

    [Header("Settings")]
    [SerializeField, Range(0,100)] private int numAgents = 1;
    [SerializeField] List<Agent> agents = new List<Agent>();
    
    // We instantiate our agent in Start, not Awake. Awake is used to generate the grid first.
    private void Start() {
        GameObject newAgentObject;
        Agent newAgent;
        for(int i = 0; i < numAgents; i++) {
            // We have to instantiate the agent into the world
            // Most is self-explanatory, except for `position`. 
            // 1. We get random coords from GridController
            // 2. We get the cell corresponding to those coods
            // 3,. We get the position from that cell's transform.
            newAgentObject = Instantiate(
                agentPrefab, 
                cellController.GetCellFromCoordinates(cellController.GetRandomCoordinates()).transform.position,
                Quaternion.identity,
                this.transform
            );
            newAgent = newAgentObject.GetComponent<Agent>();
            if (newAgent.Initialize(i, this, cellController)) {
                // With the agent successfully instantiated, we can now add our agent to our list of `agents`
                agents.Add(newAgent);
            }
        }
    }
}
