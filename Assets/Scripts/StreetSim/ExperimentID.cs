using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentID : MonoBehaviour
{
    [SerializeField] private string m_id;
    public string id {
        get { return m_id; }
        set {}
    }

    private void Start() {
        if (ExperimentGlobalController.current != null) ExperimentGlobalController.current.AddID(this);
    }

    public void SetID(string newID) {
        m_id = newID;
    }
}
