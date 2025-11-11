using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    public PlayerAttackCheck attackCheck;
    public Transform weaponSocketHip;   // 腰间挂点
    public Transform weaponSocketHand;  // 手部挂点
    public GameObject currentWeapon;

    void Start()
    {
        AttachToHip();
    }

    // 供动画事件调用：在拔剑动画中调用
    public void AttachToHand()
    {
        currentWeapon.transform.SetParent(weaponSocketHand, false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    // 供动画事件调用：在收剑动画中调用
    public void AttachToHip()
    {
        currentWeapon.transform.SetParent(weaponSocketHip, false);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }

    public void Attack(int damageOverride = 0)
    {
        attackCheck.Attack(damageOverride);
    }
}
