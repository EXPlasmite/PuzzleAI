using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
    // Origin set to bottom left corner of dungeon tilemap in world space
    public Vector2 gridOrigin = new Vector2(-3.56f, 24.95f);
    // Width extended to 45 to cover full arena including open pushing area
    public int gridWidth = 45;
    public int gridHeight = 22;

    private bool[,] walkable;

    // Initialise grid with all cells set to walkable
    // Called at the start of each episode before maze walls are spawned
    public void InitialiseGrid()
    {
        walkable = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                walkable[x, y] = true;
    }

    // Mark a cell as walkable or unwalkable
    // Called by MazeGenerator to mark wall positions
    public void SetWalkable(int x, int y, bool value)
    {
        if (InBounds(x, y))
            walkable[x, y] = value;
    }

    // Returns true if a cell is within bounds and not a wall
    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return walkable[x, y];
    }

    // Check if grid coordinates are within the grid bounds
    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    // Convert grid coordinates to world space position
    // Offset by half a cell to centre on the tile
    public Vector2 GridToWorld(int x, int y)
    {
        return new Vector2(
            gridOrigin.x + x * cellSize + cellSize * 0.5f,
            gridOrigin.y + y * cellSize + cellSize * 0.5f
        );
    }

    // Convert world space position to grid coordinates
    // Used by A* to map agent and box positions onto the grid
    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public int Width => gridWidth;
    public int Height => gridHeight;
}