using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CellNeighbor {
    public float distance;
    public Vector3 directionTo;
    public CellNeighbor(float distance, Vector3 directionTo) {
        this.distance = distance;
        this.directionTo = directionTo;
    }
}

public class Cell {

    [Header("References")]
    //      [SerializeField] private Canvas canvas;
    //      [SerializeField] TextMeshProUGUI textbox;
    // [SerializeField] private Renderer r;

    public Vector2Int index = Vector2Int.zero;
    private CellController cellController;
    private Vector3 m_worldPos = Vector3.zero;
    public Vector3 worldPos {
        get { return m_worldPos; }
        set {}
    }

    public enum CellType {
        Traversable,
        Rough,
        Impassable,
    }
    private CellType m_cellType = CellType.Traversable;
    public CellType cellType {
        get { return m_cellType; }
        set {}
    }
    private Dictionary<CellType, float> costDictionary = new Dictionary<CellType, float>() {
        {CellType.Traversable, 1f},
        {CellType.Rough, 10f},
        {CellType.Impassable, 255f}
    };

    [SerializeField]
    private float m_cost = 1;  // Range: 1 - 255 (there's no cell with purely 0 cost)
    public float cost { get { return m_cost; } set {}   }

    private Color m_costColor = Color.white;
    public Color costColor {
        get { return m_costColor; }
        set {}
    }

    public Dictionary<Cell,CellNeighbor> neighbors = new Dictionary<Cell,CellNeighbor>();
    public List<Agent> agentsInside = new List<Agent>();

    /*
    private void Awake() {
        if (r == null) r = GetComponent<Renderer>();
    }
    */

    public Cell(CellController cellController, int x, int y) {
        // Set references
        this.cellController = cellController;
        index = new Vector2Int(x,y);
        // Set Pos
        m_worldPos = 
            this.cellController.transform.position + 
            new Vector3(
                (index.x*this.cellController.resolution) + (this.cellController.resolution*0.5f), 
                0f, 
                (index.y*this.cellController.resolution) + (this.cellController.resolution*0.5f)
            );
        // this.transform.localScale = new Vector3(this.cellController.resolution,this.cellController.resolution,this.cellController.resolution);
        // We need to determine if we're a Traversable, Rough, or Impassable cell. This will determine our cell's type
        CheckCellType();

        // For good measure, update appearance before we start
        UpdateAppearance();
    }

    public void SetNeighbor(Cell neighbor) {
        if (!neighbors.ContainsKey(neighbor)) {
            // For now, we'll just do a Vector3.Distance for hte distance between this cell and the neighbor.
            float d = Vector3.Distance(m_worldPos, neighbor.worldPos);
            Vector3 dTo = (neighbor.worldPos - m_worldPos).normalized;
            neighbors.Add(neighbor,new CellNeighbor(d, dTo));
        }
    }

    public void AddAgentInside(Agent agent) {
        if (!agentsInside.Contains(agent)) agentsInside.Add(agent);
    }

    private void CheckCellType() {
        // We essentially are just checking for colliders that match specific layers
        int terrainMask = LayerMask.GetMask("RoughTerrain", "ImpassableTerrain");
        Collider[] obstacles = Physics.OverlapBox(m_worldPos, Vector3.one * cellController.resolution,Quaternion.identity,terrainMask);
        bool hasHitLayer = false;   // Just a check
        foreach(Collider col in obstacles) {
            // If we find a collider that is impassable, then it's truly impassable, no joke (we're not taking into account moving agents - just the environment)
            if (col.gameObject.layer == 9) {
                // Impassable. Set cellType to "Impassable" and rest of the loop
                m_cellType = CellType.Impassable;
                break;
            }
            else if (!hasHitLayer && col.gameObject.layer == 8) {
                // Rough. Set type to "Rough". Don't skip yet because we might hit an "Impassable" type of environment
                m_cellType = CellType.Rough;
                hasHitLayer = true;
            }            
        }

        // Now, if we want to start adding and counting costs, we can just call costDictionary[m_cellType].
        return;        
    }

    public void UpdateCell() {
        if (m_cellType == CellType.Impassable) return;
        if (agentsInside.Count == 0) return;
        // Update # of agents inside this cell
        UpdateAgentNumbers();
        // Update appearance
        UpdateAppearance();
    }

    private void UpdateAgentNumbers() {
        // We need to check with each agent if they're still in range.
        // The Agent has a track record of which indices it's closest to.
        // We just need to check if our index is among those indices
        List<Agent> tempIn = new List<Agent>();
        foreach(Agent agent in agentsInside) {
            if (agent.CheckIfCloseToArea(index)) tempIn.Add(agent);
        }
        agentsInside = tempIn;
    }

    public float GetCellCost() {
        float c = costDictionary[m_cellType] + (float)agentsInside.Count * 200f;  // For now, we're just setting the cost of a person to be 200
        return Mathf.Clamp(c, 1f, 255f);
    }
    public float GetPureCellCost() {
        float c = costDictionary[m_cellType];
        return Mathf.Clamp(c,1f,255f);
    }

    public Color GetCellColor() {
        // Total weight = cell cost / maximum (so 255 == impassable, 1 = traversable)
        float c = GetCellCost();
        float cVal = c / 255f;
        return Color.Lerp(Color.white, Color.red, cVal);
    }

    private void UpdateAppearance() {
        m_costColor = GetCellColor();
        // r.material.SetColor("_Color", costColor);
    }

    public Vector3[] GetVerts() {
        return new Vector3[]
        {
            new Vector3(m_worldPos.x - cellController.resolution*0.5f, m_worldPos.y, m_worldPos.z - cellController.resolution*0.5f),
            new Vector3(m_worldPos.x - cellController.resolution*0.5f, m_worldPos.y, m_worldPos.z + cellController.resolution*0.5f),
            new Vector3(m_worldPos.x + cellController.resolution*0.5f, m_worldPos.y, m_worldPos.z + cellController.resolution*0.5f),
            new Vector3(m_worldPos.x + cellController.resolution*0.5f, m_worldPos.y, m_worldPos.z - cellController.resolution*0.5f)
        };
    }

    public string PrintCell() {
        return "Cell ["+index.x+","+index.y+"] @ "+m_worldPos.ToString()+" of type "+m_cellType.ToString()+":\nAgents #: "+agentsInside.Count.ToString()+" || Cost: "+GetCellCost().ToString();
    }
    public string PrintCellInsideAgents() {
        if (agentsInside.Count == 0) return "No agents inside...";
        string s = "";
        foreach(Agent agent in agentsInside) {
            s += "["+agent.index+"]";
        }
        return s;
    }
    public string PrintCellNeighbors() {
        if (neighbors.Count == 0) return "No neighbors...";
        string s = "";
        foreach(KeyValuePair<Cell, CellNeighbor> kvp in neighbors) {
            s += "\t> ["+kvp.Key.index.x+","+kvp.Key.index.y+"] - World Pos:"+kvp.Key.worldPos.ToString()+" | Distance:"+kvp.Value.distance.ToString("F2")+" | Direction:"+kvp.Value.directionTo.ToString()+"\n";
        }
        return s;
    }

}
