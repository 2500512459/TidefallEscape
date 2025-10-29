using UnityEngine;
using System.Collections.Generic;


[RequireComponent(typeof(Rigidbody))]
public class SteeringBehaviors : MonoBehaviour
{
    [Header("General")]
    public float maxVelocity = 3.5f;
    public float maxAcceleration = 10f;
    public float turnSpeed = 20f;

    [Header("Arrive")]
    public float targetRadius = 0.005f;
    public float slowRadius = 1f;
    public float timeToTarget = 0.1f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Steer(Vector3 linearAcceleration)
    {
        rb.velocity += linearAcceleration * Time.deltaTime;

        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;
        }
    }

    public Vector3 Seek(Vector3 targetPosition, float maxSeekAccel)
    {
        Vector3 acceleration = targetPosition - transform.position;
        acceleration.Normalize();
        acceleration *= maxSeekAccel;
        return acceleration;
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        return Seek(targetPosition, maxAcceleration);
    }

    public void LookMoveDirection()
    {
        Vector3 direction = rb.velocity;

        LookAtDirection(direction);
    }

    public void LookAtDirection(Vector3 direction)
    {
        direction.Normalize();

        if (direction.sqrMagnitude > 0.001f)
        {
            float toRotation = -1 * (Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) + 90;
            float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);

            transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
    }

    public void LookAtDirectionHeadUp(Vector3 direction, float headUp)
    {
        direction.Normalize();

        if (direction.sqrMagnitude > 0.001f)
        {
            float toRotation = -1 * (Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) + 90;
            float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);

            transform.rotation = Quaternion.Euler(-headUp, rotation, 0);
        }
    }

    public void LookAtDirection(Quaternion toRotation)
    {
        LookAtDirection(toRotation.eulerAngles.y);
    }

    public void LookAtDirection(float toRotation)
    {
        float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
        transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        Debug.DrawLine(transform.position, targetPosition, Color.cyan, 0f, false);

        Vector3 targetVelocity = targetPosition - rb.position;

        float dist = targetVelocity.magnitude;

        if (dist < targetRadius)
        {
            rb.velocity = Vector3.zero;
            return Vector3.zero;
        }

        float targetSpeed;
        if (dist > slowRadius)
        {
            targetSpeed = maxVelocity;
        }
        else
        {
            targetSpeed = maxVelocity * (dist / slowRadius);
        }

        targetVelocity.Normalize();
        targetVelocity *= targetSpeed;

        Vector3 acceleration = targetVelocity - rb.velocity;
        acceleration *= 1 / timeToTarget;

        if (acceleration.magnitude > maxAcceleration)
        {
            acceleration.Normalize();
            acceleration *= maxAcceleration;
        }

        return acceleration;
    }

    public Vector3 Interpose(Rigidbody target1, Rigidbody target2)
    {
        Vector3 midPoint = (target1.position + target2.position) / 2;

        float timeToReachMidPoint = Vector3.Distance(midPoint, transform.position) / maxVelocity;

        Vector3 futureTarget1Pos = target1.position + target1.velocity * timeToReachMidPoint;
        Vector3 futureTarget2Pos = target2.position + target2.velocity * timeToReachMidPoint;

        midPoint = (futureTarget1Pos + futureTarget2Pos) / 2;

        return Arrive(midPoint);
    }

    public bool IsInFront(Vector3 target)
    {
        return IsFacing(target, 0);
    }

    public bool IsFacing(Vector3 target, float cosineValue)
    {
        Vector3 facing = transform.right.normalized;
        Vector3 directionToTarget = (target - transform.position).normalized;
        return Vector3.Dot(facing, directionToTarget) >= cosineValue;
    }

    // The rest of the static methods remain unchanged
    public static Vector3 OrientationToVector(float orientation)
    {
        /* Mulitply the orientation by -1 because counter clockwise on the y-axis is in the negative
             * direction, but Cos And Sin expect clockwise orientation to be the positive direction */
        return new Vector3(Mathf.Cos(-orientation), 0, Mathf.Sin(-orientation));
    }

    public static float VectorToOrientation(Vector3 direction)
    {
        /* Mulitply by -1 because counter clockwise on the y-axis is in the negative direction */
        return -1 * Mathf.Atan2(direction.z, direction.x);
    }
}
