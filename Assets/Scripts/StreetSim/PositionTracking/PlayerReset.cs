using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReset : MonoBehaviour
{

    [SerializeField] private bool sendBelow = true;

    private void OnTriggerEnter(Collider other) {
        // Debug.Log("Collision with trigger detected");
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null || other.gameObject.layer == 7 ) {
            // Debug.Log("Collision with player detected");
            if (sendBelow) StreetSim.S.FailTrial();
            else StreetSim.S.ResetTrial();
            return;
        }
    }
}
