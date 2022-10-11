using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCollision : MonoBehaviour
{
    private void OnCollisionEnter() {
        //Debug.Log("SOMETHING'S COLLIDING");
    }

    private void OnCollisionStay() {
        Debug.Log("I'M CONTINUOUSLY COLLIDING");
    }

    private void OnCollisionExit() {
        //Debug.Log("A COLLISION HAS STOPPED");
    }
}
