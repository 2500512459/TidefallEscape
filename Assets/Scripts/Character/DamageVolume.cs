using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageVolume : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
    }
    private void OnTriggerEnter(Collider other)
    {
        //AttributesModule am = other.gameObject.GetComponent<AttributesModule>();
        Character am = other.gameObject.GetComponent<Character>();
        if (am != null)
        {
            Debug.Log("触发伤害");
            EventManager.Raise<DamageMessage>(new DamageMessage(10, am));
        }
    }
}