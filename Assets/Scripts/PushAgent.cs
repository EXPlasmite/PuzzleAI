using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

// Core hybrid AI system authored by Ty Bingham
// Reward shaping values, phase switching logic and boundary handling developed through
// 28 training iterations by Ty Bingham with structural assistance from Claude (Anthropic)

public class PushAgent : Agent
{
    [Header("References")]
    public Transform box;
    public Transform goal;
    public MazeGenerator mazeGenerator;
    public AStarPathfinder pathfinder;
    public GridSystem gridSystem;

    [Header("Settings")]
    public float moveSpeed = 6f;
    public float pathFollowSpeed = 5f;
    public float waypointReachDistance = 0.5f;

    [Header("Dungeon Bounds")]
    // Bounds set to match actual dungeon tilemap world coordinates
    public float boundsMinX = -3f;
    public float boundsMaxX = 42f;
    public float boundsMinY = 24f;
    public float boundsMaxY = 47f;

    [Header("Maze Boundary")]
    // X coordinate of maze exit - used to detect agent re-entering maze in Phase 2
    public float mazeExitX = 18f;

    private Rigidbody2D rb;
    private Rigidbody2D boxRb;

    // Tracks which phase the agent is currently in
    private bool phase1Complete = false;

    // A* pathfinding waypoints for Phase 1
    private List<Vector2> currentPath;
    private int currentWaypointIndex = 0;

    // Reward tracking variables for Phase 2
    private Vector2 prevBoxToGoal;
    private bool touchedBox;
    private Vector2 lastBoxPosition;
    private int stuckCounter = 0;

    private Vector3 startAgentPos;
    private Vector3 startBoxPos;
    private Vector3 startGoalPos;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        boxRb = box.GetComponent<Rigidbody2D>();
        pathfinder.Initialise(gridSystem);

        startAgentPos = transform.position;
        startBoxPos = box.position;
        startGoalPos = goal.position;
    }

    public override void OnEpisodeBegin()
    {
        // Generate a new maze layout every episode
        mazeGenerator.GenerateMaze();

        // Reset agent to maze start position
        transform.position = mazeGenerator.GetStartPosition();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Place box at maze exit
        box.position = mazeGenerator.GetExitPosition();
        boxRb.velocity = Vector2.zero;
        boxRb.angularVelocity = 0f;

        // Randomise goal position in open area
        // Range kept relatively tight to help agent find goal during training
        float goalX = Random.Range(27f, 38f);
        float goalY = Random.Range(30f, 40f);
        goal.position = new Vector3(goalX, goalY, 0f);

        // Reset all phase and reward tracking variables
        phase1Complete = false;
        touchedBox = false;
        stuckCounter = 0;
        lastBoxPosition = box.position;
        prevBoxToGoal = goal.position - box.position;

        // Calculate A* path to box at episode start
        CalculatePathToBox();
    }

    // Calculate A* path from agent to box and store waypoints
    private void CalculatePathToBox()
    {
        Vector2Int boxGrid = gridSystem.WorldToGrid(box.position);

        currentPath = pathfinder.FindPath(transform.position, box.position);
        currentWaypointIndex = 0;

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("No path found to box!");
            // Skip to Phase 2 if no path found
            phase1Complete = true;
        }
    }

    // 11 observations provided to the neural network
    // All relative positions for coordinate independence
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(box.position - transform.position);  // Vector3 - box relative to agent
        sensor.AddObservation(goal.position - box.position);       // Vector3 - goal relative to box
        sensor.AddObservation(rb.velocity);                        // Vector2 - agent velocity
        sensor.AddObservation(boxRb.velocity);                     // Vector2 - box velocity
        sensor.AddObservation(phase1Complete ? 1f : 0f);          // float - phase flag
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!phase1Complete)
        {
            // Phase 1 - follow A* waypoints to reach the box
            FollowPath();

            float distToBox = Vector2.Distance(transform.position, box.position);
            if (distToBox < 0.8f)
            {
                // Phase 1 complete - hand off to ML-Agents
                // touchedBox set true here to skip redundant re-touch check in Phase 2
                phase1Complete = true;
                touchedBox = true;
                AddReward(1f);
            }
        }
        else
        {
            // Phase 2 - ML-Agents controls agent to push box to goal
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            rb.velocity = new Vector2(moveX, moveY) * moveSpeed;

            Vector2 agentToBox = box.position - transform.position;
            Vector2 boxToGoal = goal.position - box.position;

            float agentToBoxDist = agentToBox.magnitude;
            float boxToGoalDist = boxToGoal.magnitude;

            // Encourage agent to reach box if not yet touched
            if (!touchedBox)
            {
                AddReward(-agentToBoxDist * 0.01f);
                if (agentToBoxDist < 0.6f)
                {
                    touchedBox = true;
                    AddReward(0.5f);
                }
            }

            // Reward proportional to box movement toward goal each step
            // Multiplier and clamp values tuned across training runs
            float goalReward = (prevBoxToGoal.magnitude - boxToGoalDist) * 0.5f;
            AddReward(Mathf.Clamp(goalReward, -0.5f, 0.5f));
            prevBoxToGoal = boxToGoal;

            // Small step penalty to encourage efficiency
            AddReward(-0.0001f);

            // Penalty for agent re-entering maze during Phase 2
            // Introduced after observing agent retreating into maze
            if (transform.position.x < mazeExitX)
            {
                AddReward(-0.05f);
            }

            // End episode if agent goes too deep into maze
            if (transform.position.x < mazeExitX - 3f)
            {
                AddReward(-1f);
                EndEpisode();
            }

            // Proportional wall proximity penalty using actual dungeon bounds
            // Replaces hardcoded bounds from original simple scene
            float distToMinX = box.position.x - boundsMinX;
            float distToMaxX = boundsMaxX - box.position.x;
            float distToMinY = box.position.y - boundsMinY;
            float distToMaxY = boundsMaxY - box.position.y;
            float minWallDist = Mathf.Min(distToMinX, distToMaxX, distToMinY, distToMaxY);

            if (minWallDist < 2f)
                AddReward(-0.1f * (1f - minWallDist / 2f));

            // Stuck detection - end episode if box stops moving after being touched
            if (touchedBox)
            {
                if (Vector2.Distance(box.position, lastBoxPosition) < 0.01f)
                {
                    stuckCounter++;
                    if (stuckCounter > 200)
                    {
                        AddReward(-1f);
                        EndEpisode();
                    }
                }
                else
                {
                    stuckCounter = 0;
                    lastBoxPosition = box.position;
                }
            }

            // Success condition - box reaches goal
            if (boxToGoalDist < 0.8f)
            {
                AddReward(20f);
                EndEpisode();
            }

            // Out of bounds check using dungeon world coordinates
            if (box.position.x < boundsMinX || box.position.x > boundsMaxX ||
                box.position.y < boundsMinY || box.position.y > boundsMaxY)
            {
                AddReward(-2f);
                EndEpisode();
            }

            rb.velocity = Vector2.ClampMagnitude(rb.velocity, moveSpeed);
        }
    }

    // Follow A* waypoints by moving toward each waypoint in sequence
    private void FollowPath()
    {
        if (currentPath == null || currentWaypointIndex >= currentPath.Count)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 target = currentPath[currentWaypointIndex];
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = direction * pathFollowSpeed;

        // Move to next waypoint when close enough
        if (Vector2.Distance(transform.position, target) < waypointReachDistance)
            currentWaypointIndex++;
    }

    // Heuristic for manual testing in Heuristic Only mode
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}