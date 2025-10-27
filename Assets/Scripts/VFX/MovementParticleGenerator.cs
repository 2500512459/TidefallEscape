using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MovementParticleGenerator : MonoBehaviour
{
    public float speedEmitRate = 10;

    [Range(0.1f, 10)]
    public float maxFadeDepth = 2;

    ParticleSystem ps;
    Vector3 oldPosition = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        if (ps != null)
        {
            var mainModule = ps.main;
            mainModule.loop = true;
        }

        oldPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (ps != null)
        {
            Vector3 horizonMovement = transform.position - oldPosition;
            horizonMovement.y = 0;

            float submergeFade = Mathf.Clamp01(1 - Mathf.Abs(transform.position.y / maxFadeDepth));

            float speed = horizonMovement.magnitude / Time.deltaTime;
            float emitRate = speedEmitRate * speed * submergeFade;
            var emissionModule = ps.emission;
            emissionModule.rateOverTime = emitRate;

            oldPosition = transform.position;
        }
    }
}


