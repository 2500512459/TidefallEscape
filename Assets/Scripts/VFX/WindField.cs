using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WindField : MonoBehaviour
{
    public Camera Camera;
    public float offsetY;
    public float rotation;

    [Range(0, 20)]
    public float density = 8f;

    ParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
        if (Camera == null)
        {
            Camera = Camera.main;
        }

        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.transform.position + new Vector3(0, offsetY, 0);
        transform.rotation = Quaternion.Euler(0, rotation, 0);

        if (ps != null)
        {
            var emissionModule = ps.emission;
            emissionModule.rateOverTime = density;
        }
    }
}

