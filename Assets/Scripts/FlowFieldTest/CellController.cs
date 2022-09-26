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
    private bool m_initialized = false;
    public bool initialized {
        get { return m_initialized; }
        set {}
    }

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

    [SerializeField, Tooltip("How big should your chunks be?")]
    private int chunkSize = 10;

    public bool displayGrid = true;
    public Color gridColor = Color.yellow;

    private void OnDrawGizmos() {
        if (displayGrid) DrawGrid();
    }
    private void DrawGrid() {
        for(int x = 0; x < m_dimensions.x; x++) {
            for(int y = 0; y < m_dimensions.y; y++) {
                Gizmos.color =  Color.white;
                Vector3 center = transform.position + new Vector3( x*m_resolution + m_resolution*0.5f, 0f, y*m_resolution + m_resolution*0.5f);
                Vector3 size = Vector3.one * m_resolution;
                Gizmos.DrawWireCube(center,size);
                Gizmos.DrawWireSphere(center,0.01f);
            }
        }
    }

    private void Awake() {
        // Upon initialization, the program will look at the intended dimensions and determine how many cells are needed
        m_cells = new Cell[dimensions.x,dimensions.y];
        // Initialize `cell` and `cellObject` variables that'll be used constantly, to save memory.
        //Cell cell;
        //GameObject cellObject;
        // Iterating through multidimensional array;
        int xMin, xMax, yMin, yMax;
        for(int x = 0; x < dimensions.x; x++) {
            for(int y = 0; y < dimensions.y; y++) {
                // Initializing Area
                //cellObject = Instantiate(cellPrefab,transform.position,Quaternion.identity,this.transform);
                //cell = cellObject.GetComponent<Cell>();
                //cell.Initialize(this,x,y);
                m_cells[x,y] = new Cell(this,x,y);
                // Setting neighbors
                xMin = (x > 0) ? x-1 : x;
                xMax = (x < dimensions.x - 1) ? x+1 : x;
                yMin = (y > 0) ? y-1 : y;
                yMax = (y < dimensions.y - 1) ? y+1 : y;
                for(int xn = xMin; xn <= xMax; xn++) {
                    for(int yn = yMin; yn <= yMax; yn++) {
                        if (m_cells[xn,yn] == null) continue;
                        if (xn == x && yn == y) continue;
                        m_cells[x,y].SetNeighbor(m_cells[xn,yn]);
                        m_cells[xn,yn].SetNeighbor(m_cells[x,y]);
                    }
                }
            }
        }
        // To optimize, we'll be running several coroutines at the same time, to ensure that our cells are updated efficiently
        for(int x = 0; x < m_dimensions.x; x+=chunkSize) {
            xMin = x;
            xMax = ((xMin + chunkSize <= m_dimensions.x)) ? x+chunkSize : m_dimensions.x%(xMin+chunkSize);
            for(int y = 0; y < m_dimensions.y; y+=chunkSize) {
                // for every `chunkSize, we update on a separate coroutine
                yMin = y;
                yMax = ((yMin + chunkSize <= m_dimensions.y)) ? y+chunkSize : m_dimensions.x%(yMin+chunkSize);
                StartCoroutine(UpdateCells(xMin,xMax,yMin,yMax));
            }
        }

        m_initialized = true;
    }

    private IEnumerator UpdateCells(int xMin, int xMax, int yMin, int yMax) {
        while(true) {
            for(int x = xMin; x < xMax; x++) {
                for(int y = yMin; y < yMax; y++) {
                    m_cells[x,y].UpdateCell();
                    yield return null;
                }
            }
            yield return new WaitForSeconds(0.1f);
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
    // Helper, mapper function to determine which coordinate matches which index. Y-coord is ignored
    public Vector2Int Position3DToIndex(Vector3 pos) {
        return new Vector2Int(
            Position1DToIndex(pos.x),
            Position1DToIndex(pos.z)
        );
    }

    // Helper function, gets a random set of coordinates
    public Vector2Int GetRandomCoordinates() {
        int x, y;
        do {
            x = Random.Range(0, m_dimensions.x);
            y = Random.Range(0, m_dimensions.y);
        } while (m_cells[x,y].cellType == Cell.CellType.Impassable);
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
        if (m_cells == null) return false;
        return (coord >= 0 && coord < m_cells.GetLength(axis));
    }
}