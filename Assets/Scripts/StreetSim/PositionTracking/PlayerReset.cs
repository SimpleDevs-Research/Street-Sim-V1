using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReset : MonoBehaviour
{

    [SerializeField] private bool attachedToPlayer = false;

    private void OnCollisionEnter(Collision other) {
        if (attachedToPlayer) CheckCarCollision(other);
        else CheckPlayerCollision(other);
    }

    private void CheckCarCollision(Collision other) {
        StreetSimCar car = other.gameObject.GetComponent<StreetSimCar>();
        if (car != null && car.GetCurrentSpeed() > 0.25f) {
            StreetSim.S.ResetTrial();
            return;
        }
    }

    private void CheckPlayerCollision(Collision other) {
        if (other.gameObject.GetComponent<EVRA_CharacterController>() != null) {
            StreetSim.S.ResetTrial();
            return;
        }
    }
}
