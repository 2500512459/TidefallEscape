using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SeparationBehavior : MonoBehaviour
{
    public float separationMaxAcceleration = 25;

    public float separationMaxDistance = 1f;

    NearbySensor nearby;

    // Start is called before the first frame update
    void Start()
    {
        GameObject nearbyObj = new GameObject("NearbySendor");

        nearbyObj.transform.SetParent(transform);
        nearbyObj.transform.localPosition = Vector3.zero;

        SphereCollider collider = nearbyObj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = separationMaxDistance;

        nearby = nearbyObj.AddComponent<NearbySensor>();

    }

    public Vector3 GetSteering()
    {
        Vector3 acceleration = Vector3.zero;

        foreach (Rigidbody r in nearby.targets)
        {
            Vector3 direction = transform.position - r.transform.position;
            float dist = direction.magnitude;

            if (dist < separationMaxDistance)
            {
                var strength = separationMaxAcceleration * (separationMaxDistance - dist) / separationMaxDistance;

                direction.Normalize();
                acceleration += direction * strength;
            }
        }

        return acceleration;
    }
}
