using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAgent : MonoBehaviour
{   
    /*
    [SerializeField] private string m_carRouteName;
    [SerializeField] private CarRoute m_currentRoute;
    [SerializeField] private int m_currentRouteNodeIndex = -1;
    [SerializeField] private Transform m_currentRouteNode;

    [SerializeField] private float m_movementSpeed;
    private Vector3 velocity = Vector3.zero;

    private Vector3 startPosition;
    private float currentTime;
    private bool reachedTarget = true;
    private float lerp, smoothLerp;

    private void Start() {
        CarController.current.AddCar(this);
        if (CarController.current.GetRoute(m_carRouteName,out m_currentRoute)) {
            m_currentRouteNodeIndex = 0;
            CheckNode();
        }
    }

    private void CheckNode() {
        startPosition = transform.position;
        currentTime = 0f;
        lerp = 0f;
        smoothLerp = 0f;
        m_currentRouteNode = m_currentRoute.positions[m_currentRouteNodeIndex];
    }
    */
    
    /*
    // Update is called once per frame
    private void Update() {
        if (m_currentRoute == null || m_currentRouteNode == null) return;
        if (Vector3.Distance(transform.position,m_currentRouteNode.position) >= 0.05f) {
            // Keep moving to the next node
            // We first need to define movement speed
            lerp =  Mathf.MoveTowards(lerp, 1, Time.deltaTime * m_movementSpeed);
            smoothLerp = Mathf.SmoothStep(0,1,lerp);
            // currentTime += Time.deltaTime * m_movementSpeed;
            transform.position = Vector3.Lerp(startPosition, m_currentRouteNode.position, smoothLerp);
        }
        else {
            if (m_currentRouteNodeIndex < m_currentRoute.positions.Count - 1) {
                // Advance to the next node
                m_currentRouteNodeIndex+=1;
                CheckNode();
            } else {
                // We've reached the end of the route
                Debug.Log("CAR HAS REACHED ROUTE END");
            }
        }
    }

    private IEnumerator SmoothStepToTarget(Vector3 targetPosition) {
        Vector3 startPosition= transform.position;
        float lerp = 0;
        float smoothLerp = 0;
        reachedTarget = false;
 
        while(lerp<1 && 5>0)
        {
            lerp = Mathf.MoveTowards(lerp,1,Time.deltaTime / 5f);
            smoothLerp = Mathf.SmoothStep(0,1,lerp);
            transform.position = Vector3.Lerp(startPosition,targetPosition,smoothLerp);
            yield return null;
        }
 
        transform.position = targetPosition;
        reachedTarget = true;
    }
    */
}
