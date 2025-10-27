using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// SO事件基类
public class BaseEventSO<T> : ScriptableObject
{
    public string description;

    public UnityAction<T> onEventRaised;
    public string lastSender;

    public void RaiseEvent(T value, object sender)
    {
        onEventRaised?.Invoke(value);   // 发送广播
        lastSender = sender.ToString(); // 记录发送者
    }
}
