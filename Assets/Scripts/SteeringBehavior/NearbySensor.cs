using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 附近物体传感器 (Nearby Sensor)
// 通过触发器检测附近带有刚体的物体，并提供给其他行为使用
public class NearbySensor : MonoBehaviour
{
    // 使用HashSet存储检测到的目标刚体，自动去重
    HashSet<Rigidbody> _targets = new HashSet<Rigidbody>();

    // 公共属性：获取当前检测到的目标集合
    public HashSet<Rigidbody> targets
    {
        get
        {
            // 在返回前清理已销毁的刚体引用
            _targets.RemoveWhere(IsNull);
            return _targets;
        }
    }

    // 静态方法：检查刚体是否为null（用于清理已销毁的对象）
    static bool IsNull(Rigidbody r)
    {
        return r == null;
    }

    // 尝试将碰撞器的刚体添加到目标集合
    void TryToAdd(Component other)
    {
        // 获取碰撞器关联的刚体组件
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 将刚体添加到目标集合中
            _targets.Add(rb);
        }
    }

    // 尝试从目标集合中移除碰撞器的刚体
    void TryToRemove(Component other)
    {
        // 获取碰撞器关联的刚体组件
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 从目标集合中移除刚体
            _targets.Remove(rb);
        }
    }

    // 当其他碰撞器进入触发器范围时调用
    void OnTriggerEnter(Collider other)
    {
        // 尝试将进入的碰撞器添加到目标集合
        TryToAdd(other);
    }

    // 当其他碰撞器离开触发器范围时调用
    void OnTriggerExit(Collider other)
    {
        // 尝试将离开的碰撞器从目标集合中移除
        TryToRemove(other);
    }
}