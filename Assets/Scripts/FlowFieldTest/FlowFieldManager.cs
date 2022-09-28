using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[System.Serializable]
public class FlowField {
    public Vector2Int dimensions;
    private Vector2Int m_cellIndex;
    public Vector2Int cellIndex {
        get { return m_cellIndex; }
        set {}
    }
    private float[,] m_integrationGrid;
    public float[,] integrationGrid {
        get { return m_integrationGrid; } set {}
    }
    private float[,] m_biggestCostGrid;
    public float[,] biggestCostGrid {
        get { return m_biggestCostGrid; } set {}
    }
    private Vector3[,] m_flowFieldGrid;
    public Vector3[,] flowFieldGrid {
        get { return m_flowFieldGrid; } set {}
    }
    public FlowField(int x, int y) {
        dimensions = new Vector2Int(x,y);
        m_integrationGrid = new float[dimensions.x,dimensions.y];
        m_biggestCostGrid = new float[dimensions.x,dimensions.y];
        m_flowFieldGrid = new Vector3[dimensions.x,dimensions.y];
    }
    public void SetCellIndex(Vector2Int index) {
        m_cellIndex = index;
    }
    public void SetCellIndex(int x, int y) {
        SetCellIndex(new Vector2Int(x,y));
    }
    public void ResetIntegrationGrid() {
        m_integrationGrid = new float[dimensions.x,dimensions.y];
        m_biggestCostGrid = new float[dimensions.x,dimensions.y];
    }
    public void ResetFlowGrid() {
        m_flowFieldGrid = new Vector3[dimensions.x,dimensions.y];
    }
    public bool SetIntegrationGrid(float[,] newField) {
        if (newField.GetLength(0) != dimensions.x || newField.GetLength(1) != dimensions.y) return false;
        m_integrationGrid = newField;
        return true;
    }
    public bool SetBiggestCostGrid(float[,] newField) {
        if (newField.GetLength(0) != dimensions.x || newField.GetLength(1) != dimensions.y) return false;
        m_biggestCostGrid = newField;
        return true;
    }
    public bool SetFlowFieldGrid(Vector3[,] newField) {
        if (newField.GetLength(0) != dimensions.x || newField.GetLength(1) != dimensions.y) return false;
        m_flowFieldGrid = newField;
        return true;
    }
}

[System.Serializable]
public class FlowFieldRecords {
    public List<FlowFieldPrint> flowFieldRecord;
    public FlowFieldRecords() {}
}

[System.Serializable]
public class FlowFieldPrint {
    public int cellX, cellY, dimX, dimY;
    public float[,] integrationGrid;
    public float[,] biggestCostGrid;
    public Vector3[,] flowFieldGrid;
    public FlowFieldPrint() {}
}

public class FlowFieldManager : MonoBehaviour
{
    [SerializeField] private CellController cellController;
    [SerializeField] private AgentController agentController;
    [SerializeField] private Cell destinationCell = null;
    [SerializeField] 
    private FlowField m_currentFlowField = null;
    public FlowField currentFlowField {
        get { return m_currentFlowField; }
        set { 
            m_currentFlowField = value;
            if (m_currentFlowField == null) {
                if (flowFieldGeneratingCoroutine != null) StopCoroutine(flowFieldGeneratingCoroutine);
            } else {
                if (flowFieldGeneratingCoroutine == null) {
                    flowFieldGeneratingCoroutine = RecalculateFlowField();
                    StartCoroutine(flowFieldGeneratingCoroutine);
                }
            }
        }
    }

    [SerializeField] private string flowFieldFileKey = "";

    // We have 3 grids. 
    // The first is an "integration" grid, that keeps track of the costs to each cell neighbor
    private float[,] integrationGrid;
    // The second grid stores the biggest cost for each cell. Since this is specific to this flowfield implementation, we just create a grid here
    private float[,] biggestCostGrid;
    // The third is a Vector3 2D array for the integration field...
    private Vector3[,] flowFieldGrid;
    // This one is special - we'll pre-store all the flow grids into a Dictionary. When a cell is selected as a destination, we'll just call from the dictionary.
    private Dictionary<Cell, FlowField> flowFieldDictionary = new Dictionary<Cell, FlowField>();

    public enum RenderType {
        None,
        Grid,
        IntegrationField,
        BiggestCostField,
        FlowField,
    }
    public RenderType renderType = RenderType.Grid;
    public bool displayGrid = true;
    public Color gridColor = Color.yellow;

    private bool m_generatingFlowFields = false;

