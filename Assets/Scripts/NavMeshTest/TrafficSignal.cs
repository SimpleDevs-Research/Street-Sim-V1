using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSignal : MonoBehaviour
{

    [SerializeField] private Renderer[] goSignals, warningSignals, stopSignals;
    private IEnumerator currentFluctuator = null;
    
    public void ToggleGoSignals(bool shouldBeOn) {
        foreach(Renderer r in goSignals) r.enabled = shouldBeOn;
    } 
    public void ToggleWarningSignals(bool shouldBeOn) {
        foreach(Renderer r in warningSignals) r.enabled = shouldBeOn;
        if (shouldBeOn) {
            if (currentFluctuator == null) {
                currentFluctuator = FluctuateWarningSignals();
                StartCoroutine(currentFluctuator);
            }
        } else {
            if (currentFluctuator != null) {
                StopCoroutine(currentFluctuator);
                currentFluctuator = null;
            }
        }
    }
    public void ToggleStopSignals(bool shouldBeOn) {
        foreach(Renderer r in stopSignals) r.enabled = shouldBeOn;
    }

    private IEnumerator FluctuateWarningSignals() {
        while(true) {
            yield return new WaitForSeconds(0.5f);
            foreach(Renderer r in warningSignals) r.enabled = !r.enabled;
        }
    }


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
