using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NearbySensor : MonoBehaviour
{
    HashSet<Rigidbody> _targets = new HashSet<Rigidbody>();

    public HashSet<Rigidbody> targets
    {
        get
        {
            _targets.RemoveWhere(IsNull);
            return _targets;
        }
    }

    static bool IsNull(Rigidbody r)
    {
        return r == null;
    }

    void TryToAdd(Component other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            _targets.Add(rb);
        }
    }

    void TryToRemove(Component other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            _targets.Remove(rb);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryToAdd(other);
    }

    void OnTriggerExit(Collider other)
    {
        TryToRemove(other);
    }
}
