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
    public ExperimentID id;
    public Transform frontOfCar, backOfCar;
    [SerializeField] private RemoteCollider frontCollider;
    public TrafficSignal trafficSignal;
    public Transform startTarget, middleTarget, endTarget;
    [SerializeField] private Collider[] gazeColliders;
    [SerializeField] private Velocity Velocity;

    [SerializeField] private float m_lengthOfCar = 0f;
    [SerializeField] private float maxSpeed = 0.5f;
    private float m_originalMaxSpeed;
    [SerializeField] private bool shouldStop = false;
    public StreetSimCarStatus status = StreetSimCarStatus.Idle;
    private float smoothTime;
    private float currentTime = 0f;

    [SerializeField] private float acceleration = 5f, deceleration = 7.5f;
    private float m_originalDeceleration;

    private Transform currentTarget;
    [SerializeField] private float currentSpeed = 0f;
    private Vector3 prevTargetPos;

    [SerializeField] private Transform[] wheels;
    [SerializeField] private AudioSource m_audioSource;

    private RaycastHit carRaycastHit;
    [SerializeField] private bool foundInFront = false;
    private bool m_hitMid = false;

    private Vector3 prevPos;
    public float speed = 0f;
    [SerializeField] private Vector3 positionDiff = Vector3.zero;
    [SerializeField] private Vector3 velocityDiff = Vector3.zero;
    [SerializeField] private float spaceMinimal = 0.5f, spaceOptimal;
    [SerializeField] private float accelerationMax = 10f, accelerationPref = 5f;
    [SerializeField] private float accelerationExpected = 0f;
    [SerializeField] private float speedTargeted = 10f;
    private float originalSpeedTargeted;
    [SerializeField] private float timePref = 1f;
    private float m_distanceTraveled = 0f;
    [SerializeField] private bool passedTraffic = false;

    private void Awake() {
        if (id == null) id = gameObject.GetComponent<ExperimentID>();
        m_lengthOfCar = GetComponent<BoxCollider>().size.z * transform.localScale.z;
        Velocity = GetComponent<Velocity>();
        m_originalDeceleration = deceleration;
    }

    public void Initialize() {
        //transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
        transform.position = startTarget.position;
        transform.rotation = startTarget.rotation;
        
        foreach(Collider col in gazeColliders) col.enabled = true;

        currentTarget = endTarget;
        prevPos = transform.position;
        prevTargetPos = endTarget.position;
        
        m_audioSource.enabled = true;

        m_hitMid = false;

        maxSpeed = UnityEngine.Random.Range(4f,15f);
        m_originalMaxSpeed = maxSpeed;

        speed = 0f;
        speedTargeted = maxSpeed;
        originalSpeedTargeted = speedTargeted;
        accelerationExpected = 0f;
        spaceMinimal = UnityEngine.Random.Range(0.25f,0.75f);
        timePref = UnityEngine.Random.Range(0.25f,0.75f);
        m_distanceTraveled = 0f;
        passedTraffic = false;

        status = StreetSimCarStatus.Active;
    }

    private void ReturnToIdle() {
        StreetSimCarManager.CM.SetCarToIdle(this);
        m_audioSource.enabled = false;
        foreach(Collider col in gazeColliders) col.enabled = false;
        speed = 0f;
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

    private void Update() {
        // Don't do anything if we're idle
        if (status == StreetSimCarStatus.Idle) return;
        // Check if there's a car in front of us.
        //  foundInFront = global variable : boolean
        //  out carRaycastHit = global variable : RaycastHit
        foundInFront = Physics.Raycast(frontOfCar.position,frontOfCar.forward, out carRaycastHit, 6f, StreetSimCarManager.CM.carDetectionLayerMask);
        //foundInFront = Physics.BoxCast(frontOfCar.position,Vector3.one,frontOfCar.forward,out carRaycastHit,frontOfCar.rotation,6f, StreetSimCarManager.CM.carDetectionLayerMask);
        // Calcualte position and velocity changes
        CalculateAcceleration();
        // Check how far we've moved
        CalculateDistanceFromStart();
        // After we do the calculation, we actually don't do anything else if we passed the midpoint
        if (m_hitMid) return;
        if (startTarget.position.x*transform.position.x<0f || Mathf.Abs(transform.position.x) <= 0.01f) {
            StreetSimCarManager.CM.AddCarMidToHistory(this,StreetSim.S.GetTimeFromStart(Time.time));
            m_hitMid = true;
        }
    }

    private void CalculateAcceleration() {
        passedTraffic = Vector3.Dot((middleTarget.position - frontOfCar.position).normalized, frontOfCar.forward) < 0f;

        float L = (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go)
            ? 1f
            : 0f;
        float O = (foundInFront)
            ? 1f
            : 0f;

        float mSpeed = originalSpeedTargeted + originalSpeedTargeted*0.5f*O;
        //float mSpeed = (foundInFront) ? originalSpeedTargeted : originalSpeedTargeted * 1.5f;
        positionDiff = (carRaycastHit.point-frontOfCar.position)*O + ((middleTarget.position-frontOfCar.position)*L + new Vector3(spaceOptimal+1f,0f,0f)*(1f-L))*(1f-O);
        /*
        positionDiff = (foundInFront) 
            ? carRaycastHit.point - frontOfCar.position 
            : (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go) 
                ? middleTarget.position - frontOfCar.position
                : new Vector3(spaceOptimal+1f,0f,0f);
        */
        // The bottom SHOULD be how we do this...
        // float speedDiff = (speed-carRaycastHit.transform.GetComponent<StreetSimCar>().speed)*O + (speed*L)*(1f-O);
        float speedDiff = (foundInFront) 
            ? speed - carRaycastHit.transform.GetComponent<StreetSimCar>().speed 
//            : (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go && (speed < 14f || (speed >= 14f && positionDiff.magnitude < spaceMinimal)))
            : (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go)
                ? speed
                : 0f;
        
        spaceOptimal = 
            (1f-(1f-L)*(1f-O))*(spaceMinimal + speed * timePref) 
            + 
            (speed*speedDiff)/(2*Mathf.Pow(accelerationMax*accelerationPref,0.5f));
        /*
        spaceOptimal = (foundInFront) 
            ? spaceMinimal + speed * timePref + ((speed*speedDiff)/(2*Mathf.Pow(accelerationMax*accelerationPref,0.5f))) 
//            : (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go && (speed < 14f || (speed >= 14f && positionDiff.magnitude < spaceMinimal))) 
            : (!passedTraffic && trafficSignal.status != TrafficSignal.TrafficSignalStatus.Go) 
                ? spaceMinimal + speed * timePref + ((speed*speedDiff)/(2*Mathf.Pow(accelerationMax*accelerationPref,0.5f)))
                : 0f;
        */
        accelerationExpected = accelerationMax * (
            1f - Mathf.Pow((speed/mSpeed),4f) 
            - Mathf.Pow((spaceOptimal/positionDiff.magnitude),2f)
        );
    }
    private void CalculateDistanceFromStart() {
        m_distanceTraveled = Vector3.Distance(transform.position,startTarget.position);
    }

    private void FixedUpdate() {
        // don't do anything if we're idle
        if (status == StreetSimCarStatus.Idle) return;

        // We end out of the loop if we've reached our target and that target happens to be the same position as the endtarget
        if (Vector3.Distance(transform.position,endTarget.position) <= 0.01f || m_distanceTraveled >= 150f) {
            ReturnToIdle();
            return;
        }

        // We also pause if any values are missing
        if (trafficSignal == null || middleTarget == null || endTarget == null) return;
        
        //UpdateSequence1();
        UpdateSequence2();
    }

    private void UpdateSequence2() {
        // We know current acceleration `accelerationExpected`
        // We convert that to speed, then to position
        speed += accelerationExpected * Time.fixedDeltaTime;
        transform.position = transform.position + transform.forward.normalized * speed * Time.fixedDeltaTime; 
        // Spin our wheels, if we have any
        if (wheels.Length > 0) {
            foreach(Transform wheel in wheels) wheel.Rotate(speed,0f,0f,Space.Self);
        }
    }

    private void UpdateSequence1() {
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
        maxSpeed = (foundInFront) ? m_originalMaxSpeed : Mathf.Clamp(m_originalMaxSpeed * 1.25f,0f,15f);
        deceleration = (trafficSignal.status == TrafficSignal.TrafficSignalStatus.Warning || trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop) 
            ? m_originalDeceleration * 5f
            : m_originalDeceleration;
        Vector3 positionToStopAt = (trafficSignal != null && trafficSignal.status == TrafficSignal.TrafficSignalStatus.Stop) 
            ? (foundInFront && HelperMethods.HasComponent<StreetSimCar>(carRaycastHit.transform, out potentialFrontCar) && potentialFrontCar.status == StreetSimCarStatus.Active) // Traffic light is WARNING or STOP
                ? GetPositionBeforeCar(potentialFrontCar)    // The car is still before the traffic point. Let's follow it.
                : (Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) < 0) // Nothing's in front of us
                    ? endTarget.position        // We're beyond the traffic point, so we keep going until the end target
                    : middleTarget.position     // We're still before the traffic point. So we stop in front of the light
            : (foundInFront && HelperMethods.HasComponent<StreetSimCar>(carRaycastHit.transform, out potentialFrontCar) && potentialFrontCar.status == StreetSimCarStatus.Active)  // Traffic light is GO
                ? GetPositionBeforeCar(potentialFrontCar)                                   // It's a car in front of us. Let's follow it
                : endTarget.position;   // We don't have something in front of us. Let's keep going until the end.

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

    public float GetCurrentSpeed() {
        return currentSpeed;
    }
}
