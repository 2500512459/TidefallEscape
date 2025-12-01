using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageVolume : MonoBehaviour
{
    // [Tooltip("设置需要排除的Layer")]
    // public LayerMask excludeLayers;

    private GameObject owner;

    public void Setup(GameObject owner)
    {
        this.owner = owner;
    }

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
        // 检查发射者
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
        {
            return;
        }

        // // 检查是否在排除的 LayerMask 中
        // if (((1 << other.gameObject.layer) & excludeLayers) != 0)
        //     return;

        // // 排除玩家：检查 Tag 或者检查 Layer
        // if (other.CompareTag("PlayerShip") || other.gameObject.layer == LayerMask.NameToLayer("PlayerShip")) 
        //     return;
        //AttributesModule am = other.gameObject.GetComponent<AttributesModule>();
        Character am = other.gameObject.GetComponent<Character>();
        if (am != null)
        {
            EventManager.Raise<DamageMessage>(new DamageMessage(10, am));
        }
    }
}