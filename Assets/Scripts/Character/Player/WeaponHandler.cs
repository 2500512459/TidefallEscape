using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    public PlayerAttackCheck attackCheck;
    public Transform weaponSocketHip;   // 腰间挂点
    public Transform weaponSocketHand;  // 手部挂点
    public GameObject currentWeapon;

    [Header("攻击特效")]
    [SerializeField] private ParticleSystem attackParticle01;
    [SerializeField] private ParticleSystem attackParticle02;
    [SerializeField] private ParticleSystem attackParticle03;

    void Start()
    {
        AttachToHip();
        StopAllAttackEffects();
    }

    // 供动画事件调用：在拔剑动画中调用
    public void AttachToHand()
    {
        // 将武器挂到手部
        currentWeapon.transform.SetParent(weaponSocketHand, false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    // 供动画事件调用：在收剑动画中调用
    public void AttachToHip()
    {
        // 将武器挂到腰间
        currentWeapon.transform.SetParent(weaponSocketHip, false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    public void Attack1(int damageOverride = 0)
    {
        attackCheck.Attack(damageOverride);
        PlayAttackEffect(attackParticle01);
    }

    public void Attack2(int damageOverride = 0)
    {
        attackCheck.Attack(damageOverride);
        PlayAttackEffect(attackParticle02);
    }

    public void Attack3(int damageOverride = 0)
    {
        attackCheck.Attack(damageOverride);
        PlayAttackEffect(attackParticle03);
    }

    private void PlayAttackEffect(ParticleSystem particle)
    {
        if (particle == null) return;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }
    private void StopAllAttackEffects()
    {
        if (attackParticle01 != null) attackParticle01.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (attackParticle02 != null) attackParticle02.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (attackParticle03 != null) attackParticle03.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
