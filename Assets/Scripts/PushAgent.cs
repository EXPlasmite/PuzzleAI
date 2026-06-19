using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PushAgent : Agent
{
    [Header("References")]
    public Transform box;
    public Transform goal;
    
    [Header("Settings")]
    public float moveSpeed = 5f;
    
    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 boxStartPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.localPosition;
        boxStartPosition = box.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition;
        rb.velocity = Vector2.zero;
        box.localPosition = boxStartPosition;
        box.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((Vector2)transform.localPosition);
        sensor.AddObservation((Vector2)box.localPosition);
        sensor.AddObservation((Vector2)goal.localPosition);
        Vector2 dirToGoal = ((Vector2)goal.localPosition - (Vector2)box.localPosition).normalized;
        sensor.AddObservation(dirToGoal);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        rb.velocity = new Vector2(moveX, moveY) * moveSpeed;

        AddReward(-0.001f);

        float distanceBoxToGoal = Vector2.Distance(box.localPosition, goal.localPosition);
        if (distanceBoxToGoal < 0.5f)
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}