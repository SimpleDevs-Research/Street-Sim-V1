using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNext : MonoBehaviour
{
    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null) {
            StreetSim.S.TriggerNextTrial();
            return;
        }
    }
}
