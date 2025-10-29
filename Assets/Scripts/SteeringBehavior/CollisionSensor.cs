using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CollisionSensor : MonoBehaviour
{
    public float rayStart = 0.5f;
    public float rayLength = 10f;
    public int rayCount = 36;
    public LayerMask collisionLayers;

    //private void Update()
    //{
    //    GetCollisionFreeDirectionOpt(transform.forward);
    //}

    public Vector3 GetCollisionFreeDirection(Vector3 desiredDirection)
    {
        if (desiredDirection == Vector3.zero)
            return Vector3.zero;

        Vector3 bestDirection = Vector3.zero;
        float bestAngle = float.MaxValue;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = 360f / rayCount * i;
            Vector3 direction = transform.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward;

            float dotProduct = Vector3.Dot(desiredDirection, direction);

            if (dotProduct > 0) // Only consider directions in the general desired direction
            {
                RaycastHit hit;
                bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

                if (collision)
                {
                    Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
                }
                else
                {
                    Debug.DrawRay(transform.position, direction * rayLength, Color.green);

                    float angleFromDesired = Vector3.Angle(desiredDirection, direction);
                    if (angleFromDesired < bestAngle)
                    {
                        bestAngle = angleFromDesired;
                        bestDirection = direction;
                    }
                }
            }
        }

        return bestDirection != Vector3.zero ? bestDirection : desiredDirection;
    }

    public Vector3 GetCollisionFreeDirectionOpt(Vector3 desiredDirection)
    {
        if (desiredDirection == Vector3.zero)
            return Vector3.zero;

        Vector3 bestDirection = Vector3.zero;

        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle1 = 360f / rayCount * i;
            Vector3 direction1 = transform.rotation * Quaternion.Euler(0, angle1, 0) * Vector3.forward;

            RaycastHit hit;
            bool collision1 = Physics.Raycast(transform.position, direction1, out hit, rayLength, collisionLayers);

            if (collision1)
            {
                Debug.DrawRay(transform.position, direction1 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction1 * rayLength, Color.green);

                bestDirection = direction1;
                break;
            }

            float angle2 = -360f / rayCount * i;
            Vector3 direction2 = transform.rotation * Quaternion.Euler(0, angle2, 0) * Vector3.forward;

            bool collision2 = Physics.Raycast(transform.position, direction2, out hit, rayLength, collisionLayers);

            if (collision2)
            {
                Debug.DrawRay(transform.position, direction2 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction2 * rayLength, Color.green);

                bestDirection = direction2;
                break;
            }
        }

        return bestDirection != Vector3.zero ? bestDirection : desiredDirection;
    }


    public bool GetCollisionFreeDirection(Vector3 desiredDirection, out Vector3 outDirection)
    {
        desiredDirection.Normalize();

        outDirection = desiredDirection;

        if (desiredDirection == Vector3.zero)
            return false;


        Vector3 bestDirection = Vector3.zero;

        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle1 = 360f / rayCount * i;
            Vector3 direction1 = Quaternion.Euler(0, angle1, 0) * desiredDirection;

            RaycastHit hit;
            bool collision1 = Physics.Raycast(transform.position, direction1, out hit, rayLength, collisionLayers);

            if (collision1)
            {
                Debug.DrawRay(transform.position, direction1 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction1 * rayLength, Color.green);

                bestDirection = direction1;
                break;
            }

            float angle2 = -360f / rayCount * i;
            Vector3 direction2 = Quaternion.Euler(0, angle2, 0) * desiredDirection;

            bool collision2 = Physics.Raycast(transform.position, direction2, out hit, rayLength, collisionLayers);

            if (collision2)
            {
                Debug.DrawRay(transform.position, direction2 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction2 * rayLength, Color.green);

                bestDirection = direction2;
                break;
            }
        }

        if (bestDirection != desiredDirection)
        {
            outDirection = bestDirection;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool GetCollisionFreeDirection2(Vector3 desiredDirection, out Vector3 outDirection)
    {
        desiredDirection.Normalize();

        outDirection = desiredDirection;

        if (desiredDirection == Vector3.zero)
            return false;

        Vector3 bestDirection = Vector3.zero;

        Vector3 bestDirection1 = Vector3.zero;
        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle = 360f / rayCount * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

            if (collision)
            {
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * rayLength, Color.green);

                bestDirection1 = direction;
                break;
            }
        }

        Vector3 bestDirection2 = Vector3.zero;
        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle = -360f / rayCount * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

            if (collision)
            {
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * rayLength, Color.green);

                bestDirection2 = direction;
                break;
            }
        }

        if (Vector3.Dot(transform.forward, bestDirection1) > Vector3.Dot(transform.forward, bestDirection2))
        {
            bestDirection = bestDirection1;
        }
        else
        {
            bestDirection = bestDirection2;
        }

        if (bestDirection != desiredDirection)
        {
            outDirection = bestDirection;
            return true;
        }
        else
        {
            return false;
        }
    }
}

