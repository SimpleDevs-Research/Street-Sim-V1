using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class SmoothDampBackAndForth : MonoBehaviour
{
    public PathCreator pathCreator;
    public enum MovingTowards {
        Start,
        End
    }
    public MovingTowards movingTo;
    [SerializeField] private Transform startTarget, middleTarget, endTarget;
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

    private void Start() {
        transform.position = pathCreator.path.GetClosestPointOnPath(transform.position);
        currentTarget = endTarget;
        movingTo = MovingTowards.End;
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
        if (shouldStop && Vector3.Dot(transform.forward,(middleTarget.position-transform.position)) >= 0) {
            currentTarget = middleTarget;
        } else {
            currentTarget = (movingTo == MovingTowards.End) ? endTarget : startTarget;
        }

        float distToDecelerate = CalculateDistanceUntilDeceleration();
        float distBetweenTargets = Vector3.Distance(startTarget.position,currentTarget.position);
        float distToTarget = Vector3.Distance(transform.position,currentTarget.position);

        // update speed based on acceleration
        currentSpeed = (distToTarget <= distToDecelerate) 
            ? currentSpeed - deceleration * Time.fixedDeltaTime
            : currentSpeed + acceleration * Time.fixedDeltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed,0f,maxSpeed);
        Debug.Log(currentSpeed);

        // Calculate the distance covered based on speed;
        float distCovered = (transform.position - startTarget.position).magnitude + (currentSpeed * Time.fixedDeltaTime);
        // Fraction of journey completed equals current distance divided by total distance.
        float fractionOfJourney = distCovered / distBetweenTargets;
        // Set our position as a fraction of the distance between the markers.
        transform.position = Vector3.Lerp(startTarget.position, currentTarget.position, fractionOfJourney);


        //currentSpeed += acceleration * Time.fixedDeltaTime;
        //currentSpeed = Mathf.Clamp(currentSpeed,0f,maxSpeed);
        //transform.position = SuperSmoothLerp(transform.position,prevTargetPos,currentTarget.position,Time.fixedDeltaTime*maxSpeed,20f);
        //prevTargetPos = currentTarget.position;
        //transform.position = Vector3.Lerp(transform.position,currentTarget.position,Time.fixedDeltaTime * currentSpeed);
        
        //float timeToTarget = distToTarget/maxSpeed;
        /*
        if (distToTarget <= 0.01f || (currentTarget == endTarget && Vector3.Dot(transform.forward,(currentTarget.position - transform.position)) <= 0)) {
            transform.position = currentTarget.position;
            //currentVelocity = Vector3.zero;
            currentSpeed = 0f;
            movingTo = (movingTo == MovingTowards.End) ? MovingTowards.Start : MovingTowards.End;
            return;
        }
        if (distToTarget <= distToDecelerate) {
            currentSpeed -= deceleration * Time.fixedDeltaTime;
        } else {
            currentSpeed += acceleration * Time.fixedDeltaTime;
        }
        currentSpeed = Mathf.Clamp(currentSpeed,0f,maxSpeed);
        transform.position += transform.forward.normalized * currentSpeed * Time.fixedDeltaTime;
        prevPos = transform.position;
        */
        
        //transform.position = Vector3.SmoothDamp(transform.position,currentTarget.position,ref currentVelocity,timeToTarget);

        /*
        float timeToTarget = distToTarget / currentVelocity.magnitude;
        currentTime += Time.fixedDeltaTime;
        transform.position = Vector3.SmoothDamp(transform.position,currentTarget.position,);
        if (distToTarget <= 0.01f || Vector3.Dot(transform.forward,(currentTarget.position - transform.position)) <= 0) {
            transform.position = currentTarget.position;
            currentVelocity = 0f;
            currentTime = 0f;
            movingTo = (movingTo == MovingTowards.End) ? MovingTowards.Start : MovingTowards.End;
            return;
        }
        */
    
        
        //transform.position = Vector3.Lerp(transform.position,currentTarget.position,timeToTarget);

        /*
        if (Vector3.Distance(transform.position,currentTarget.position) > 0.01f) {
            // We haven't reached our target yet, so we need to either move or stop, depending.
            if (shouldStop && Vector3.Dot(currentVelocity,(middleTarget.position-transform.position))>0) {
                currentTarget = middleTarget;
            } else {
                currentTarget = endTarget;
            }
            GetSmoothTime();
            transform.position = Vector3.SmoothDamp(transform.position, currentTarget.position, ref currentVelocity, smoothTime);
            currentTarget = 
                ? (shouldStop && Vector3.Dot(transform.forward,(middleTarget.position - transform.position))==1f)
                    ? middleTarget
                    : 
                : (currentTarget == target1) 
                    ? target2 
                    : target1;
        }
        */
        /*
        // Smoothly move the camera towards that target position
        if (shouldStop) {
            velocity = Vector3.zero;
            if (Vector3.Distance(transform.position,prevPos) > 0.01f) transform.position = Vector3.SmoothDamp(transform.position, currentTarget.position, ref velocity, smoothTime);
            //currentSmoothTime = Mathf.Infinity;
        } else {
            transform.position = Vector3.SmoothDamp(transform.position, currentTarget.position, ref velocity, smoothTime);
        }
        */
    }

    /*
    private void GetSmoothTime() {
        float dist = Vector3.Distance(startTarget.position,currentTarget.position);
        smoothTime = dist / speed;
    }
    */
}
