using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    [SerializeField] private Vector3 m_estimatedVelocity = Vector3.zero;
    public Vector3 velocity {
        get { return m_estimatedVelocity; }
        set {}
    }
    private Vector3 prevPos = Vector3.zero;

    private void Start() {
        prevPos = transform.position;
    }

    private void LateUpdate() {
        m_estimatedVelocity = (transform.position - prevPos) / Time.deltaTime;
        prevPos = transform.position;
    }
}
