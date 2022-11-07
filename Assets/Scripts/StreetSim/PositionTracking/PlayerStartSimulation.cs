using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStartSimulation : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        Debug.Log("Potential Starter Colliding");
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null || other.gameObject.layer == 7 ) {
            Debug.Log("Should Start Simulation...");
            StreetSim.S.StartSimulation();
            return;
        }
    }
}
