using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Helpers;

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
    [SerializeField] private string m_id = "";
    public string id {
        get { return m_id; }
        set {}
    }
    [SerializeField] private string m_ref_id = "";
    public string ref_id {
        get { return (m_ref_id.Length > 0) ? m_ref_id : m_id; }
        set {}
    }
    private bool idSet = false;

    [SerializeField] private bool m_shouldTrack = true;
    public bool shouldTrack { get=>m_shouldTrack; set{} }

    private void Awake() {
        if (m_parent != null) {
            m_parent.AddChild(this);
        }
    }

    private UnityEvent targetsToNotify = new UnityEvent();
    public delegate void MyTargetDelegate();
    public MyTargetDelegate onConfirmedID;

    private void Start() {
        Initialize();
    }

    public void Initialize() {
        idSet = StreetSimIDController.ID.AddID(this, out m_id);
        if (idSet && m_children.Count > 0) {
            StreetSimIDController.ID.AddChildren(this);
        }
        /*
        if (ExperimentGlobalController.current != null) {
            idSet = ExperimentGlobalController.current.AddID(this, out m_id);
            if (m_children.Count > 0) {
                foreach(ExperimentID child in m_children) {
                    child.Initialize();
                }
            }
            onConfirmedID?.Invoke();
        }
        */
    }

    public void AddChild(ExperimentID child) {
        if (!m_children.Contains(child)) m_children.Add(child);
    }

    public bool GetChildrenOfType<T>(out List<T> output) {
        output = new List<T>();
        if (m_children.Count == 0) return false;
        foreach(ExperimentID child in m_children) {
            T potential = default(T);
            if (HelperMethods.HasComponent<T>(child.gameObject, out potential)) {
                output.Add(potential);
            }
        }
        return output.Count == 0;
    }

    public void SetID(string newID) {
        m_id = newID;
    }
    public void SetRefID(string newRefID) {
        m_ref_id = newRefID;
    }
    public void SetParent(ExperimentID newParent) {
        m_parent = newParent;
    }
}
