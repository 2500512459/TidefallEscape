using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 分离行为 (Separation Behavior)
// 让角色远离附近的其他物体，避免拥挤和碰撞
public class SeparationBehavior : MonoBehaviour
{
    // 分离行为产生的最大加速度
    public float separationMaxAcceleration = 25;
    
    // 分离行为的作用距离，在此距离内的物体会产生排斥力
    public float separationMaxDistance = 1f;

    // 忽略的层级或Tag，避免与玩家产生排斥
    [Header("Filtering")]
    public LayerMask ignoreLayers; // 可在Inspector设置要忽略的层，如 Player 层
    public List<string> ignoreTags = new List<string> { "Player" }; // 要忽略的Tag列表

    // 附近物体传感器组件
    NearbySensor nearby;

    // 初始化方法
    void Start()
    {
        // 创建一个新的GameObject作为传感器
        GameObject nearbyObj = new GameObject("NearbySendor");
        
        // 将传感器设置为当前物体的子物体
        nearbyObj.transform.SetParent(transform);
        // 将传感器位置设置为与父物体相同
        nearbyObj.transform.localPosition = Vector3.zero;

        // 添加球形碰撞体作为触发器
        SphereCollider collider = nearbyObj.AddComponent<SphereCollider>();
        collider.isTrigger = true;  // 设置为触发器，不产生物理碰撞
        collider.radius = separationMaxDistance;  // 设置检测半径

        // 添加附近物体传感器组件
        nearby = nearbyObj.AddComponent<NearbySensor>();
    }

    // 获取分离行为产生的转向力
    public Vector3 GetSteering()
    {
        Vector3 acceleration = Vector3.zero;  // 初始化加速度为零

        // 遍历传感器检测到的所有目标
        foreach (Rigidbody r in nearby.targets)
        {
            // 过滤逻辑：跳过忽略的层级或Tag
            if (r == null) continue;
            GameObject go = r.gameObject;
            
            // 1. 检查层级是否在忽略列表中
            if (((1 << go.layer) & ignoreLayers) != 0) continue;

            // 2. 检查Tag是否在忽略列表中
            if (ignoreTags.Contains(go.tag)) continue;

            // 计算从目标指向当前物体的方向向量
            Vector3 direction = transform.position - r.transform.position;
            float dist = direction.magnitude;  // 计算与目标的距离

            // 如果目标在分离距离范围内
            if (dist < separationMaxDistance)
            {
                // 计算排斥力的强度：距离越近，排斥力越强
                var strength = separationMaxAcceleration * (separationMaxDistance - dist) / separationMaxDistance;

                // 标准化方向向量
                direction.Normalize();
                // 累加到总加速度中
                acceleration += direction * strength;
            }
        }

        return acceleration;  // 返回计算出的分离加速度
    }
}