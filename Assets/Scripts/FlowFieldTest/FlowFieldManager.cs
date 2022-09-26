using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FlowFieldManager : MonoBehaviour
{
    [SerializeField] private CellController cellController;
    [SerializeField] private Cell destinationCell = null;

    // We have 3 grids. 
    // The first is an "integration" grid, that keeps track of the costs to each cell neighbor
    private float[,] integrationGrid;
    // The second grid stores the biggest cost for each cell. Since this is specific to this flowfield implementation, we just create a grid here
    private float[,] biggestCostGrid;
    // The third is a Vector3 2D array for the integratiojn field...
    private Vector3[,] flowFieldGrid;

    public enum RenderType {
        None,
        Grid,
        IntegrationField,
        FlowField,
    }
    public RenderType renderType = RenderType.FlowField;
    public bool displayGrid = true;
    public Color gridColor = Color.yellow;

    private void OnDrawGizmos() {
        if (displayGrid) DrawGrid();
    }
    private void DrawGrid() {
        if (cellController == null) return;
        for(int x = 0; x < cellController.dimensions.x; x++) {
            for(int y = 0; y < cellController.dimensions.y; y++) {
                Gizmos.color = cellController.cells[x,y].GetCellColor();
                Vector3 center = transform.position + new Vector3( x*cellController.resolution + cellController.resolution*0.5f, 0f, y*cellController.resolution + cellController.resolution*0.5f);
                Vector3 size = Vector3.one * cellController.resolution;
                Gizmos.DrawWireCube(center,size);
                Gizmos.DrawWireSphere(center,0.01f);
            }
        }
    }

    private void OnGUI() {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.black;
        switch(renderType) {
            case RenderType.Grid:
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for (int y = 0; y < cellController.dimensions.y; y++) {
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),curCell.GetCellColor(),Color.black);
                        Handles.Label(curCell.transform.position, "", style);
                    }
                }
                break;
            case RenderType.IntegrationField:
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        float cost = integrationGrid[x,y];
                        Cell curCell = cellController.cells[x,y];
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),curCell.GetCellColor(),Color.black);
                        Handles.Label(curCell.transform.position, cost.ToString(), style);
                    }
                }
                break;
            case RenderType.FlowField:
                for(int x = 0; x < cellController.dimensions.x; x++) {
                    for(int y = 0; y < cellController.dimensions.y; y++) {
                        // Vector2Int direction = flowGrid[x,y];
                        Vector3 direction = flowFieldGrid[x,y];
                        Cell curCell = cellController.cells[x,y];
                        // Handles.DrawLine(curCell.worldPos,curCell.worldPos + new Vector3(direction.x * 0.5f * m_cellSize, 0f, direction.y * 0.5f * m_cellSize), 0.1f);
                        Handles.DrawSolidRectangleWithOutline(curCell.GetVerts(),curCell.GetCellColor(),Color.black);
                        Handles.Label(curCell.transform.position, "", style);
                        Handles.DrawLine(
                            curCell.transform.position,
                            curCell.transform.position + direction*0.5f*cellController.resolution, 
                            0.2f
                        );
                    }
                }
                break;
        }
    }

    private void Awake() {
        integrationGrid = new float[cellController.dimensions.x,cellController.dimensions.y];
        biggestCostGrid = new float[cellController.dimensions.x,cellController.dimensions.y];
        flowFieldGrid = new Vector3[cellController.dimensions.x,cellController.dimensions.y];
        ResetIntegrationGrid();
        ResetFlowGrid();
    }

    private void Update() {
        if (CheckInputs()) {
            // our destination cell changed. We need to do some re-calculating
            GenerateNewFlowField();
        }
    }

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

    public void GenerateNewFlowField() {
        ResetIntegrationGrid();
        GenerateIntegrationField();
        ResetFlowGrid();
        GenerateFlowField();
    }

    private void GenerateIntegrationField() {
        if (destinationCell == null) return;    // Don't bother if the destination cell is bogus

        // First thing is to set the integration grid's value for our destination cell to 0
        integrationGrid[destinationCell.index.x,destinationCell.index.y] = 0f;

        // We generate some lists for processing each cell
        List<Cell> cellsToCheck = new List<Cell>();
        List<Cell> cellsProcessed = new List<Cell>();
        // We add the destination cell to the first to be checked
        cellsToCheck.Add(destinationCell);

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
                    integrationGrid[curNeighbor.index.x,curNeighbor.index.y] = 255;
                    continue;
                }
                // Assuming the neighbor is NOT an impassable cell, we get the cost of going to this cell through an equation:
                // 1. Get the current cell's integration cost
                /// 2 add that to the cost of actually GOING to that neighbor.
                float c = integrationGrid[curCell.index.x,curCell.index.y] + curDistance;
                // If the `cellsProcessed` already contains the neighbor, then we need to do a comparison between `c` and its existing cost in the grid
                if (cellsProcessed.Contains(curNeighbor)) {
                    // If `c` is LESS than the current integration cost for this neighbor, we replace that cost with `c` instead
                    if (c < integrationGrid[curNeighbor.index.x,curNeighbor.index.y]) {
                        integrationGrid[curNeighbor.index.x,curNeighbor.index.y] = c;
                    }
                    // If `c` is bigger than the biggest distance, we gotta replace that to
                    if (c > biggestDistance) biggestDistance = c;
                } 
                // We haven't processed this neighbor yet. So let's just blindly set the cost
                else {
                    integrationGrid[curNeighbor.index.x,curNeighbor.index.y] = c;
                    cellsProcessed.Add(curNeighbor);
                    cellsToCheck.Add(curNeighbor);
                    // Like in the `if` statement, we replace the biggest distance with `c` if `c` is bigger.
                    if (c > biggestDistance) biggestDistance = c;
                }
            }
            // We store the biggest cost for the current cell inside our special `biggestCostGrid`
            biggestCostGrid[curCell.index.x,curCell.index.y] = biggestDistance;
            // We add our curCell to the list of processed cells.
            if (!cellsProcessed.Contains(curCell)) cellsProcessed.Add(curCell);
        }
    }

    private void GenerateFlowField() {
        // Initialize some variables for memory
        Cell curCell;
        Vector3 goToVector, curNeighborVector;
        float modifier;
        // We first look at each cell
        for(int x = 0; x < cellController.cells.GetLength(0); x++) {
            for(int y = 0; y < cellController.cells.GetLength(1); y++) {
                curCell = cellController.cells[x,y];
                goToVector = Vector3.zero;
                // If our current cell is NOT the destination cell, we peform the flow field calculation
                if (curCell != destinationCell) {
                    // We iterate through each neighbor, contributing to modifier for each one
                    foreach(KeyValuePair<Cell, CellNeighbor> kvp in curCell.neighbors) {
                        modifier = (integrationGrid[kvp.Key.index.x,kvp.Key.index.y] == 255f)
                            ? 0f 
                            : biggestCostGrid[curCell.index.x,curCell.index.y] - integrationGrid[kvp.Key.index.x, kvp.Key.index.y];
                        goToVector += new Vector3(kvp.Value.directionTo.x, 0f, kvp.Value.directionTo.y) * modifier;
                    }
                }
                flowFieldGrid[curCell.index.x, curCell.index.y] = goToVector.normalized;
            }
        }
    }

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
}
