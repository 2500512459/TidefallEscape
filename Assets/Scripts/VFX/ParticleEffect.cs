using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ParticleEffect : MonoBehaviour
{
    public bool needAutoRecycle = true;
    public float maxDuration = 10;

    List<ParticleSystem> particles = new List<ParticleSystem>();

    void Awake()
    {
        foreach (ParticleSystem ps in gameObject.GetComponentsInChildren<ParticleSystem>())
        {
            particles.Add(ps);
        }
    }

    private void OnEnable()
    {
        if (needAutoRecycle)
            StartCoroutine(DelayedRecycle());
    }

    void Start()
    {
        Play();
    }

    void Update()
    {
        if (needAutoRecycle && AllParticleSystemsStopped())
        {
            gameObject.Recycle();
        }
    }

    IEnumerator DelayedRecycle()
    {
        yield return new WaitForSeconds(maxDuration);

        gameObject.Recycle();
    }

    public void Play()
    {
        foreach (ParticleSystem ps in particles)
        {
            ps.Play();
        }
    }

    public void Stop()
    {
        foreach (ParticleSystem ps in particles)
        {
            ps.Stop();
        }
    }

    bool AllParticleSystemsStopped()
    {
        foreach (ParticleSystem ps in particles)
        {
            if (ps.isPlaying)
            {
                return false;
            }
        }

        return true;
    }
}

