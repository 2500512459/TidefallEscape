using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SteeringBehaviors))]
public class WanderBehavior : MonoBehaviour
{
    public Vector2 targetChangeRange = new Vector2(2.0f, 6.0f);

    public float wanderRadius = 1.2f;

    public float targetHeight = -10;

    Vector3 targetPosition;

    SteeringBehaviors steeringBehaviors;

    Rigidbody rb;

    void Awake()
    {
        steeringBehaviors = GetComponent<SteeringBehaviors>();

        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(TargetPositionChange());
    }

    public Vector3 GetSteering()
    {
        Debug.DrawLine(transform.position, targetPosition, Color.gray);

        return steeringBehaviors.Seek(targetPosition);
    }

    IEnumerator TargetPositionChange()
    {
        while (true)
        {
            Vector3 wanderTarget;

            float theta = Random.value * 2 * Mathf.PI;
            /* Create a vector to a target position on the wander circle */
            wanderTarget = new Vector3(wanderRadius * Mathf.Cos(theta), 0f, wanderRadius * Mathf.Sin(theta));

            wanderTarget.Normalize();
            wanderTarget *= wanderRadius;

            targetPosition = transform.position + wanderTarget;

            targetPosition.y = targetHeight;

            yield return new WaitForSeconds(Random.Range(targetChangeRange.x, targetChangeRange.y));
        }
    }
}
