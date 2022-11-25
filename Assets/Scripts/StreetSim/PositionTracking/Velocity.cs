using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Velocity : MonoBehaviour
{
    public enum TrackingType {
        Manual,
        Estimated,
        Both
    }
    public TrackingType trackingType = TrackingType.Estimated;
    
    [SerializeField] private Vector3 m_estimatedVelocity = Vector3.zero;
    public Vector3 estimatedVelocity { get=>m_estimatedVelocity; set {} }
    public float estimatedSpeed => m_estimatedVelocity.magnitude;
    
    public Vector3 manualVelocity = Vector3.zero;
    public float manualSpeed = 0f;

    private Vector3 prevPos = Vector3.zero;
    private void Start() {
        prevPos = transform.position;
    }
    private void Update() {
        if (trackingType == TrackingType.Manual) return;
        m_estimatedVelocity = (transform.position - prevPos) / Time.deltaTime;
        prevPos = transform.position;
    }

    public Vector3 estimatedVelocityWithDirection(Vector3 direction) { 
        return Vector3.Scale(direction.normalized, m_estimatedVelocity); 
    }
    public float estimatedSpeedWithDirection(Vector3 direction) { 
        return estimatedVelocityWithDirection(direction).magnitude; 
    }
    public Vector3 manualVelocityWithDirection(Vector3 direction) { 
        return Vector3.Scale(direction.normalized, manualVelocity); 
    }
}
