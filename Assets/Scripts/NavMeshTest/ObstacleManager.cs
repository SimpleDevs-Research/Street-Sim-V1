using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] 
public class ObstaclePath {
    public Transform startPosition;
    public Transform endPosition;
}

public class ObstacleManager : MonoBehaviour
{

    [SerializeField] private List<GameObject> objectsList = new List<GameObject>();


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
