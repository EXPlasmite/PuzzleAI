using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinder : MonoBehaviour
{
    private GridSystem grid;

    public void Initialise(GridSystem gridSystem)
    {
        grid = gridSystem;
    }

    public List<Vector2> FindPath(Vector2 startWorld, Vector2 targetWorld)
    {
        Vector2Int start = grid.WorldToGrid(startWorld);
        Vector2Int target = grid.WorldToGrid(targetWorld);

        if (!grid.InBounds(start.x, start.y) || !grid.InBounds(target.x, target.y))
            return null;

        if (!grid.IsWalkable(target.x, target.y))
            return null;

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start, null, 0, Heuristic(start, target));
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node current = GetLowestF(openList);

            if (current.position == target)
                return ReconstructPath(current);

            openList.Remove(current);
            closedSet.Add(current.position);

            foreach (Vector2Int neighbour in GetNeighbours(current.position))
            {
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
                    existing.g = g;
                    existing.parent = current;
                }
            }
        }

        return null;
    }

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

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Node GetLowestF(List<Node> list)
    {
        Node lowest = list[0];
        foreach (Node n in list)
            if (n.F < lowest.F) lowest = n;
        return lowest;
    }

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

    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public float g;
        public float h;
        public float F => g + h;

        public Node(Vector2Int pos, Node parent, float g, float h)
        {
            position = pos;
            this.parent = parent;
            this.g = g;
            this.h = h;
        }
    }
}