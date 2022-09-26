using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* This controller handles all indexing of cells.
* If any other controllers need access to cell data, such as locating cells closest to a point, they go here.
*/

public class CellController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cellPrefab;

    [Header("Cell Determination")]
    // Tracks cells in a grid
    private Cell[,] m_cells;
    public Cell[,] cells {
        get { return m_cells; }
        set {}
    }
    [SerializeField, Tooltip("How many cells long and wide is this grid?")] 
    private Vector2Int m_dimensions = new Vector2Int(10,10);
    public Vector2Int dimensions {
        get { return m_dimensions; }
        set {}
    }
    [SerializeField, Tooltip("How big is each cell?")] 
    private float m_resolution = 0.5f;
    public float resolution {
        get { return m_resolution; }
        set {}
    }

    private void Awake() {
        // Upon initialization, the program will look at the intended dimensions and determine how many cells are needed
        m_cells = new Cell[dimensions.x,dimensions.y];
        // Initialize `cell` and `cellObject` variables that'll be used constantly, to save memory.
        Cell cell;
        GameObject cellObject;
        // Iterating through multidimensional array;
        for(int x = 0; x < dimensions.x; x++) {
            for(int y = 0; y < dimensions.y; y++) {
                // Initializing Area
                cellObject = Instantiate(cellPrefab,transform.position,Quaternion.identity,this.transform);
                cell = cellObject.GetComponent<Cell>();
                cell.Initialize(this,x,y);
                m_cells[x,y] = cell;
                // Setting neighbors
                if (x>0) {
                    cell.SetNeighbor(m_cells[x-1,y]);
                    m_cells[x-1,y].SetNeighbor(cell);
                }
                if (y>0)  {
                    cell.SetNeighbor(m_cells[x,y-1]);
                    m_cells[x,y-1].SetNeighbor(cell);
                }
                if (x>0 && y>0) {
                    cell.SetNeighbor(m_cells[x-1,y-1]);
                    m_cells[x-1,y-1].SetNeighbor(cell);
                }
            }
        }
    }

    public void AgentToCellUpdate(Agent agent) {
        // At this point, all we know is: 1) the position of the current agent, and 2) the agent's radius.
        // We can pinpoint the specific m_cells the agent is in.
        // The maximum X indices we need to consider is minX < x < maxX. Same with Y
        int minX = Position1DToIndex(agent.transform.position.x - agent.radius);
        int maxX = Position1DToIndex(agent.transform.position.x + agent.radius);
        int minY = Position1DToIndex(agent.transform.position.z - agent.radius);
        int maxY = Position1DToIndex(agent.transform.position.z + agent.radius);
        
        // We keep track of the indices that the agent is close to
        List<Vector2Int> closeTo = new List<Vector2Int>();
        // Iterate through possible cells, confirming their hits and telling our Agent that we hit those cells
        for(int x = minX; x <= maxX; x++) {
            if (!Validate1DCoord(x,0)) continue;
            for (int y = minY; y <= maxY; y++) {
                if (!Validate1DCoord(y,1)) continue;
                // We add to our closeTo
                closeTo.Add(new Vector2Int(x,y));
                // We notify the cell if they fit within the radius of the obstacle
                m_cells[x,y].AddAgentInside(agent);
            }
        }
        // We notify the agent what indices they're closest to
        agent.SetCellIndices(closeTo);
    }

    // Helper, mapper function to determine which coordinate matches which index.
    public int Position1DToIndex(float pos) {
        return (int)Mathf.Floor(pos/m_resolution);
    }
    // Helper, mapper function to determine which coordinate matches which index.
    public Vector2Int Position2DToIndex(Vector2 pos) {
        return new Vector2Int(
            Position1DToIndex(pos.x),
            Position1DToIndex(pos.y)
        );
    }

    // Helper function, gets a random set of coordinates
    public Vector2Int GetRandomCoordinates() {
        int x = Random.Range(0, m_dimensions.x);
        int y = Random.Range(0, m_dimensions.y);
        return new Vector2Int(x,y);
    }

    // Helper function, locates a cell based on indices
    public Cell GetCellFromCoordinates(Vector2Int coords) {
        // We return `NULL` if no cells found from coordinates
        // Otherwise, we return the cell with the corresponing coordinates
        return ValidateCoords(coords) ? m_cells[coords.x,coords.y] : null;
    }

    // Helper function, checks if the provided coords are in the scope of the grid structure
    public bool ValidateCoords(Vector2Int coords) {
        return Validate1DCoord(coords.x, 0) && Validate1DCoord(coords.y, 1);
    }
    // Helper function, checks if the provided 1D coord are in the scope of the grid structure, given axis (0==x or 1==y)
    public bool Validate1DCoord(int coord, int axis) {
        return (coord >= 0 && coord < m_cells.GetLength(axis));
    }
}