using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStartSimulation : MonoBehaviour
{
    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null) {
            StreetSim.S.StartSimulation();
            return;
        }
    }
}
