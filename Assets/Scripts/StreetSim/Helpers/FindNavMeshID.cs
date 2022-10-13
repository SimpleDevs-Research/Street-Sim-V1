using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FindNavMeshID : MonoBehaviour
{   
    public NavMeshAgent navMeshAgent;
    public int agentTypeID;
    private void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    private void Update() {
        agentTypeID = GetAgenTypeIDByName("Humanoid");
    }
    
    public static int GetAgenTypeIDByName(string agentTypeName) {
        int count = NavMesh.GetSettingsCount();
        string[] agentTypeNames = new string[count + 2];
        for (var i = 0; i < count; i++) {
            int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            string name = NavMesh.GetSettingsNameFromID(id);
            if(name == agentTypeName) {
                return id;
            }
        }
        return -1;
    }
}
