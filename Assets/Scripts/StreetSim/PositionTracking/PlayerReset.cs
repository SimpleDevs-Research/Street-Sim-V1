using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReset : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other) {
        StreetSimCar car = other.gameObject.GetComponent<StreetSimCar>();
        if (car != null && car.GetCurrentSpeed() > 0.25f) {
            StreetSim.S.ResetTrial();
        }
    }
}
