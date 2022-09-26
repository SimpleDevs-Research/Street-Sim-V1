using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private AgentController agentController; 
    [SerializeField] private CellController cellController;
    [SerializeField] 
    private float m_radius = 0.25f;
    public float radius {
        get { return m_radius; }
        set {}
    }
    [SerializeField] private List<Vector2Int> cellIndices = new List<Vector2Int>();

    public bool Initialize(int index, AgentController agentController, CellController cellController) {
        this.index = index;
        this.agentController = agentController;
        this.cellController = cellController;
        return true;
    }

    private void Update() {
        cellController.AgentToCellUpdate(this);
    }
    
    public void SetCellIndices(List<Vector2Int> indices) {
        cellIndices = indices;
    }

    public bool CheckIfCloseToArea(Vector2Int areaIndex) {
        return cellIndices.Contains(areaIndex);
    }
}
