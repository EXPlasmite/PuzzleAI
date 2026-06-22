using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinder : MonoBehaviour
{
    private GridSystem grid;

    // Initialise with a reference to the shared GridSystem
    public void Initialise(GridSystem gridSystem)
    {
        grid = gridSystem;
    }

    // Find optimal path from start to target in world space
    // Returns list of world space waypoints or null if no path exists
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 targetWorld)
    {
        // Convert world positions to grid coordinates
        Vector2Int start = grid.WorldToGrid(startWorld);
        Vector2Int target = grid.WorldToGrid(targetWorld);

        // Return null if start or target are outside grid bounds
        if (!grid.InBounds(start.x, start.y) || !grid.InBounds(target.x, target.y))
            return null;

        // Return null if target cell is a wall
        if (!grid.IsWalkable(target.x, target.y))
            return null;

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start, null, 0, Heuristic(start, target));
        openList.Add(startNode);

        // A* main loop - explore nodes until target is reached or no path exists
        while (openList.Count > 0)
        {
            // Always process the node with lowest f(n) = g(n) + h(n)
            Node current = GetLowestF(openList);

            // Target reached - reconstruct and return the path
            if (current.position == target)
                return ReconstructPath(current);

            openList.Remove(current);
            closedSet.Add(current.position);

            foreach (Vector2Int neighbour in GetNeighbours(current.position))
            {
                // Skip already evaluated nodes and walls
                if (closedSet.Contains(neighbour)) continue;
                if (!grid.IsWalkable(neighbour.x, neighbour.y)) continue;

                float g = current.g + 1f;
                float h = Heuristic(neighbour, target);
                Node neighbourNode = new Node(neighbour, current, g, h);

                Node existing = openList.Find(n => n.position == neighbour);
                if (existing == null)
                    openList.Add(neighbourNode);
                else if (g < existing.g)
                {
                    // Found a cheaper path to this node - update it
                    existing.g = g;
                    existing.parent = current;
                }
            }
        }

        // No path found
        return null;
    }

    // Trace back from target node to start and return as ordered list of world positions
    private List<Vector2> ReconstructPath(Node endNode)
    {
        List<Vector2> path = new List<Vector2>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(grid.GridToWorld(current.position.x, current.position.y));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    // Manhattan distance heuristic - counts horizontal and vertical steps to target
    // Chosen for 4-directional grid movement
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Return the node with the lowest combined f score from the open list
    private Node GetLowestF(List<Node> list)
    {
        Node lowest = list[0];
        foreach (Node n in list)
            if (n.F < lowest.F) lowest = n;
        return lowest;
    }

    // Return the four cardinal neighbours of a grid position
    private List<Vector2Int> GetNeighbours(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(pos.x + 1, pos.y),
            new Vector2Int(pos.x - 1, pos.y),
            new Vector2Int(pos.x, pos.y + 1),
            new Vector2Int(pos.x, pos.y - 1)
        };
    }

    // Node class representing a cell in the pathfinding grid
    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public float g; // Cost from start to this node
        public float h; // Estimated cost from this node to target
        public float F => g + h; // Total estimated cost

        public Node(Vector2Int pos, Node parent, float g, float h)
        {
            position = pos;
            this.parent = parent;
            this.g = g;
            this.h = h;
        }
    }
}