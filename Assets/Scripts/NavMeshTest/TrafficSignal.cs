using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SignalLightTuple {
    public Renderer renderer;
    public Light light;
}

public class TrafficSignal : MonoBehaviour
{
    public enum TrafficSignalStatus {
        Go,
        Stop,
        Warning
    }
    [SerializeField] private SignalLightTuple[] goSignals, warningSignals, stopSignals;
    private IEnumerator currentFluctuator = null;
    [SerializeField] private TrafficSignalStatus m_status = TrafficSignalStatus.Stop;
    public TrafficSignalStatus status {
        get { return m_status; }
        set {}
    }
    
    public void ToggleGoSignals(bool shouldBeOn, bool shouldFluctuate) {
        foreach(SignalLightTuple r in goSignals) {
            r.renderer.enabled = shouldBeOn;
            r.light.enabled = shouldBeOn;
        }
        if (shouldBeOn) m_status = TrafficSignalStatus.Go;
        if (currentFluctuator != null) {
            StopCoroutine(currentFluctuator);
            currentFluctuator = null;
        }
        if (shouldFluctuate) {
            currentFluctuator = FluctuateSignals(goSignals);
            StartCoroutine(currentFluctuator);
        }
    } 
    public void ToggleWarningSignals(bool shouldBeOn, bool shouldFluctuate) {
        foreach(SignalLightTuple r in warningSignals) {
            r.renderer.enabled = shouldBeOn;
            r.light.enabled = shouldBeOn;
        }
        if (shouldBeOn) m_status = TrafficSignalStatus.Warning;
        if (currentFluctuator != null) {
            StopCoroutine(currentFluctuator);
            currentFluctuator = null;
        }
        if (shouldFluctuate) {
            currentFluctuator = FluctuateSignals(warningSignals);
            StartCoroutine(currentFluctuator);
        }
    }
    public void ToggleStopSignals(bool shouldBeOn, bool shouldFluctuate) {
        foreach(SignalLightTuple r in stopSignals) {
            r.renderer.enabled = shouldBeOn;
            r.light.enabled = shouldBeOn;
        }
        if (shouldBeOn) m_status = TrafficSignalStatus.Stop;
        if (currentFluctuator != null) {
            StopCoroutine(currentFluctuator);
            currentFluctuator = null;
        }
        if (shouldFluctuate) {
            currentFluctuator = FluctuateSignals(stopSignals);
            StartCoroutine(currentFluctuator);
        }
    }

    private IEnumerator FluctuateSignals(SignalLightTuple[] signals) {
        while(true) {
            yield return new WaitForSeconds(0.5f);
            foreach(SignalLightTuple r in signals) {
                r.renderer.enabled = !r.renderer.enabled;
                r.light.enabled = !r.light.enabled;
            }
        }
    }

    /*
    private IEnumerator FluctuateWarningSignals() {
        while(true) {
            yield return new WaitForSeconds(0.5f);
            foreach(Renderer r in warningSignals) r.enabled = !r.enabled;
        }
    }
    */


    /*
    public void Initialize(WalkingSignalController controller) {
        this.controller = controller;
    }
    */

    /*

    public IEnumerator SetSignal(bool agentsCanGo, float t, float w) {
        float maxTime;
        if (currentSignal != null) StopCoroutine(currentSignal);
        if (agentsCanGo) {
            // Set light to blue, and then when the timing is set to the warning signal, then start flashing red
            currentSignal = SetGoSignal();
            StartCoroutine(currentSignal);
            yield return new WaitForSeconds(t-w);
        }
        else {
            // Set light to red


        }
    }

    public IEnumerator SetGoSignal() {
        goSignal?.enabled = true;
        warningSignal?.enabled = false;
        stopSignal?.enabled = false;
        yield return null;
    }
    public IEnumerator SetWarningSignal() {
        goSignal?.enabled = false;
        stopSignal?.enabled = false;
        while(warningSignal != null) {
            warningSignal.enabled = !warningSignal.enabled;
            yield return new WaitForSeconds(0.75f);
        }
    }
    public IEnumerator SetStopSignal() {
        goSignal?.enabled = false;
        warningSignal?.enabled = false;
        stopSignal?.enabled = true;
    }
    */
}
