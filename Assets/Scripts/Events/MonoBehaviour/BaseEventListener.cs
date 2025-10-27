using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 用来监听某个事件的基类
public class BaseEventListener<T> : MonoBehaviour
{
    public BaseEventSO<T> eventSO;// 监听的事件

    public UnityEvent<T> response;// 响应的事件，可在Unity编辑器中添加

    private void OnEnable()
    {
        if (eventSO != null)
        {
            eventSO.onEventRaised += onEventRaised; // 添加事件监听
        }
    }

    private void OnDisable()
    {
        if (eventSO != null)
        {
            eventSO.onEventRaised -= onEventRaised; // 移除事件监听
        }
    }

    private void onEventRaised(T value)
    {
        response.Invoke(value);
    }
}
