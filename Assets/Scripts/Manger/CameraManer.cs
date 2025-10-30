using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraManer : MonoBehaviour
{
    public float rotateSpeed = 1.0f;    //��ת�ٶ�
    public float scrollSpeed = 3.0f;    //�����ٶ�
    public float lookRotateX = 60;      //��ʼ�����Ƕ�
    public float lookRotateY = 180;       //��ʼƫ���Ƕ�
    public float lookDistance = 20;     //��ʼ�߶�
    public Transform target;            //����Ŀ��
    Vector3 offset;                     //��������
    private Camera Camera;

    private void Start()
    {
        StartCoroutine(InitCamer());
    }

    void LateUpdate()
    {
        if (!InputManager.Instance.isInventoryOpen)
        {
            if (InputManager.Instance.inputRotateState)
            {
                UpdateVertical();
                UpdateHorizontal();
            }

            UpdateDistance();

            lookRotateX = Mathf.Clamp(lookRotateX, 60, 80);
            Quaternion rot = Quaternion.Euler(lookRotateX, lookRotateY, 0);
            offset = rot * Vector3.forward * lookDistance;

            transform.position = target.transform.position - offset;
            transform.LookAt(target);
        }
    }

    IEnumerator InitCamer()
    {
        yield return null;
        Camera = Camera.main;
        Camera.transform.SetParent(transform, false);
        Camera.transform.localPosition = Vector3.zero;
        Camera.transform.localRotation = Quaternion.identity;
    }
    void UpdateVertical()
    {
        float verticalDelta = InputManager.Instance.inputLook.y * rotateSpeed;
        lookRotateX -= verticalDelta;
    }
    void UpdateHorizontal()
    {
        float horizontal = InputManager.Instance.inputLook.x * rotateSpeed;
        lookRotateY += horizontal;
    }
    void UpdateDistance()
    {
        lookDistance += Input.mouseScrollDelta.y * scrollSpeed;
        lookDistance = Mathf.Clamp(lookDistance, 4, 100);
    }
}
