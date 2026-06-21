using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

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
    public float boundsMinX = -3f;
    public float boundsMaxX = 42f;
    public float boundsMinY = 24f;
    public float boundsMaxY = 47f;

    private Rigidbody2D rb;
    private Rigidbody2D boxRb;

    private bool phase1Complete = false;

    private List<Vector2> currentPath;
    private int currentWaypointIndex = 0;

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
        mazeGenerator.GenerateMaze();

        transform.position = mazeGenerator.GetStartPosition();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        box.position = mazeGenerator.GetExitPosition();
        boxRb.velocity = Vector2.zero;
        boxRb.angularVelocity = 0f;

        float goalX = Random.Range(25f, 40f);
        float goalY = Random.Range(26f, 45f);
        goal.position = new Vector3(goalX, goalY, 0f);

        phase1Complete = false;
        touchedBox = false;
        stuckCounter = 0;
        lastBoxPosition = box.position;
        prevBoxToGoal = goal.position - box.position;

        CalculatePathToBox();
    }

    private void CalculatePathToBox()
    {
        currentPath = pathfinder.FindPath(transform.position, box.position);
        currentWaypointIndex = 0;

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("No path found to box!");
            phase1Complete = true;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(box.position - transform.position);
        sensor.AddObservation(goal.position - box.position);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(boxRb.velocity);
        sensor.AddObservation(phase1Complete ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!phase1Complete)
        {
            FollowPath();

            float distToBox = Vector2.Distance(transform.position, box.position);
            if (distToBox < 0.8f)
            {
                phase1Complete = true;
                AddReward(1f);
            }
        }
        else
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            rb.velocity = new Vector2(moveX, moveY) * moveSpeed;

            Vector2 agentToBox = box.position - transform.position;
            Vector2 boxToGoal = goal.position - box.position;

            float agentToBoxDist = agentToBox.magnitude;
            float boxToGoalDist = boxToGoal.magnitude;

            if (!touchedBox)
            {
                AddReward(-agentToBoxDist * 0.01f);
                if (agentToBoxDist < 0.6f)
                {
                    touchedBox = true;
                    AddReward(0.5f);
                }
            }

            float goalReward = (prevBoxToGoal.magnitude - boxToGoalDist) * 0.2f;
            AddReward(Mathf.Clamp(goalReward, -0.2f, 0.2f));
            prevBoxToGoal = boxToGoal;

            AddReward(-0.0002f);

            // Wall proximity penalty using dungeon bounds
            float distToMinX = box.position.x - boundsMinX;
            float distToMaxX = boundsMaxX - box.position.x;
            float distToMinY = box.position.y - boundsMinY;
            float distToMaxY = boundsMaxY - box.position.y;
            float minWallDist = Mathf.Min(distToMinX, distToMaxX, distToMinY, distToMaxY);

            if (minWallDist < 2f)
                AddReward(-0.1f * (1f - minWallDist / 2f));

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

            if (boxToGoalDist < 0.8f)
            {
                AddReward(10f);
                EndEpisode();
            }

            // Out of bounds check using dungeon bounds
            if (box.position.x < boundsMinX || box.position.x > boundsMaxX ||
                box.position.y < boundsMinY || box.position.y > boundsMaxY)
            {
                AddReward(-2f);
                EndEpisode();
            }

            rb.velocity = Vector2.ClampMagnitude(rb.velocity, moveSpeed);
        }
    }

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

        if (Vector2.Distance(transform.position, target) < waypointReachDistance)
            currentWaypointIndex++;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}