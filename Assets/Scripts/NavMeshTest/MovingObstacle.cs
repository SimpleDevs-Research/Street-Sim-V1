using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    public float speed = 2f;
    Vector3 start, end;

    void Start()
    {
        start = transform.position - new Vector3(0f,0f,15f);
        end = transform.position + new Vector3(0f,0f,15f);
    }

    void Update()
    {
        //PingPong between 0 and 1
        float time = Mathf.PingPong(Time.time * speed, 1);
        transform.position = Vector3.Lerp(start, end, time);
    }
    }
