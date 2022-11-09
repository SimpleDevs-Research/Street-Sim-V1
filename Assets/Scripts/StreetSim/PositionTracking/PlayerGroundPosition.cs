using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundPosition : MonoBehaviour
{

    // Update is called once per frame
    void Update() {
        transform.position = new Vector3(StreetSim.S.xrCamera.position.x, StreetSim.S.xrTrackingSpace.position.y, StreetSim.S.xrCamera.position.z);
    }
}
