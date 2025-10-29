using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SteeringBehaviors))]
public class PursueBehavior : MonoBehaviour
{
    /// <summary>
    /// Maximum prediction time the pursue will predict in the future
    /// </summary>
    public float maxPrediction = 1f;

    Rigidbody rb;
    SteeringBehaviors steeringBehaviors;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        steeringBehaviors = GetComponent<SteeringBehaviors>();
    }

    public Vector3 GetSteering(Rigidbody target)
    {
        /* Calculate the distance to the target */
        Vector3 displacement = target.position - transform.position;
        float distance = displacement.magnitude;

        /* Get the character's speed */
        float speed = rb.velocity.magnitude;

        /* Calculate the prediction time */
        float prediction;
        if (speed <= distance / maxPrediction)
        {
            prediction = maxPrediction;
        }
        else
        {
            prediction = distance / speed;
        }

        /* Put the target together based on where we think the target will be */
        Vector3 explicitTarget = target.position + target.velocity * prediction;

        //Debug.DrawLine(transform.position, explicitTarget);

        return steeringBehaviors.Seek(explicitTarget);
    }
}

