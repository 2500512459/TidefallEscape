using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ��������ĳ���¼��Ļ���
public class BaseEventListener<T> : MonoBehaviour
{
    public BaseEventSO<T> eventSO;// �������¼�

    public UnityEvent<T> response;// ��Ӧ���¼�������Unity�༭�������

    private void OnEnable()
    {
        if (eventSO != null)
        {
            eventSO.onEventRaised += onEventRaised; // ����¼�����
        }
    }

    private void OnDisable()
    {
        if (eventSO != null)
        {
            eventSO.onEventRaised -= onEventRaised; // �Ƴ��¼�����
        }
    }

    private void onEventRaised(T value)
    {
        response.Invoke(value);
    }
}
