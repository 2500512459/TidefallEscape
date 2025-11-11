using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShipWheel : MonoBehaviour
{
    public Vector2 range;

    public float speed;

    private float rotationY = 0;

    // 旋转中心（父物体）
    public Transform wheelRoot;

    // Update is called once per frame
    void Update()
    {
        rotationY += PlayerInput.Instance.AxesX * speed;
        rotationY = Mathf.Clamp(rotationY, range.x, range.y);

        // 绕父物体的中心旋转
        if (wheelRoot != null)
        {
            wheelRoot.localRotation = Quaternion.Euler(0, rotationY, 0);
        }
        else
        {
            // 没有父物体就退而求其次
            transform.localRotation = Quaternion.Euler(0, rotationY, 0);
        }
    }
}


