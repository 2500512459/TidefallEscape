using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// SO�¼�����
public class BaseEventSO<T> : ScriptableObject
{
    public string description;

    public UnityAction<T> onEventRaised;
    public string lastSender;

    public void RaiseEvent(T value, object sender)
    {
        onEventRaised?.Invoke(value);   // ���͹㲥
        lastSender = sender.ToString(); // ��¼������
    }
}
