using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private Transform vfxHit1;
    [SerializeField] private Transform vfxHit2;
    private Rigidbody rb;
    private bool isRecycling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void OnEnable()
    {
        isRecycling = false;
        float speed = 40f;
        if (rb != null)
        {
            rb.velocity = Vector3.zero; // 重置速度，防止累积
            rb.velocity = transform.forward * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRecycling) return;

        // 排除玩家：检查 Tag 或者检查 Layer
        if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Player")) 
            return;


        Transform vfxPrefab = (other.GetComponent<BulletTarget>() != null) ? vfxHit1 : vfxHit2;
        Transform vfxInstance = Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        Character target = other.gameObject.GetComponent<Character>();
        if (target != null)
        {
            EventManager.Raise<DamageMessage>(new DamageMessage(10, target));
        }
        Destroy(vfxInstance.gameObject, 0.2f);
        gameObject.Recycle();
    }
}
