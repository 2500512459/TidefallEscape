using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    //用于存储每个事件类型对应的处理程序列表
    private class EventEntry
    {
        public List<(WeakReference listener, Action<object> handler)> handlers = new();
    }
    //用于存储所有注册的事件和对应的处理程序
    private static readonly Dictionary<Type, EventEntry> events = new();
    private static EventEntry GetOrCreateEntry(Type messageType)
    {
        if (!events.TryGetValue(messageType, out var entry))
        {
            entry = new EventEntry();
            events[messageType] = entry;
        }
        return entry;
    }
    private static void CleanupEntry(EventEntry entry)
    {
        entry.handlers.RemoveAll(h=>h.listener.Target == null);
    }
    //注册事件
    public static void Listen<TMessage>(UnityEngine.Object listener, Action<TMessage> handler)
    {
        var entry = GetOrCreateEntry(typeof(TMessage));
        var weakRef = new WeakReference(listener);
        Action<object> wrapper = obj => handler((TMessage)obj);
        entry.handlers.Add((weakRef, wrapper));
        CleanupEntry(entry);
    }
    public static void Unlisten<TMessage>(UnityEngine.Object listener)
    {
        if (!events.TryGetValue(typeof(TMessage), out var entry))
            return;
        entry.handlers.RemoveAll(h => ReferenceEquals(h.listener.Target, listener));
    }
    //触发事件
    public static void Raise<TMessage>(TMessage message)
    {
        if (!events.TryGetValue(typeof(TMessage), out var entry))
            return;
        foreach (var (weakListener, handler) in entry.handlers.ToArray())
        {
            var listener = weakListener.Target;
            if (listener != null)
            {
                handler(message);
            }
        }
        CleanupEntry(entry);
    }
}
