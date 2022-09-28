using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Agent : MonoBehaviour
{
    [SerializeField] public int index;
    [SerializeField] private FlowFieldManager flowFieldManager;
    [SerializeField] private AgentController agentController; 
    [SerializeField] private CellController cellController;
    [SerializeField] 
    private float m_radius = 0.25f;
    public float radius {
        get { return m_radius; }
        set {}
    }
    [SerializeField] private List<Vector2Int> cellIndices = new List<Vector2Int>();

    [SerializeField] private Rigidbody rb;
    private bool initialized = false;

#if UNITY_EDITOR
    private void OnGUI() {
        Handles.DrawSolidDisc(transform.position, Vector3.up, 0.01f);
    }
#endif

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public bool Initialize(int index, FlowFieldManager flowFieldManager, AgentController agentController, CellController cellController) {
        this.index = index;
        this.flowFieldManager = flowFieldManager;
        this.agentController = agentController;
        this.cellController = cellController;
        initialized = true;
        return true;
    }

    private void Update() {
        if (!initialized) return;
        cellController.AgentToCellUpdate(this);
        if (rb == null) return;
        Vector2Int gridPos = cellController.Position3DToIndex(transform.position);
        if (!cellController.ValidateCoords(gridPos)) return;
        if (flowFieldManager.currentFlowField == null) return;
        if (flowFieldManager.currentFlowField.flowFieldGrid == null) return;
        rb.AddForce(flowFieldManager.currentFlowField.flowFieldGrid[gridPos.x,gridPos.y]);

    }
    
    public void SetCellIndices(List<Vector2Int> indices) {
        cellIndices = indices;
    }

    public bool CheckIfCloseToArea(Vector2Int areaIndex) {
        return cellIndices.Contains(areaIndex);
    }
}
