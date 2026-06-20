using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PushAgent : Agent
{
    public Transform box;
    public Transform goal;

    public float moveSpeed = 6f;

    private Rigidbody2D rb;
    private Rigidbody2D boxRb;

    private Vector2 prevBoxToGoal;
    private bool touchedBox;

    private Vector3 startAgentPos;
    private Vector3 startBoxPos;

    private Vector2 lastBoxPosition;
    private int stuckCounter = 0;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        boxRb = box.GetComponent<Rigidbody2D>();
        startAgentPos = transform.position;
        startBoxPos = box.position;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = startAgentPos;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector3 randomOffset = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            0f);

        Vector3 newBoxPos = startBoxPos + randomOffset;
        newBoxPos.x = Mathf.Clamp(newBoxPos.x, -7f, 7f);
        newBoxPos.y = Mathf.Clamp(newBoxPos.y, -7f, 7f);
        box.position = newBoxPos;

        boxRb.velocity = Vector2.zero;
        boxRb.angularVelocity = 0f;

        touchedBox = false;
        prevBoxToGoal = goal.position - box.position;
        lastBoxPosition = box.position;
        stuckCounter = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(box.position - transform.position);
        sensor.AddObservation(goal.position - box.position);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(boxRb.velocity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        rb.velocity = new Vector2(moveX, moveY) * moveSpeed;

        Vector2 agentToBox = box.position - transform.position;
        Vector2 boxToGoal = goal.position - box.position;

        float agentToBoxDist = agentToBox.magnitude;
        float boxToGoalDist = boxToGoal.magnitude;

        // Encourage reaching the box
        if (!touchedBox)
        {
            AddReward(-agentToBoxDist * 0.01f);
            if (agentToBoxDist < 0.6f)
            {
                touchedBox = true;
                AddReward(0.5f);
            }
        }

        // Reward moving box closer to goal
        float goalReward = (prevBoxToGoal.magnitude - boxToGoalDist) * 0.2f;
        AddReward(Mathf.Clamp(goalReward, -0.2f, 0.2f));
        prevBoxToGoal = boxToGoal;

        // Step penalty
        AddReward(-0.0002f);

        // Graduated wall proximity penalty
        float boxDistToWallX = 8.5f - Mathf.Abs(box.position.x);
        float boxDistToWallY = 8.5f - Mathf.Abs(box.position.y);
        float minWallDist = Mathf.Min(boxDistToWallX, boxDistToWallY);

        if (minWallDist < 2f)
        {
            AddReward(-0.1f * (1f - minWallDist / 2f));
        }

        // Stuck detection - only after touching box
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

        // Success
        if (boxToGoalDist < 0.8f)
        {
            AddReward(10f);
            EndEpisode();
        }

        // Out of bounds
        if (Mathf.Abs(box.position.x) > 8.5f ||
            Mathf.Abs(box.position.y) > 8.5f)
        {
            AddReward(-2f);
            EndEpisode();
        }

        rb.velocity = Vector2.ClampMagnitude(rb.velocity, moveSpeed);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}