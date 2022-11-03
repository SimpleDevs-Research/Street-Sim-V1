using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayerDebug : MonoBehaviour
{

    public EVRA_CharacterController charController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector2 movement = new Vector2 (0f, Input.GetAxis("Vertical")).normalized;
        charController.GetMovementInput(movement);
        Vector2 rotation = new Vector2(Input.GetAxis("Horizontal"), 0f).normalized * 0.01f;
        charController.GetRotationInput(rotation);
    }
}
