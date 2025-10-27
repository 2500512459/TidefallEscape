using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDynamics : MonoBehaviour
{
    [SerializeField] private float maxImpetus = 1000f;          //最大动力
    [SerializeField] private float backwardSpeedFactor = 0.5f;  //后退系数
    [SerializeField] private float turningFactor = 1.0f;        //转向系数
    private float force = 0f;   //动力

    private float verticalImpetus = 0f;     // 键盘上下输入
    private float horizontalImpetus = 0f;   // 键盘左右输入

    private Rigidbody rigidbodyComponent;

    void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // 移动
        force = 0;
        if (verticalImpetus > 0)
        {
            force = verticalImpetus * maxImpetus;
        }
        else if (verticalImpetus < 0)
        {
            force = verticalImpetus * maxImpetus * backwardSpeedFactor;
        }
        rigidbodyComponent.AddRelativeForce(Vector3.forward * force);

        // 转向
        float rotationAngle = horizontalImpetus * turningFactor;
        if (verticalImpetus < 0)
        {
            rotationAngle *= -1;// 倒车
        }

        Quaternion currenRotation = rigidbodyComponent.rotation;
        Vector3 angle = currenRotation.eulerAngles;
        angle.y += rotationAngle;   //绕y轴旋转
        angle.y = angle.y % 360.0f;
        Quaternion newRotation = Quaternion.Euler(angle);
        rigidbodyComponent.MoveRotation(newRotation);
    }

    private void Update()
    {
        verticalImpetus = InputManager.Instance.inputMove.y;
        horizontalImpetus = InputManager.Instance.inputMove.x;
    }
}
