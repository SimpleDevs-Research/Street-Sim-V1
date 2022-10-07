using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentID : MonoBehaviour
{
    [SerializeField] private ExperimentID m_parent;
    public ExperimentID parent {
        get { return m_parent; }
        set {}
    }
    [SerializeField] private List<ExperimentID> m_children = new List<ExperimentID>();
    public List<ExperimentID> children {
        get { return m_children; }
        set {}
    }
    [SerializeField] private string m_id;
    public string id {
        get { return m_id; }
        set {}
    }
    private bool idSet = false;

    private void Awake() {
        if (m_parent != null) {
            m_parent.AddChild(this);
        }
    }

    private void Start() {
        if (m_parent == null) Initialize();
    }

    public void Initialize() {
        if (ExperimentGlobalController.current != null) {
            idSet = ExperimentGlobalController.current.AddID(this, out m_id);
            if (m_children.Count > 0) {
                foreach(ExperimentID child in m_children) {
                    child.Initialize();
                }
            }
        }
    }

    public void AddChild(ExperimentID child) {
        if (!m_children.Contains(child)) m_children.Add(child);
    }
}
