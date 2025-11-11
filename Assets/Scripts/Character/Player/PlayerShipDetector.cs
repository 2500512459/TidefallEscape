using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShipDetector : MonoBehaviour
{
    [SerializeField] float detectDistance = 0.2f;    // 射线长度（检测距离）
    [SerializeField] LayerMask shipLayer;

    public bool IsShiped
    {
        get
        {
            // 从当前物体位置向下发射一条射线
            return Physics.Raycast(transform.position, Vector3.down, detectDistance, shipLayer);
        }
    }
}