    private IEnumerator flowFieldGeneratingCoroutine = null;

#if UNITY_EDITOR
    private void OnGUI() {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.black;
        if (!cellController.initialized) return;
        GUIStyle guiStyle;
        switch(renderType) {
            case RenderType.Grid:
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for (int y = 0; y < cellController.dimensions.y; y++) {
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),Color.clear,curCell.GetCellColor());
                        Handles.Label(curCell.worldPos, "", style);
                    }
                }
                break;
            case RenderType.IntegrationField:
                guiStyle = new GUIStyle(); //create a new variable
                guiStyle.fontSize = 15; //change the font size
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),Color.clear,curCell.GetCellColor());
                        if (m_currentFlowField == null) {
                            Handles.Label(curCell.worldPos,"", style);
                        } else if (m_currentFlowField.integrationGrid == null) {
                            Handles.Label(curCell.worldPos,"", style);
                        } else {
                            float cost = m_currentFlowField.integrationGrid[x,y];
                          Handles.Label(curCell.worldPos, cost.ToString("F2"), guiStyle);
                        }

                    }
                }
                break;
            case RenderType.BiggestCostField:
                guiStyle = new GUIStyle(); //create a new variable
                guiStyle.fontSize = 15; //change the font size
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),Color.clear,curCell.GetCellColor());
                        if (m_currentFlowField == null) {
                            Handles.Label(curCell.worldPos,"", style);
                        } else if (m_currentFlowField.biggestCostGrid == null) {
                            Handles.Label(curCell.worldPos,"", style);
                        } else {
                            float cost = m_currentFlowField.biggestCostGrid[x,y];
                            Handles.Label(curCell.worldPos, cost.ToString("F2"), guiStyle);
                        }
                    }
                }
                break;
            case RenderType.FlowField:
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),Color.clear,curCell.GetCellColor());
                        Handles.Label(curCell.worldPos, "", style);
                        if (m_currentFlowField == null) continue;
                        if (m_currentFlowField.flowFieldGrid == null) continue;
                        Vector3 direction = m_currentFlowField.flowFieldGrid[x,y];
                        Handles.DrawLine(
                            curCell.worldPos,
                            curCell.worldPos + direction*0.5f*cellController.resolution, 
                            0.2f
                        );
                    }
                }
                break;
        }
    }
