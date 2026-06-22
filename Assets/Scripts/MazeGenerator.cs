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

    // Generate a new maze layout and spawn wall prefabs
    // Called at the start of every episode to create a unique environment
    public void GenerateMaze()
    {
        ClearMaze();
        gridSystem.InitialiseGrid();

        mazeGrid = new bool[mazeWidth, mazeHeight];

        // Fill everything with walls initially
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                mazeGrid[x, y] = true;

        // Carve paths using recursive backtracking algorithm
        // Starts at (1,1) and carves passages until all cells are visited
        CarvePassage(1, 1);

        // Always ensure agent start position is clear
        mazeGrid[1, 1] = false;

        // Create clear exit opening on right side of maze at mid height
        // Ensures consistent exit point for box placement
        int exitY = mazeHeight / 2;
        mazeGrid[mazeWidth - 1, exitY] = false;
        mazeGrid[mazeWidth - 2, exitY] = false;
        mazeGrid[mazeWidth - 3, exitY] = false;

        // Carve corridor from maze interior to exit if blocked
        int innerX = mazeWidth - 4;
        while (innerX > 0 && mazeGrid[innerX, exitY])
        {
            mazeGrid[innerX, exitY] = false;
            innerX--;
        }

        // Spawn wall prefabs and mark cells as unwalkable in GridSystem
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

    // Recursive backtracking maze carving algorithm
    // Randomly visits unvisited neighbours and carves passages between them
    private void CarvePassage(int x, int y)
    {
        mazeGrid[x, y] = false;

        // Four possible directions - move 2 cells to leave a wall between passages
        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };

        // Shuffle directions to ensure random maze layout each episode
        Shuffle(directions);

        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            // Only carve if neighbour is within bounds and unvisited
            if (nx > 0 && nx < mazeWidth - 1 && ny > 0 && ny < mazeHeight - 1 && mazeGrid[nx, ny])
            {
                // Carve the wall between current cell and neighbour
                mazeGrid[x + dir.x / 2, y + dir.y / 2] = false;
                CarvePassage(nx, ny);
            }
        }
    }

    // Fisher-Yates shuffle to randomise direction order each passage carve
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

    // Destroy all spawned wall objects and clear the list
    // Called at the start of each episode before generating a new maze
    public void ClearMaze()
    {
        foreach (GameObject wall in spawnedWalls)
            if (wall != null) Destroy(wall);
        spawnedWalls.Clear();
    }

    // Returns world position of agent start cell - bottom left of maze
    public Vector2 GetStartPosition()
    {
        return gridSystem.GridToWorld(1, 1);
    }

    // Returns world position just outside maze exit for box placement
    // Gives agent room to approach box
    public Vector2 GetExitPosition()
    {
        int exitY = mazeHeight / 2;
        return gridSystem.GridToWorld(mazeWidth + 2, exitY);
    }
}