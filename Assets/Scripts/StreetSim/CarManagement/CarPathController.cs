using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class CarPathController : MonoBehaviour
{
    public static CarPathController CP;

    public PathCreator westToEastPath, eastToWestPath;

    private void Awake() {
        CP = this;
    }
}