#endif

    private void Start() {
        /*
        integrationGrid = new float[cellController.dimensions.x,cellController.dimensions.y];
        biggestCostGrid = new float[cellController.dimensions.x,cellController.dimensions.y];
        flowFieldGrid = new Vector3[cellController.dimensions.x,cellController.dimensions.y];
        ResetIntegrationGrid();
        ResetFlowGrid();
        */

        // Start generating flow fields
        //m_generatingFlowFields = true;
        //StartCoroutine(BakeFlowFields());
    }

    private IEnumerator BakeFlowFields() {
        Debug.Log("Starting to bake flow fields...");
    
        // First, we check if we have data we can actually read
        // If we do, let's try to read it. If this returns true, we already have our data. Otherwise, we have to re-generate it.
        if (!LoadExistingDictionary()) {
            flowFieldDictionary = new Dictionary<Cell, FlowField>();
            FlowFieldRecords records = new FlowFieldRecords();
            records.flowFieldRecord = new List<FlowFieldPrint>();
            FlowFieldPrint tempRecord;
            Cell curCell;
            FlowField curField = new FlowField(cellController.dimensions.x,cellController.dimensions.y);
            float[,] tempIntGrid, tempBigGrid;
            Vector3[,] tempFlowGrid;

            while(m_generatingFlowFields) {
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        curCell = cellController.cells[x,y];
                        curField.SetCellIndex(x,y);
                        Debug.Log("Generating Cell @ [" + curCell.index.x + "," + curCell.index.y + "]");
                        // We skip impassable ones
                        if (curCell.cellType == Cell.CellType.Impassable) {
                            yield return null;
                            continue;
                        }
                        // We generate the flow field, assuming that the current cell is the destination
                        if(GenerateIntegrationField(curCell, out tempIntGrid, out tempBigGrid)) {
                            curField.SetIntegrationGrid(tempIntGrid);
                            curField.SetBiggestCostGrid(tempBigGrid);
                            if (GenerateFlowField(curCell, curField.integrationGrid, curField.biggestCostGrid, out tempFlowGrid)) {
                                curField.SetFlowFieldGrid(tempFlowGrid);
                                // At this point, all we need to do is save this new `curField` into our dictionary.
                                flowFieldDictionary.Add(curCell,curField);
                                // Save our data into a tempRecord, for saving later
                                tempRecord = new FlowFieldPrint();
                                tempRecord.cellX = x;
                                tempRecord.cellY = y;
                                tempRecord.dimX = curField.dimensions.x;
                                tempRecord.dimY = curField.dimensions.y;
                                tempRecord.integrationGrid = curField.integrationGrid;
                                tempRecord.biggestCostGrid = curField.biggestCostGrid;
                                tempRecord.flowFieldGrid = curField.flowFieldGrid;
                                records.flowFieldRecord.Add(tempRecord);
                            }
                        }
                        yield return null;
                    }
                }
                yield return null;
            }
            // Save our dictionary into a file
            if (!SaveDictionaryFile(records)) {
                Debug.Log("UNABLE TO SAVE FILE... YA DINGUS, YOU BONKERED SOMETHING");
            }
        }

        // Now we can generate agents
        Debug.Log("All field generated for each possible destination cell. # Flow Fields: " + flowFieldDictionary.Count);
        //agentController.Initialize();
    }

    private void Update() {
        CheckInputs();
    }

    private void CheckInputs() {
        Vector3 mousePos;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int closestCellCoordinates = cellController.Position2DToIndex(new Vector2(mousePos.x, mousePos.z));
            Cell closestCell = cellController.GetCellFromCoordinates(closestCellCoordinates);
            if (closestCell != null) {
                Debug.Log(closestCell.PrintCell());
                Debug.Log(closestCell.PrintCellNeighbors());
                // Generate Flow Field
                StartCoroutine(GenerateFlowFieldFromClick(closestCell));
            }
        }
    }

    private IEnumerator GenerateFlowFieldFromClick(Cell _destination) {
        if (
            _destination.cellType == Cell.CellType.Impassable
            || (destinationCell != null && destinationCell == _destination) 
        ) {
            // In this scenario, we either clicked an impassable cell or the destination in our memory is the same as the selected cell
            // We will destroy the flow field and reset everything
            ResetFlowField();
            yield return null;
        }
        else {
            // In this scenario, we're doing it - we're generating teh flow field
            FlowField newFlowField = new FlowField(cellController.dimensions.x,cellController.dimensions.y);
            newFlowField.SetCellIndex(_destination.index);
            float[,] tempIntGrid, tempBigGrid;
            Vector3[,] tempFlowGrid;
            if(GenerateIntegrationField(_destination, out tempIntGrid, out tempBigGrid)) {
                newFlowField.SetIntegrationGrid(tempIntGrid);
                newFlowField.SetBiggestCostGrid(tempBigGrid);
                yield return null;
                if (GenerateFlowField(_destination, newFlowField.integrationGrid, newFlowField.biggestCostGrid, out tempFlowGrid)) {
                    newFlowField.SetFlowFieldGrid(tempFlowGrid);
                    // We've completed our new flow field.
                    destinationCell = _destination;
                    currentFlowField = newFlowField;
                    yield return null;
                } else {
                    // For some reason, we failed. We can't do anything...
                    Debug.Log("FAILED TO GENERATE FLOW FIELD FROM CLICK");
                    ResetFlowField();
                    yield return null;
                }
            } else {
                // For some reason, we failed. We can't do anything...
                Debug.Log("FAILED TO GENERATE INTEGRATION FIELD FROM CLICK");
                ResetFlowField();
                yield return null;
            }
        }
    }

    private IEnumerator RecalculateFlowField() {
        while(true) {
            Cell _destination = destinationCell;
            FlowField newFlowField = new FlowField(cellController.dimensions.x,cellController.dimensions.y);
            newFlowField.SetCellIndex(_destination.index);
            float[,] tempIntGrid, tempBigGrid;
            Vector3[,] tempFlowGrid;
            if(GenerateIntegrationField(_destination, out tempIntGrid, out tempBigGrid)) {
                newFlowField.SetIntegrationGrid(tempIntGrid);
                newFlowField.SetBiggestCostGrid(tempBigGrid);
                yield return null;
                if (GenerateFlowField(_destination, newFlowField.integrationGrid, newFlowField.biggestCostGrid, out tempFlowGrid)) {
                    newFlowField.SetFlowFieldGrid(tempFlowGrid);
                    // We've completed our new flow field.
                    destinationCell = _destination;
                    m_currentFlowField = newFlowField;
                    yield return null;
                } else {
                    // For some reason, we failed. We can't do anything...
                    Debug.Log("FAILED TO GENERATE FLOW FIELD FROM CLICK");
                    ResetFlowField();
                    yield return null;
                }
            } else {
                // For some reason, we failed. We can't do anything...
                Debug.Log("FAILED TO GENERATE INTEGRATION FIELD FROM CLICK");
                ResetFlowField();
                yield return null;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    /*
    private bool CheckInputs() {
        Vector3 mousePos;
        bool changed = false;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int closestCellCoordinates = cellController.Position2DToIndex(new Vector2(mousePos.x, mousePos.z));
            Cell closestCell = cellController.GetCellFromCoordinates(closestCellCoordinates);
            if (closestCell != null) {
                // We got a hit. This will become the next destination (or cancel it, if it's already a destination.)
                destinationCell = (destinationCell == closestCell) ? null : closestCell;
                changed = true;
            }
        }
        return changed;
    }
    */

    public void ResetFlowField() {
        destinationCell = null;
        currentFlowField = null;
    }

    /*
    public void GenerateNewFlowField() {
        ResetIntegrationGrid();
        GenerateIntegrationField();
        ResetFlowGrid();
        GenerateFlowField();
    }
    */
    
    private bool GenerateIntegrationField(Cell _destination, out float[,] newIntegrationGrid, out float[,] newBiggestCostGrid) {
        // Given a destination cell, we create an integration grid and return it.

        // First thing is to set the integration grid's value for our destination cell to 0
        newIntegrationGrid = new float[cellController.dimensions.x,cellController.dimensions.y]; 
        newBiggestCostGrid = new float[cellController.dimensions.x,cellController.dimensions.y];
        if (_destination == null) return false;    // Don't bother if the destination cell is bogus

        newIntegrationGrid[_destination.index.x,_destination.index.y] = 0f;

        // We generate some lists for processing each cell
        List<Cell> cellsToCheck = new List<Cell>();
        List<Cell> cellsProcessed = new List<Cell>();
        // We add the destination cell to the first to be checked
        cellsToCheck.Add(_destination);

        // Creating a cell to store for memory, alongside a float for the current neighbor and biggest distances
        Cell curCell, curNeighbor;
        float curDistance, biggestDistance;

        // Iterate through our list of cells to check. This will keep going until all cells have been checked.
        while(cellsToCheck.Count > 0) {
            // We get the first cell,the remove it from our cells to check
            curCell = cellsToCheck[0];
            cellsToCheck.RemoveAt(0);

            // We set `biggestDistance` to 0f. We'll be storing hte biggest distance from this cel to its neighbors.
            biggestDistance = 0f;

            // Each Cell contains its neighbors. It stores them in a Dictionary called `neighbors`. The value is the distance to this neighbor cell from teh current cell
            foreach(KeyValuePair<Cell, CellNeighbor> kvp in curCell.neighbors) {
                curNeighbor = kvp.Key;
                curDistance = kvp.Value.distance;
                // If the current cell is impassable (denoted through `cellType`, which is of type `Cell.CellType`)
                // In sch a scenario, we set the integration cst to be 255, the appointed maximum cost possibe
                if (curNeighbor.cellType == Cell.CellType.Impassable) {
                    newIntegrationGrid[curNeighbor.index.x,curNeighbor.index.y] = 255f;
                    continue;
                }
                // Assuming the neighbor is NOT an impassable cell, we get the cost of going to this cell through an equation:
                // 1. Get the current cell's integration cost
                /// 2 add that to the cost of actually GOING to that neighbor.
                float c = newIntegrationGrid[curCell.index.x,curCell.index.y] + curDistance;
                // If the `cellsProcessed` already contains the neighbor, then we need to do a comparison between `c` and its existing cost in the grid
                if (cellsProcessed.Contains(curNeighbor)) {
                    // If `c` is LESS than the current integration cost for this neighbor, we replace that cost with `c` instead
                    if (c < newIntegrationGrid[curNeighbor.index.x,curNeighbor.index.y]) {
                        newIntegrationGrid[curNeighbor.index.x,curNeighbor.index.y] = c;
                    }
                    // If `c` is bigger than the biggest distance, we gotta replace that to
                    if (c > biggestDistance) biggestDistance = c;
                } 
                // We haven't processed this neighbor yet. So let's just blindly set the cost
                else {
                    newIntegrationGrid[curNeighbor.index.x,curNeighbor.index.y] = c;
                    cellsProcessed.Add(curNeighbor);
                    cellsToCheck.Add(curNeighbor);
                    // Like in the `if` statement, we replace the biggest distance with `c` if `c` is bigger.
                    if (c > biggestDistance) biggestDistance = c;
                }
            }
            // We store the biggest cost for the current cell inside our special `biggestCostGrid`
            newBiggestCostGrid[curCell.index.x,curCell.index.y] = biggestDistance;
            // We add our curCell to the list of processed cells.
            if (!cellsProcessed.Contains(curCell)) cellsProcessed.Add(curCell);
        }
        // We return to to inform the program that we're done
        return true;
    }

    private bool GenerateFlowField(Cell _destination, float[,] integGrid, float[,] bigGrid, out Vector3[,] flowGrid) {
        // Initialize some variables for memory
        Cell curCell;
        Vector3 goToVector;
        float modifier;
        flowGrid = new Vector3[cellController.dimensions.x,cellController.dimensions.y];

        // We first look at each cell
        for(int x = 0; x < cellController.cells.GetLength(0); x++) {
            for(int y = 0; y < cellController.cells.GetLength(1); y++) {
                curCell = cellController.cells[x,y];
                goToVector = Vector3.zero;
                // If our current cell is NOT the destination cell, we peform the flow field calculation
                if (curCell != _destination) {
                    // We iterate through each neighbor, contributing to modifier for each one
                    foreach(KeyValuePair<Cell, CellNeighbor> kvp in curCell.neighbors) {
                        if (kvp.Key.cellType == Cell.CellType.Impassable) continue;
                        modifier = (integGrid[kvp.Key.index.x,kvp.Key.index.y] == 255f)
                            ? 0f 
                            : bigGrid[curCell.index.x,curCell.index.y] - integGrid[kvp.Key.index.x, kvp.Key.index.y];
                        goToVector += new Vector3(kvp.Value.directionTo.x, 0f, kvp.Value.directionTo.z) * Mathf.Abs(modifier);
                    }
                }
                flowGrid[curCell.index.x, curCell.index.y] = goToVector.normalized;
            }
        }
        // Return to indicate completion
        return true;
    }

    /*
    private void ResetIntegrationGrid() {
        for(int x = 0; x < integrationGrid.GetLength(0); x++) {
            for(int y = 0; y < integrationGrid.GetLength(1); y++) {
                integrationGrid[x,y] = 0f;
                biggestCostGrid[x,y] = 0f;
            }
        }
    }
    private void ResetFlowGrid() {
        flowFieldGrid = new Vector3[cellController.dimensions.x,cellController.dimensions.y];
    }
    */

    private bool LoadExistingDictionary() {
        string path = Application.dataPath + "/FlowFieldData/" + flowFieldFileKey + ".json";
        if (System.IO.File.Exists(path)) {
            return ReadExistingDictionaryFile(path);
        } else {
            // No file exists, have to generate
            return false;
        }
    }
    private bool SaveDictionaryFile(FlowFieldRecords r) {
        string path = Application.dataPath + "/FlowFieldData/" + flowFieldFileKey + ".json";
        // Convert our data into json
        string data = JsonUtility.ToJson(r);
        System.IO.File.WriteAllText(path, data);
        return true;
    }
    private bool ReadExistingDictionaryFile(string path) {
        string jsonString = File.ReadAllText(path); 
        FlowFieldRecords records = JsonUtility.FromJson<FlowFieldRecords>(jsonString);
        bool finished = true;
        Cell matchingCell = null;
        Vector2Int cellCoords;
        foreach(FlowFieldPrint r in records.flowFieldRecord) {
            // Generate new FlowField type from it
            cellCoords = new Vector2Int(r.cellX,r.cellY);
            if (!cellController.ValidateCoords(cellCoords)) {
                // not valid cell, have to restart
                finished = false;
                break;
            }
            matchingCell = cellController.cells[r.cellX, r.cellY];
            FlowField newFlowField = new FlowField(r.dimX, r.dimY);
            newFlowField.SetCellIndex(cellCoords);
            if (
                !newFlowField.SetIntegrationGrid(r.integrationGrid) 
                || !newFlowField.SetBiggestCostGrid(r.biggestCostGrid)
                || !newFlowField.SetFlowFieldGrid(r.flowFieldGrid)
            ) {
                // Flow field data bonked, have to restart
                finished = false;
                break;
            }
            // Generated flow field, now need to add to dictionary
            flowFieldDictionary.Add(matchingCell,newFlowField);
        }
        return finished;
    }
}
