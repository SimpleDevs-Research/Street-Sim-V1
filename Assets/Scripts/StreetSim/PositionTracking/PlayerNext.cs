using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNext : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null || other.gameObject.layer == 7) {
            StreetSim.S.TriggerNextTrial();
            return;
        }
    }
}
