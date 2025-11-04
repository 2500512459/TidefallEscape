using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CannonBall : MonoBehaviour
{
    public float speed;
    public float collisionRecycleDelta = 0.5f;
    Vector3 direction = Vector3.up;
    Rigidbody rb;
    MeshRenderer mr;
    bool live = true;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponentInChildren<MeshRenderer>();
    }
    private void OnEnable()
    {
        live = true;
        ShowRenderer();
    }
    void ShowRenderer()
    {
        if (mr != null)
        {
            mr.enabled = true;
        }
    }
    void HideRenderer()
    {
        if (mr != null)
        {
            mr.enabled = false;
        }
    }
    public void Launch(Vector3 dir)
    {
        direction = dir.normalized;
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }
    }
    private void FixedUpdate()
    {
        if (!live) return;
        float waterHeight = Water.Instance.GetWaterHeight(transform.position);
        if (waterHeight > transform.position.y)
        {
            //collision
            HandleWaterCollision(transform.position);
        }
    }
    void HandleWaterCollision(Vector3 position)
    {
        DynamicSplashManager.Instance.MakeSplash(position, 1f);
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
        live = false;
        HideRenderer();
        StartCoroutine(DelayedRecycle(collisionRecycleDelta));
    }
    IEnumerator DelayedRecycle(float delta)
    {
        yield return new WaitForSeconds(delta);
        gameObject.Recycle();
    }
}
