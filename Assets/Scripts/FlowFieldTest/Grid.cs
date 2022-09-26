using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    private AgentController agentController;
    private CellController cellController;
    private float[,] m_cells;

    public Grid(CellController cellController, float defaultValue = 0f) {
        this.cellController = cellController;
        // Generate new `m_cells` based on `cellController`'s dimensions
        m_cells = new float[this.cellController.dimensions.x,this.cellController.dimensions.y];
        // We pre-fill `m_cells` with a default value of 0f
        for(int x = 0; x < m_cells.GetLength(0); x++) {
            for(int y = 0; y < m_cells.GetLength(1); y++) {
                m_cells[x,y] = defaultValue;
            }
        }
    }

    public bool SetCellValue(Vector2Int coords, float newVal) {
        if (coords.x < 0 || coords.x >= m_cells.GetLength(0) || coords.y < 0 || coords.y >= m_cells.GetLength(1)) {
            return false;
        }
        m_cells[coords.x, coords.y] = newVal;
        return true;
    } 

    public float GetCellValue(Vector2Int coords) {
        if (coords.x < 0 || coords.x >= m_cells.GetLength(0) || coords.y < 0 || coords.y >= m_cells.GetLength(1)) {
            // -1 == this isn't a valid cell coordinate
            return -1;
        }
        return m_cells[coords.x, coords.y];
    }

}
