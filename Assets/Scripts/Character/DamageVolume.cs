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
        // 排除玩家：检查 Tag 或者检查 Layer
        if (other.CompareTag("PlayerShip") || other.gameObject.layer == LayerMask.NameToLayer("PlayerShip")) 
            return;
        //AttributesModule am = other.gameObject.GetComponent<AttributesModule>();
        Character am = other.gameObject.GetComponent<Character>();
        if (am != null)
        {
            EventManager.Raise<DamageMessage>(new DamageMessage(10, am));
        }
    }
}