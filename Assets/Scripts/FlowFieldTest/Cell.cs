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

public class Cell : MonoBehaviour {

    [Header("References")]
    //      [SerializeField] private Canvas canvas;
    //      [SerializeField] TextMeshProUGUI textbox;
    [SerializeField] private Renderer r;

    public Vector2Int index = Vector2Int.zero;
    private CellController cellController;

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

    private Color costColor = Color.white;

    public Dictionary<Cell,CellNeighbor> neighbors = new Dictionary<Cell,CellNeighbor>();
    public List<Agent> agentsInside = new List<Agent>();

    private void Awake() {
        if (r == null) r = GetComponent<Renderer>();
    }

    public void Initialize(CellController cellController, int x, int y) {
        // Set references
        this.cellController = cellController;
        index = new Vector2Int(x,y);
        // Set Pos
        this.transform.position = 
            this.cellController.transform.position + 
            new Vector3(
                (index.x*this.cellController.resolution) + (this.cellController.resolution*0.5f), 
                0f, 
                (index.y*this.cellController.resolution) + (this.cellController.resolution*0.5f)
            );
        this.transform.localScale = new Vector3(this.cellController.resolution,this.cellController.resolution,this.cellController.resolution);
        // We need to determine if we're a Traversable, Rough, or Impassable cell. This will determine our cell's type
        CheckCellType();

        // For good measure, update appearance before we start
        UpdateAppearance();
    }

    public void SetNeighbor(Cell neighbor) {
        if (!neighbors.ContainsKey(neighbor)) {
            // For now, we'll just do a Vector3.Distance for hte distance between this cell and the neighbor.
            float d = Vector3.Distance(transform.position, neighbor.transform.position);
            Vector3 dTo = Vector3.ClampMagnitude(neighbor.transform.position - transform.position,1f);
            neighbors.Add(neighbor,new CellNeighbor(d, dTo));
        }
    }

    public void AddAgentInside(Agent agent) {
        if (!agentsInside.Contains(agent)) agentsInside.Add(agent);
    }

    private void CheckCellType() {
        // We essentially are just checking for colliders that match specific layers
        int terrainMask = LayerMask.GetMask("RoughTerrain", "ImpassableTerrain");
        Collider[] obstacles = Physics.OverlapBox(transform.position, Vector3.one * cellController.resolution,Quaternion.identity,terrainMask);
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

    private void Update() {
        // We only return early if this is an Impassable type (we shouldn't see any agents in here)
        if (m_cellType == CellType.Impassable) return;

        // Update # of agents inside this cell
        UpdateAgentNumbers();
        // Update appearance
        UpdateAppearance();
        return;
    }

    private void UpdateAgentNumbers() {
        // We end early if there aren't any agents to consider
        if (agentsInside.Count == 0) {
            return;
        }
        // We need to check with each agent if they're still in range.
        // The Agent has a track record of which indices it's closest to.
        // We just need to check if our index is among those indices

        List<Agent> tempIn = new List<Agent>();
        foreach(Agent agent in agentsInside) {
            if (agent.CheckIfCloseToArea(index)) tempIn.Add(agent);
        }
        bool hasAgents = tempIn.Count > 0;
        agentsInside = tempIn;
        return;
    }

    public float GetCellCost() {
        float c = costDictionary[m_cellType] + (float)agentsInside.Count * 200f;  // For now, we're just setting the cost of a person to be 200
        return Mathf.Clamp(c, 1f, 255f);
    }

    public Color GetCellColor() {
        // Total weight = cell cost / maximum (so 255 == impassable, 1 = traversable)
        float c = GetCellCost();
        float cVal = c / 255f;
        return Color.Lerp(Color.white, Color.red, cVal);
    }

    private void UpdateAppearance() {
        Color costColor = GetCellColor();
        r.material.SetColor("_Color", costColor);
    }

    public Vector3[] GetVerts() {
        return new Vector3[]
        {
            new Vector3(transform.position.x - cellController.resolution*0.5f, transform.position.y, transform.position.z - cellController.resolution*0.5f),
            new Vector3(transform.position.x - cellController.resolution*0.5f, transform.position.y, transform.position.z + cellController.resolution*0.5f),
            new Vector3(transform.position.x + cellController.resolution*0.5f, transform.position.y, transform.position.z + cellController.resolution*0.5f),
            new Vector3(transform.position.x + cellController.resolution*0.5f, transform.position.y, transform.position.z - cellController.resolution*0.5f)
        };

    }
}
