using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicSplashManager : MonoSingleton<DynamicSplashManager>
{
    public ParticleEffect bigExplosionSplash;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    
    }

    public void MakeSplash(Vector3 position, float scale)
    {
        ParticleEffect splash = bigExplosionSplash.Spawn(position, Quaternion.identity);
        splash.transform.localScale = Vector3.one * scale;
        splash.needAutoRecycle = true;
        splash.maxDuration = 10;
    }
}

