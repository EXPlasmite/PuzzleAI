using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    [Header("References")]
    public GridSystem gridSystem;
    public GameObject wallPrefab;

    [Header("Maze Settings")]
    public int mazeWidth = 21;
    public int mazeHeight = 21;

    private List<GameObject> spawnedWalls = new List<GameObject>();
    private bool[,] mazeGrid;

    public void GenerateMaze()
    {
        ClearMaze();
        gridSystem.InitialiseGrid();

        mazeGrid = new bool[mazeWidth, mazeHeight];

        // Fill everything with walls
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                mazeGrid[x, y] = true;

        // Carve paths using recursive backtracking
        CarvePassage(1, 1);

        // Always ensure start is clear
        mazeGrid[1, 1] = false;

        // Create clear exit on right side of maze
        int exitY = mazeHeight / 2;
        mazeGrid[mazeWidth - 1, exitY] = false;
        mazeGrid[mazeWidth - 2, exitY] = false;
        mazeGrid[mazeWidth - 3, exitY] = false;

        // Carve path from maze interior to exit
        int innerX = mazeWidth - 4;
        while (innerX > 0 && mazeGrid[innerX, exitY])
        {
            mazeGrid[innerX, exitY] = false;
            innerX--;
        }

        // Spawn walls and update grid system
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (mazeGrid[x, y])
                {
                    Vector2 worldPos = gridSystem.GridToWorld(x, y);
                    GameObject wall = Instantiate(wallPrefab, worldPos, Quaternion.identity);
                    spawnedWalls.Add(wall);
                    gridSystem.SetWalkable(x, y, false);
                }
            }
        }
    }

    private void CarvePassage(int x, int y)
    {
        mazeGrid[x, y] = false;

        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };

        Shuffle(directions);

        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            if (nx > 0 && nx < mazeWidth - 1 && ny > 0 && ny < mazeHeight - 1 && mazeGrid[nx, ny])
            {
                mazeGrid[x + dir.x / 2, y + dir.y / 2] = false;
                CarvePassage(nx, ny);
            }
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public void ClearMaze()
    {
        foreach (GameObject wall in spawnedWalls)
            if (wall != null) Destroy(wall);
        spawnedWalls.Clear();
    }

    public Vector2 GetStartPosition()
    {
        return gridSystem.GridToWorld(1, 1);
    }

    public Vector2 GetExitPosition()
    {
        int exitY = mazeHeight / 2;
        return gridSystem.GridToWorld(mazeWidth + 2, exitY);
    }
}