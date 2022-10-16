using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Helpers;

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

    [SerializeField] private CarStatus m_status = CarStatus.Moving;

    [SerializeField] private RemoteCollider closeCollider;
    [SerializeField] private RemoteCollider farCollider;

    [SerializeField] private Transform[] wheels;
    [SerializeField] private float wheelRadius;
    private Vector3 prevPosition;

    private void Start() {
        if (pathCreator != null) {
            transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
            prevPosition = transform.position - transform.forward;
            distanceTraveled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }
    }
    private void Update() {
        if (pathCreator == null) return;
        UpdateSpeed();
        distanceTraveled += currentSpeed * Time.deltaTime;
        transform.position = pathCreator.path.GetPointAtDistance(distanceTraveled);
        transform.rotation = Quaternion.LookRotation(transform.position - prevPosition, Vector3.up);
        prevPosition = transform.position;
        if (wheels.Length > 0) {
            foreach(Transform wheel in wheels) {
                wheel.Rotate(currentSpeed,0f,0f,Space.Self);
            }
        }
        //transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTraveled);
    }
    private void UpdateSpeed() {
        if (closeCollider.colliders.Count > 0) {
            Collider closest = closeCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                m_status = CarStatus.Moving;
                currentSpeed = (currentSpeed < potentialFollower.currentSpeed) ? currentSpeed += acceleration * Time.deltaTime : (currentSpeed > potentialFollower.currentSpeed) ? currentSpeed -= acceleration * Time.deltaTime : potentialFollower.currentSpeed;
            } else {
                m_status = CarStatus.FastStopping;
                currentSpeed = (currentSpeed > 0f) ? currentSpeed -= acceleration * 2f * Time.deltaTime : 0f;
            }
        }
        else if (farCollider.colliders.Count > 0) {
            Collider closest = farCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                m_status = CarStatus.Moving;
                currentSpeed = (currentSpeed < potentialFollower.currentSpeed) ? currentSpeed += acceleration * Time.deltaTime : (currentSpeed > potentialFollower.currentSpeed) ? currentSpeed -= acceleration * Time.deltaTime : potentialFollower.currentSpeed;
            } else {
                m_status = CarStatus.SlowlyStopping;
                currentSpeed = (currentSpeed > 0f) ? currentSpeed - acceleration * Time.deltaTime: 0f;
            }
        }
        else {
            m_status = CarStatus.Moving;
            currentSpeed = (currentSpeed < maxSpeed) ? currentSpeed + acceleration * Time.deltaTime : maxSpeed;
        }
    }
}
