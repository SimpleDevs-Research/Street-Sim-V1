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
    private bool idSet = false;

    private void Start() {
        if (ExperimentGlobalController.current != null) {
            idSet = ExperimentGlobalController.current.AddID(this, out m_id);
        }
    }
}
