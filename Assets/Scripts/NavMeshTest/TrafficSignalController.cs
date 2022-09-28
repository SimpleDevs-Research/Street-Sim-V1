using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SignalSession {
    public string name;
    public TrafficSignal[] goSignals, warningSignals, stopSignals;
    public NavMeshObstacle[] obstacles;
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
        foreach(TrafficSignal signal in goSignals) {
            signal.ToggleGoSignals(isOn);
        }
    }
    public void ToggleWarningSignals(bool isOn) {
        if (warningSignals.Length == 0) return;
        foreach(TrafficSignal signal in warningSignals) {
            signal.ToggleWarningSignals(isOn);
        }
    }
    public void ToggleStopSignals(bool isOn) {
        if (stopSignals.Length == 0) return;
        foreach(TrafficSignal signal in stopSignals) {
            signal.ToggleStopSignals(isOn);
        }
    }
    public void ToggleObstacles(bool isOn) {
        if (obstacles.Length == 0) return;
        foreach(NavMeshObstacle obstacle in obstacles) {
            obstacle.enabled = isOn;
        }
    }
}


public class TrafficSignalController : MonoBehaviour
{

    [SerializeField] private NavMeshObstacle crossingObstacle;
    [SerializeField] private List<SignalSession> sessions = new List<SignalSession>();
    private SignalSession currentSession = null;

    private void Start() {
        StartCoroutine(CycleSignalSessions());

    }

    private IEnumerator CycleSignalSessions() {
        while(sessions.Count > 0) {
            for(int i = 0; i < sessions.Count; i++) {
                if (currentSession != null) currentSession.TurnOff();
                currentSession = sessions[i];
                currentSession.TurnOn();
                if (i == sessions.Count - 1) i = -1;
                yield return new WaitForSeconds(currentSession.duration);
            }
        }
    }

    /*
    // Start is called before the first frame update
    void Awake() {
        foreach(WalkingSignal signal in walkingSignals) {
            signal.Initialize(this);
        }
        StartCoroutine(SignalCycle());
    }

    private IEnumerator SignalCycle() {
        while(true) {
            if (pedestriansCanWalk) {
                // This will allow pedestrians to walk across the road
                SetWalkingStatus(pedestriansCanWalk, m_walkTime);
                yield return new WaitForSeconds(walkTime);
            }
            else {
                // This forces pedestrians to wait until they can walk
                SetWalkingStatus(pedestriansCanWalk, m_vehicleTime);
                yield return new WaitForSeconds(vehicleTime);
            }
            pedestriansCanWalk = !pedestriansCanWalk;
        }
    }

    private void SetWalkingStatus(bool pedestriansCanCross, float t) {
        foreach(WalkingSignal signal in walkingSignals) {
            StartCoroutine(signal.SetStatus(pedestriansCanCross, t, m_warningTime));
        }
    }
    */
}
