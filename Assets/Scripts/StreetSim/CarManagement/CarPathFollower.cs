using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Helpers;

public class CarPathFollower : MonoBehaviour
{
    public enum CarStatus {
        Moving,
        Following,
        Stop
    }

    public PathCreator pathCreator;
    public float maxSpeed = 5f;
    public float currentSpeed = 0f;
    public float acceleration = 2.5f;
    public float deceleration = 5f;
    private float distanceTraveled;

    [SerializeField] private CarStatus m_status = CarStatus.Moving;

    [SerializeField] private RemoteCollider closeCollider;
    [SerializeField] private RemoteCollider farCollider;
    [SerializeField] private Transform frontOfCar, backOfCar;
    [SerializeField] private AudioSource movingAudioSource, idleAudioSource;

    [SerializeField] private Transform[] wheels;
    private Vector3 prevPosition;

    private void Start() {
        if (pathCreator != null) {
            transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
            prevPosition = transform.position - transform.forward;
            distanceTraveled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }
    }
    private void FixedUpdate() {
        if (pathCreator == null) return;
        UpdateSpeed();
        movingAudioSource.volume = currentSpeed / maxSpeed;
        //idleAudioSource.volume = 1 - movingAudioSource.volume;
        distanceTraveled += currentSpeed * Time.deltaTime;
        transform.position = pathCreator.path.GetPointAtDistance(distanceTraveled);
        if(Vector3.Distance(transform.position,prevPosition)>0.05f) transform.rotation = Quaternion.LookRotation(transform.position - prevPosition, Vector3.up);
        prevPosition = transform.position;
        if (wheels.Length > 0) {
            foreach(Transform wheel in wheels) {
                wheel.Rotate(currentSpeed,0f,0f,Space.Self);
            }
        }
        //transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTraveled);
    }
    private void UpdateSpeed() {
        // Speed works like this:
        // At a neutral state, the car will attempt to accelerate until it matches `maxSpeed`
        // The car's max speed is either:
        //      - `maxSpeed` (if no other followers in front of it),
        //      - the current speed of the follower in front of it, if there is a follower in front of it, or
        //      - 0, if there's anything else in front of it.
        // The car must accelerate at the rate stated at in `acceleration` and deccelerate at the rate of `decceleration`.
        // Therefore, we must check:
        //      - If there's nothing in front of the car, the car can `accelerate` up onto a max speed of `maxSpeed`
        //      - If there's a follower in front of the car, there are some conditions:
        //          1. if the car in front is a certain distance away, we can actually continue to accelerate
        //          2. If the car is too close, we adjust max speed to match the car in front
        //      - If there's something in front, better decelerate to a max speed of `0`

        float currentMaxSpeed = maxSpeed;
        if (closeCollider.colliders.Count > 0) {
            // We're gonna hit... something
            Collider closest = closeCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                // It's another car. We'll adjust the current max speed based on distance to the car
                if (Vector3.Distance(frontOfCar.position,potentialFollower.backOfCar.position) < 2f) {
                    currentMaxSpeed = 0f;
                } else {
                    currentMaxSpeed = potentialFollower.currentSpeed;
                }
            } else {
                // We only stop if they're less than 2  meters in front of us
                if (Vector3.Distance(frontOfCar.position,closest.transform.position) < 2f) {
                    // OH FUDGE, WE GOTTA STOP
                    currentMaxSpeed = 0f;
                }
            }
        }
        else if (farCollider.colliders.Count > 0) {
            // We're gonna hit... something
            Collider closest = farCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                // It's another car. We'll adjust the current max speed based on distance to the car
                if (Vector3.Distance(frontOfCar.position,potentialFollower.backOfCar.position) < 2f) {
                    currentMaxSpeed = 0f;
                } else {
                    currentMaxSpeed = potentialFollower.currentSpeed;
                }
            }
            else {
                // We only stop if they're less than 2  meters in front of us
                if (Vector3.Distance(frontOfCar.position,closest.transform.position) < 1f) {
                    // OH FUDGE, WE GOTTA STOP
                    currentMaxSpeed = 0f;
                }
            }
        }

        currentSpeed = (currentSpeed < currentMaxSpeed)
            ? currentSpeed + acceleration * Time.deltaTime 
            : (currentSpeed > currentMaxSpeed) 
                ? currentSpeed -= deceleration * Time.deltaTime
                : currentMaxSpeed;
        currentSpeed = Mathf.Clamp(currentSpeed,0f,currentMaxSpeed);

        /*
        if (closeCollider.colliders.Count > 0) {
            Collider closest = closeCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                m_status = CarStatus.Moving;
                currentSpeed = (Vector3.Distance(transform.position,potentialFollower.transform.position) > 1f) 
                    ? (currentSpeed < maxSpeed) 
                        ? currentSpeed + acceleration * Time.deltaTime 
                        : maxSpeed
                    : (currentSpeed < potentialFollower.currentSpeed) 
                        ? currentSpeed += acceleration * Time.deltaTime 
                        : (currentSpeed > potentialFollower.currentSpeed) 
                            ? currentSpeed -= deceleration * Time.deltaTime 
                            : potentialFollower.currentSpeed;
            } else {
                m_status = CarStatus.FastStopping;
                currentSpeed = (currentSpeed > 0f) ? currentSpeed -= deceleration * 2f * Time.deltaTime : 0f;
            }
        }
        else if (farCollider.colliders.Count > 0) {
            Collider closest = farCollider.GetClosestCollider();
            CarPathFollower potentialFollower;
            if (HelperMethods.HasComponent<CarPathFollower>(closest.gameObject, out potentialFollower)) {
                m_status = CarStatus.Moving;
                currentSpeed = (Vector3.Distance(transform.position,potentialFollower.transform.position) > 1f) 
                    ? (currentSpeed < maxSpeed) 
                        ? currentSpeed + acceleration * Time.deltaTime 
                        : maxSpeed
                    : (currentSpeed < potentialFollower.currentSpeed) 
                        ? currentSpeed += acceleration * Time.deltaTime 
                        : (currentSpeed > potentialFollower.currentSpeed) 
                            ? currentSpeed -= deceleration * Time.deltaTime 
                            : potentialFollower.currentSpeed;
            } else {
                m_status = CarStatus.SlowlyStopping;
                currentSpeed = (currentSpeed > 0f) ? currentSpeed - deceleration * Time.deltaTime: 0f;
            }
        }
        else {
            m_status = CarStatus.Moving;
            currentSpeed = (currentSpeed < maxSpeed) ? currentSpeed + acceleration * Time.deltaTime : maxSpeed;
        }

        currentSpeed = Mathf.Clamp(currentSpeed,0f,Mathf.Infinity);
        */
    }
}
