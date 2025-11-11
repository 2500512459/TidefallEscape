using System.Collections;
using UnityEngine;

public class NPCAnimationController : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        PlayIdle();
    }

    /// <summary>
    /// 播放待机动画
    /// </summary>
    public void PlayIdle()
    {
        animator.Play("Idle");
        animator.SetBool("isIdle", true);
    }

    /// <summary>
    /// 播放受击动画
    /// </summary>
    public void PlayHit()
    {
        animator.SetTrigger("TiggerHit");
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDeath()
    {
        animator.SetBool("isDeath", true);
        animator.SetBool("isIdle", false);
    }

}
