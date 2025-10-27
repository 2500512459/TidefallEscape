using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDynamics : MonoBehaviour
{
    [SerializeField] private float maxImpetus = 1000f;          //�����
    [SerializeField] private float backwardSpeedFactor = 0.5f;  //����ϵ��
    [SerializeField] private float turningFactor = 1.0f;        //ת��ϵ��
    private float force = 0f;   //����

    private float verticalImpetus = 0f;     // ������������
    private float horizontalImpetus = 0f;   // ������������

    private Rigidbody rigidbodyComponent;

    void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // �ƶ�
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

        // ת��
        float rotationAngle = horizontalImpetus * turningFactor;
        if (verticalImpetus < 0)
        {
            rotationAngle *= -1;// ����
        }

        Quaternion currenRotation = rigidbodyComponent.rotation;
        Vector3 angle = currenRotation.eulerAngles;
        angle.y += rotationAngle;   //��y����ת
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
