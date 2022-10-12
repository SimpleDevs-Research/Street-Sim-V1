using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class CarPathFollower : MonoBehaviour
{
    public enum CarStatus {
        Moving,
        SlowlyStopping,
        FastStopping
    }

    public PathCreator pathCreator;
    public float maxSpeed = 5f;
    public float currentSpeed = 0f;
    public float acceleration = 2.5f;
    private float distanceTraveled;

    [SerializeField] private Transform frontOfCar;
    [SerializeField] private EVRA_Pointer pointer;
    [SerializeField] private Transform checkBoxRef;
    [SerializeField] private Vector3 checkBoxRefSize = new Vector3(3f,0.5f,3f);
    [SerializeField] private CarStatus m_status = CarStatus.Moving;

    [SerializeField] private RemoteCollider farCollider;
    [SerializeField] private RemoteCollider closeCollider;

    private void Update() {
        if (pathCreator == null) return;
        UpdateSpeed();
        distanceTraveled += currentSpeed * Time.deltaTime;
        transform.position = pathCreator.path.GetPointAtDistance(distanceTraveled);
        transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTraveled);
    }
    private void UpdateSpeed() {
        // Check if raycast hit anything
        /*
        float dist = 0f;
        Collider[] colliders = Physics.OverlapBox(checkBoxRef.position,checkBoxRefSize);
        //Debug.Log(colliders.Length);
        if (colliders.Length == 0) m_status = CarStatus.Moving;
        else {
            foreach(Collider col in colliders) {
                dist = Vector3.Distance(col.transform.position,frontOfCar.position);
                Debug.Log(dist);
                if (dist < 3f) {
                    Debug.Log("Slowly stop...");
                    m_status = CarStatus.SlowlyStopping;
                }
                if (dist < 1f) {
                    Debug.Log("QUICK STOP!");
                    m_status = CarStatus.FastStopping;
                    break;
                }
            }
        }
        */

        if (closeCollider.colliders.Count > 0) {
            m_status = CarStatus.FastStopping;
        } 
        else if (farCollider.colliders.Count > 0) {
            m_status = CarStatus.SlowlyStopping;
        }
        else {
            m_status = CarStatus.Moving;
        }

        switch(m_status) {
            case CarStatus.Moving:
                if (currentSpeed < maxSpeed) currentSpeed += acceleration * Time.deltaTime;
                else currentSpeed = maxSpeed;
                break;
            case CarStatus.SlowlyStopping:
                if (currentSpeed > 0f) currentSpeed -= acceleration * Time.deltaTime;
                else currentSpeed = 0f;
                break;
            case CarStatus.FastStopping:
                if (currentSpeed > 0f) currentSpeed -= acceleration * 2f * Time.deltaTime;
                else currentSpeed = 0f;
                break;
        }
    }
}
