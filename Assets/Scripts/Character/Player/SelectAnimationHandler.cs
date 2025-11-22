using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectAnimationHandler : MonoBehaviour
{
    public Transform weaponSocketHip;   // 腰间挂点
    public Transform weaponSocketHand;  // 手部挂点
    public GameObject currentWeapon;
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
}
