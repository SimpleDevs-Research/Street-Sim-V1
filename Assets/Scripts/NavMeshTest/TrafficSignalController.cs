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

    private void Awake() {
        current = this;
    }

    private void Start() {
        cycleSession = CycleSignalSessions(0);
        StartCoroutine(cycleSession);
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
}
