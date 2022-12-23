using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SignalFluctationTuple {
    public TrafficSignal signal;
    public bool shouldFluctuate;
}

[System.Serializable]
public class SignalSession {
    public string name;
    public SignalFluctationTuple[] goSignals, warningSignals, stopSignals;
    public GameObject[] obstacles;
    public float duration;

    public void TurnOn() {
        ToggleGoSignals(true);
        ToggleWarningSignals(true);
        ToggleStopSignals(true);
        ToggleObstacles(true);
    }
    public void TurnOff() {
        ToggleGoSignals(false);
        ToggleWarningSignals(false);
        ToggleStopSignals(false);
        ToggleObstacles(false);
    }

    public void ToggleGoSignals(bool isOn) {
        if (goSignals.Length == 0) return;
        foreach(SignalFluctationTuple signalTuple in goSignals) {
            signalTuple.signal.ToggleGoSignals(isOn, signalTuple.shouldFluctuate);
        }
    }
    public void ToggleWarningSignals(bool isOn) {
        if (warningSignals.Length == 0) return;
        foreach(SignalFluctationTuple signalTuple in warningSignals) {
            signalTuple.signal.ToggleWarningSignals(isOn, signalTuple.shouldFluctuate);
        }
    }
    public void ToggleStopSignals(bool isOn) {
        if (stopSignals.Length == 0) return;
        foreach(SignalFluctationTuple signalTuple in stopSignals) {
            signalTuple.signal.ToggleStopSignals(isOn, signalTuple.shouldFluctuate);
        }
    }
    public void ToggleObstacles(bool isOn) {
        if (obstacles.Length == 0) return;
        foreach(GameObject obstacle in obstacles) {
            obstacle.SetActive(isOn);
        }
    }
}


public class TrafficSignalController : MonoBehaviour
{
    public static TrafficSignalController current;
    public TrafficSignal[] walkSignals, carSignals;
    [SerializeField] private NavMeshObstacle crossingObstacle;
    [SerializeField] private List<SignalSession> sessions = new List<SignalSession>();
    private SignalSession currentSession = null;
    private IEnumerator cycleSession = null;

    [SerializeField] private Transform m_northCarDetector, m_southCarDetector, m_northCrossMidpoint, m_southCrossMidpoint, m_southCrossEndpoint, m_northCrossEndpoint;
    [SerializeField] private bool m_safeToCross = false; 
    [SerializeField] private LayerMask m_safetyRaycastTargets;

    public bool safeToCross {
        get { return m_safeToCross; }
        set {}
    }

    [SerializeField] private RemoteCollider m_carAtCrosswalkDetector;
    public RemoteCollider carAtCrosswalkDetector { get=>m_carAtCrosswalkDetector; set{} }

    private void Awake() {
        current = this;
    }

    private void Start() {
        cycleSession = CycleSignalSessions(0);
        StartCoroutine(cycleSession);
    }

    public bool GetSafety(bool onSouth, float agentSpeed = 0.4f, float timeOffset = 0f) {
        bool s = false, n = false;
        RaycastHit hitSouth, hitNorth;
        Velocity hitSouthVel = null, hitNorthVel = null;

        RaycastHit[] hitsSouth, hitsNorth;

        float distance = ((1.375f / agentSpeed) * 9f) + (timeOffset * agentSpeed * 9f);
        if (onSouth) {
            /*s = Physics.Raycast(m_southCrossEndpoint.position, m_southCrossEndpoint.forward, out hitSouth, distance*2f, m_safetyRaycastTargets);
            if (s) hitSouthVel = hitSouth.transform.GetComponent<Velocity>();
            // hitsNorth = Physics.RaycastAll(m_northCrossMidpoint.position + (m_northCrossMidpoint.forward * distance), m_northCrossMidpoint.forward, distance); 
            n = Physics.Raycast(m_northCrossEndpoint.position, m_northCrossEndpoint.forward, out hitNorth, distance*2f, m_safetyRaycastTargets);
            if (n) hitNorthVel = hitNorth.transform.GetComponent<Velocity>();
            */
            hitsSouth = Physics.RaycastAll(m_southCrossEndpoint.position, m_southCrossEndpoint.forward, distance*2f, m_safetyRaycastTargets);
            hitsNorth = Physics.RaycastAll(m_northCrossEndpoint.position, m_northCrossEndpoint.forward, distance*2f, m_safetyRaycastTargets);
        } else {
            /*
            // hitsSouth = Physics.RaycastAll(m_southCrossMidpoint.position + (m_southCrossMidpoint.forward * distance), m_southCrossMidpoint.forward, distance);
            s = Physics.Raycast(m_southCrossEndpoint.position, m_southCrossEndpoint.forward, out hitSouth, distance*2f, m_safetyRaycastTargets);
            if (s) hitSouthVel = hitSouth.transform.GetComponent<Velocity>();
            n = Physics.Raycast(m_northCrossEndpoint.position, m_northCrossEndpoint.forward, out hitNorth, distance*2f, m_safetyRaycastTargets);
            if (n) hitNorthVel = hitNorth.transform.GetComponent<Velocity>();
            */
            hitsSouth = Physics.RaycastAll(m_southCrossEndpoint.position, m_southCrossEndpoint.forward, distance*2f, m_safetyRaycastTargets);
            hitsNorth = Physics.RaycastAll(m_northCrossEndpoint.position, m_northCrossEndpoint.forward, distance*2f, m_safetyRaycastTargets);
        }
        /*
        return (
            (!s || (hitSouthVel != null && hitSouthVel.manualSpeed <= 10f))
            && 
            (!n || (hitNorthVel != null && hitNorthVel.manualSpeed <= 10f))
        );
        */
        return hitsSouth.Length == 0 && hitsNorth.Length == 0;
    }

    private IEnumerator CycleSignalSessions(int startIndex) {
        while(sessions.Count > 0) {
            for(int i = startIndex; i < sessions.Count; i++) {
                if (currentSession != null) currentSession.TurnOff();
                currentSession = sessions[i];
                currentSession.TurnOn();
                if (i == sessions.Count - 1) i = -1;
                yield return new WaitForSeconds(currentSession.duration);
            }
        }
    }

    public TrafficSignal GetFacingWalkingSignal(Vector3 dir, out float finalDiff) {
        float diff = 1f, curDiff = 1f;
        TrafficSignal closestSignal = null;
        foreach(TrafficSignal signal in walkSignals) {
            curDiff = Vector3.Dot(dir,signal.transform.forward);
            if (closestSignal == null || curDiff < diff) {
                closestSignal = signal;
                diff = curDiff;
            }
        }
        finalDiff = diff;
        return closestSignal;
    }

    public void StartAtSessionIndex(int index) {
        if (index >= sessions.Count) { Debug.Log("[TRAFFIC SIGNAL CONTROLLER] ERROR: Cannot start at an index that is nonexistent in our sessions"); return; }
        if (cycleSession != null) StopCoroutine(cycleSession);
        cycleSession = CycleSignalSessions(index);
        StartCoroutine(cycleSession);
    }

    public void SetDurationOfSession(int sessionIndex, float newDuration = 30f) {
        if (sessionIndex >= 0 && sessionIndex <= sessions.Count-1) {
            sessions[sessionIndex].duration = newDuration;
        }
    }
}
