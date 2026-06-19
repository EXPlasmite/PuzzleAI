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
        // Reset agent position
        transform.localPosition = startPosition;
        rb.velocity = Vector2.zero;
        
        // Reset box position
        box.localPosition = boxStartPosition;
        box.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);
        
        // Box position
        sensor.AddObservation(box.localPosition.x);
        sensor.AddObservation(box.localPosition.y);
        
        // Goal position
        sensor.AddObservation(goal.localPosition.x);
        sensor.AddObservation(goal.localPosition.y);
        
        // Direction from box to goal
        Vector2 dirToGoal = (goal.localPosition - box.localPosition).normalized;
        sensor.AddObservation(dirToGoal.x);
        sensor.AddObservation(dirToGoal.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Move agent
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        
        rb.velocity = new Vector2(moveX, moveY) * moveSpeed;
        
        // Small negative reward each step to encourage efficiency
        AddReward(-0.001f);
        
        // Check if box is on goal
        float distanceBoxToGoal = Vector2.Distance(box.localPosition, goal.localPosition);
        
        if (distanceBoxToGoal < 0.5f)
        {
            AddReward(1.0f);
            EndEpisode();
        }
        
        // Check if agent falls off
        if (Mathf.Abs(transform.localPosition.x) > 5f || 
            Mathf.Abs(transform.localPosition.y) > 5f)
        {
            AddReward(-0.5f);
            EndEpisode();
        }
        
        // Check if box falls off
        if (Mathf.Abs(box.localPosition.x) > 5f || 
            Mathf.Abs(box.localPosition.y) > 5f)
        {
            AddReward(-0.5f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}