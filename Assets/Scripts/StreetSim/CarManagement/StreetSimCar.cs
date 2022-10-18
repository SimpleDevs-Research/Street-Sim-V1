using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Helpers;

public class StreetSimCar : MonoBehaviour
{
    public enum StreetSimCarStatus {
        Idle,
        Active,
    }
    public Transform frontOfCar, backOfCar;
    [SerializeField] private RemoteCollider frontCollider;
    public TrafficSignal trafficSignal;
    public Transform startTarget, middleTarget, endTarget;
    [SerializeField] private Collider[] gazeColliders;

    [SerializeField] private float m_lengthOfCar = 0f;
    [SerializeField] private float maxSpeed = 0.5f;
    [SerializeField] private bool shouldStop = false;
    [SerializeField] private StreetSimCarStatus m_status = StreetSimCarStatus.Idle;
    public StreetSimCarStatus status { get { return m_status; } set{} }
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

    public void Initialize() {
        //transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
        transform.position = startTarget.position;
        transform.rotation = startTarget.rotation;

        currentTarget = endTarget;
        prevPos = transform.position;
        prevTargetPos = endTarget.position;
        
        m_status = StreetSimCarStatus.Active;
    }

    private void ReturnToIdle() {
        Debug.Log("RETURNING TO IDLE");
        m_status = StreetSimCarStatus.Idle;
        StreetSimCarManager.CM.SetCarToIdle(this);
    }

    private float CalculateDistanceUntilDeceleration() {
        return (currentSpeed*currentSpeed)/(2f*deceleration);
    }
    private Vector3 SuperSmoothLerp(Vector3 x0, Vector3 y0, Vector3 yt, float t, float k) {
        Vector3 f = x0 - y0 + (yt - y0) / (k * t);
        return yt - (yt - y0) / (k*t) + f * Mathf.Exp(-k*t);
    }

    private Vector3 GetPositionBeforeObject(Transform obstacle) {
        // Z and Y axis positions must be consistent
        Vector3 pos = obstacle.position + (-transform.forward.normalized * 0.5f) + (-transform.forward.normalized * 0.5f * m_lengthOfCar);
        return new Vector3(
            pos.x,
            transform.position.y,
            transform.position.z
        );
    }
    private Vector3 GetPositionBeforeCar(StreetSimCar otherCar) {
        return otherCar.backOfCar.position + (-otherCar.transform.forward.normalized * 0.5f) + (-otherCar.transform.forward.normalized * 0.5f * m_lengthOfCar);
    }

    private void FixedUpdate() {

        // Make sure our collider is off if we're idle
        if (m_status == StreetSimCarStatus.Idle) {
            foreach(Collider col in gazeColliders) {
                col.enabled = false;
            }
            return;
        }

        // We end out of the loop if we've reached our target and that target happens to be the same position as the endtarget
        if (Vector3.Distance(transform.position,endTarget.position) <= 0.01f) {
            ReturnToIdle();
            return;
        }

        // We also pause if any values are missing
        if (trafficSignal == null || middleTarget == null || endTarget == null) return;

        foreach(Collider col in gazeColliders) {
            col.enabled = true;
        }
        
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

        StreetSimCar potentialFrontCar;
        Vector3 positionToStopAt = (trafficSignal != null && trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop) 
            ? (frontCollider.numColliders > 0 && HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar) && potentialFrontCar.status == StreetSimCarStatus.Active) // Traffic light is WARNING or STOP
                ? GetPositionBeforeCar(potentialFrontCar)    // The car is still before the traffic point. Let's follow it.
                : (Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) < 0) // Nothing's in front of us
                    ? endTarget.position        // We're beyond the traffic point, so we keep going until the end target
                    : middleTarget.position     // We're still before the traffic point. So we stop in front of the light
            : (frontCollider.numColliders > 0 && HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar) && potentialFrontCar.status == StreetSimCarStatus.Active)  // Traffic light is GO
                ? GetPositionBeforeCar(potentialFrontCar)                                   // It's a car in front of us. Let's follow it
                : endTarget.position;   // We don't have something in front of us. Let's keep going until the end.
        
        /*
        Vector3 positionToStopAt =  (trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop || trafficSignal.status == TrafficSignal.TrafficSignalStatus.Warning) 
            ? (frontCollider.numColliders > 0) // Traffic light is WARNING or STOP
                ? (HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar))   // Something's in front of us
                    ? (Vector3.Dot(potentialFrontCar.transform.forward,(middleTarget.position-potentialFrontCar.transform.position)) < 0)     // It's a car in front of us
                        ? middleTarget.position                      // the car in front is beyond the traffic point. We stop at the middle target
                        : GetPositionBeforeCar(potentialFrontCar)    // The car is still before the traffic point. Let's follow it.
                    : (Mathf.Abs(transform.position.x - middleTarget.position.x) > Mathf.Abs(frontOfCar.position.x - frontCollider.GetClosestCollider().transform.position.x))   // It's not a car in front of us.
                        ? GetPositionBeforeObject(frontCollider.GetClosestCollider().transform)     // Obstacle is closer to us. We stop before it.
                        : middleTarget.position // The traffic stop point is closer. We prioritize that.
                : (Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) < 0) // Nothing's in front of us
                    ? endTarget.position        // We're beyond the traffic point, so we keep going until the end target
                    : middleTarget.position     // We're still before the traffic point. So we stop in front of the light
            : (frontCollider.numColliders > 0)  // Traffic light is GO
                ? (HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar))   // We have something in front of us
                    ? GetPositionBeforeCar(potentialFrontCar)                                   // It's a car in front of us. Let's follow it
                    : GetPositionBeforeObject(frontCollider.GetClosestCollider().transform)     // It's not a car in front of us. We have to stop before we hit it
                : endTarget.position;   // We don't have something in front of us. Let's keep going until the end.
        */
        
        /*
        (frontCollider.numColliders > 0) // do we have anything in front of us?
            ? (HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar)) // Yes, we do. But is it a car?
                ? GetPositionBeforeCar(potentialFrontCar)   // It is a car, so we just tandem follow it.
                : GetPositionBeforeObject(frontCollider.GetClosestCollider().transform) // No it isn't. So we stop before we hit it
            : endTarget.position;
        */
        /*
        (frontCollider.numColliders > 0)
            ? (HelperMethods.HasComponent<StreetSimCar>(frontCollider.GetClosestCollider().gameObject,out potentialFrontCar))
                ? potentialFrontCar.backOfCar.position + (-potentialFrontCar.transform.forward.normalized * 0.5f) + (-potentialFrontCar.transform.forward.normalized * 0.5f * m_lengthOfCar)
                : frontCollider.GetClosestCollider().transform.position + (-transform.forward.normalized * 0.5f) + (-transform.forward.normalized * 0.5f * m_lengthOfCar)
            : (trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop || trafficSignal.status == TrafficSignal.TrafficSignalStatus.Warning) 
                ? (Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) < 0)
                    ? endTarget.position 
                    : middleTarget.position
                : endTarget.position;
        */

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
