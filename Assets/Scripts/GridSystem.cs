using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
    public Vector2 gridOrigin = new Vector2(-3.56f, 24.95f);
    public int gridWidth = 22;
    public int gridHeight = 22;

    private bool[,] walkable;

    public void InitialiseGrid()
    {
        walkable = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                walkable[x, y] = true;
    }

    public void SetWalkable(int x, int y, bool value)
    {
        if (InBounds(x, y))
            walkable[x, y] = value;
    }

    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return walkable[x, y];
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public Vector2 GridToWorld(int x, int y)
    {
        return new Vector2(
            gridOrigin.x + x * cellSize + cellSize * 0.5f,
            gridOrigin.y + y * cellSize + cellSize * 0.5f
        );
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public int Width => gridWidth;
    public int Height => gridHeight;
}