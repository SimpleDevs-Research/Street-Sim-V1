using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReset : MonoBehaviour
{

    [SerializeField] private bool sendBelow = true;

    private void OnTriggerEnter(Collider other) {
        /*
        if (attachedToPlayer) CheckCarCollision(other);
        else CheckPlayerCollision(other);
        */
        Debug.Log("Collision with trigger detected");
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null || other.gameObject.layer == 7 ) {
            Debug.Log("Collision with player detected");
            if (sendBelow) StreetSim.S.FailTrial();
            //if (sendBelow) StreetSim.S.transform.position = new Vector3(StreetSim.S.transform.position.x, 6f, StreetSim.S.transform.position.z);
            else StreetSim.S.ResetTrial();
            return;
        }
    }

    /*
    private void CheckCarCollision(Collision other) {
        StreetSimCar car = other.gameObject.GetComponent<StreetSimCar>();
        if (car != null && car.GetCurrentSpeed() > 0.25f) {
            transform.position = new Vector3(transform.position.x, -6f, transform.position.z);
            return;
        }
    }

    private void CheckPlayerCollision(Collision other) {
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null) {
            StreetSim.S.ResetTrial();
            return;
        }
    }
    */
}
