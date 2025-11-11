using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageManager : MonoSingleton<DamageManager>
{
    private void OnEnable()
    {
        EventManager.Listen<DamageMessage>(this, OnDamage);
    }
    private void OnDisable()
    {
        EventManager.Unlisten<DamageMessage>(this);
    }
    private void OnDamage(DamageMessage msg)
    {
        Debug.Log("OnDamage:" + msg.Damage);
        Character target = msg.Target;
        if (target != null)
        {
            target.TakeDamage(msg.Damage);
        }
    }
}
