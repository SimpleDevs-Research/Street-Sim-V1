using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Helpers;

public class StreetSimCar : MonoBehaviour
{

    [SerializeField] private PathCreator pathCreator;
    public Transform frontOfCar, backOfCar;
    [SerializeField] private RemoteCollider frontCollider;
    [SerializeField] private TrafficSignal trafficSignal;
    [SerializeField] private Transform startTarget, middleTarget, endTarget;

    [SerializeField] private float m_lengthOfCar = 0f;
    [SerializeField] private float maxSpeed = 0.5f;
    [SerializeField] private bool shouldStop = false;
    private float smoothTime;
    private float currentTime = 0f;

    [SerializeField] private float acceleration = 5f, deceleration = 7.5f;

    private Transform currentTarget;
    [SerializeField] private Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private float currentSpeed = 0f;
    private Vector3 prevPos;
    private Vector3 prevTargetPos;

    [SerializeField] private Transform[] wheels;

    private void Awake() {
        m_lengthOfCar = GetComponent<BoxCollider>().size.z * transform.localScale.z;
    }

    private void Start() {
        transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
        currentTarget = endTarget;
        prevPos = transform.position;
        prevTargetPos = endTarget.position;
    }

    private float CalculateDistanceUntilDeceleration() {
        return (currentSpeed*currentSpeed)/(2f*deceleration);
    }
    private Vector3 SuperSmoothLerp(Vector3 x0, Vector3 y0, Vector3 yt, float t, float k) {
        Vector3 f = x0 - y0 + (yt - y0) / (k * t);
        return yt - (yt - y0) / (k*t) + f * Mathf.Exp(-k*t);
    }

    private void FixedUpdate() {

        // We need to determine which target position to aim towards.
        // Just because the traffic light shines red that doesn't mean we should stop.
        // overall, we should prioritize if there's something in front of us first
        // CONDITION 1: is there something in front of us?
        //      IF SO, we stop at a reasonable distance
        //      IF NOT:...
        //  Assuming we don't see anything, we need to check if our traffic light is red/warning and if we're still behind `middleTarget`
        //  CONDITION 2: Traffic light red/warning && we're still in front of the traffic light
        //      IF SO, we stop at `middleTarget`
        //      IF NOT...
        //  At this point, there's nothing stopping us. Just keep going!
        //  IF NOT... we keep going to `endTarget`.

        StreetSimCar potentialFrontCar = null;
        Vector3 positionToStopAt = (frontCollider.numColliders > 0)
            ? (HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar))
                ? potentialFrontCar.backOfCar.position + (-potentialFrontCar.transform.forward.normalized * 0.5f) + (-potentialFrontCar.transform.forward.normalized * 0.5f * m_lengthOfCar)
                : frontCollider.GetClosestCollider().transform.position + (-transform.forward.normalized * 0.5f) + (-transform.forward.normalized * 0.5f * m_lengthOfCar)
            : (trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop || trafficSignal.status == TrafficSignal.TrafficSignalStatus.Warning) 
                ? (Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) < 0)
                    ? endTarget.position 
                    : middleTarget.position
                : endTarget.position;

        /*
        StreetSimCar potentialFrontCar = null;
        bool objectButNotCarFound = (frontCollider.numColliders > 0) 
            ? !HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject, out potentialFrontCar)
            : false;
        shouldStop = (
            objectButNotCarFound ||
            trafficSignal.status == TrafficSignal.TrafficSignalStatus.Warning || 
            trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop
        );

        Debug.Log(potentialFrontCar);
        
        Vector3 positionToStopAt = (potentialFrontCar != null)
            ? potentialFrontCar.backOfCar.position + (-potentialFrontCar.transform.forward.normalized * 0.5f) + (-potentialFrontCar.transform.forward.normalized * 0.5f * m_lengthOfCar)
            : (shouldStop)
                ? (Vector3.Distance(transform.position,middleTarget.position) <= 0.01f) 
                    ? middleTarget.position
                    : (frontCollider.numColliders > 0)
                        ? frontCollider.GetClosestCollider().transform.position + (-transform.forward.normalized * 0.5f) + (-transform.forward.normalized * 0.5f * m_lengthOfCar)
                        : endTarget.position
                : endTarget.position;
        */
        /*            
        currentTarget = (shouldStop && Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) >= 0)
            ? middleTarget
            : endTarget;
        */

        float distToDecelerate = CalculateDistanceUntilDeceleration();
        // float distBetweenTargets = Vector3.Distance(startTarget.position,currentTarget.position);
        float distBetweenTargets = Vector3.Distance(startTarget.position,positionToStopAt);
        // float distToTarget = Vector3.Distance(transform.position,currentTarget.position);
        float distToTarget = Vector3.Distance(transform.position,positionToStopAt);

        // update speed based on acceleration
        currentSpeed = (distToTarget <= distToDecelerate) 
            ? currentSpeed - deceleration * Time.fixedDeltaTime
            : currentSpeed + acceleration * Time.fixedDeltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed,0f,maxSpeed);

        // Calculate the distance covered based on speed;
        float distCovered = (transform.position - startTarget.position).magnitude + (currentSpeed * Time.fixedDeltaTime);
        // Fraction of journey completed equals current distance divided by total distance.
        float fractionOfJourney = distCovered / distBetweenTargets;
        // Set our position as a fraction of the distance between the markers.
        // transform.position = Vector3.Lerp(startTarget.position, currentTarget.position, fractionOfJourney);
        transform.position = Vector3.Lerp(startTarget.position, positionToStopAt, fractionOfJourney);

        // Spin our wheels, if we have any
        if (wheels.Length > 0) {
            foreach(Transform wheel in wheels) {
                wheel.Rotate(currentSpeed,0f,0f,Space.Self);
            }
        }
    }
}
