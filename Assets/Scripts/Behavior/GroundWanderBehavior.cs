using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 岸上随机游走（基于 GroundSteering + CharacterController）
[RequireComponent(typeof(GroundSteering))]
public class GroundWanderBehavior : MonoBehaviour
{
    // 目标位置变化的时间间隔范围（最小值和最大值）
    public Vector2 targetChangeRange = new Vector2(2.0f, 6.0f);

    // 游走圆半径（XZ 平面）
    public float wanderRadius = 1.4f;
    // 游走圆的前向偏移距离（在角色前方生成圆心，移动更自然）
    public float wanderAheadDistance = 1.2f;

    // 地面/水面图层
    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    // 射线高度与距离（用于从候选点上方向下贴地）
    public float groundRaycastHeight = 5f;
    public float groundRaycastDistance = 20f;

    // 当前的目标位置
    Vector3 targetPosition;

    // 地面转向组件
    GroundSteering ground;

    // 本地缓存：是否已有有效目标
    bool hasTarget = false;

    // 是否到达当前目标点附近（距离<0.5米）
    public bool IsAtTarget { get; private set; } = false;

    [Header("Safety Probe")]
    private ForwardSafetyProbe safetyProbe;           // 可选：外部提供的 ForwardSafetyProbe（反射调用）

    void Awake()
    {
        ground = GetComponent<GroundSteering>();
        safetyProbe = GetComponentInChildren<ForwardSafetyProbe>();
    }

    void Start()
    {
        StartCoroutine(TargetPositionChange());
    }

    // 获取当前帧的转向力 - 随机游走行为的核心方法
    public Vector3 GetSteering()
    {
        if (!hasTarget)
            return Vector3.zero;

        // 若前方不安全（无地面或水位更高），停止推进（统一由 ForwardSafetyProbe 判断）
        if (safetyProbe != null && safetyProbe.IsForwardUnsafe())
            return Vector3.zero;

        Debug.DrawLine(transform.position, targetPosition, Color.gray);

        float sqrDist = (targetPosition - transform.position).sqrMagnitude;
        // 如果距离目标点小于0.5米，认为到达目标，停止移动
        if (sqrDist < 1f * 1f)
        {
            IsAtTarget = true;
            return Vector3.zero;
        }
        else if (sqrDist < ground.slowRadius * ground.slowRadius)
        {
            IsAtTarget = false;
            return ground.Arrive(targetPosition);
        }
        else
        {
            IsAtTarget = false;
            return ground.Seek(targetPosition);
        }
    }

    // 协程：定期随机改变目标位置，实现游走行为
    IEnumerator TargetPositionChange()
    {
        while (true)
        {
            Vector3 newTarget = transform.position;

            // 在角色前方的圆上随机取点
            Vector3 forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 circleCenter = transform.position + forwardFlat * wanderAheadDistance;

            const int k_MaxTries = 8;
            bool found = false;
            for (int i = 0; i < k_MaxTries; i++)
            {
                float theta = Random.value * 2 * Mathf.PI;
                Vector3 onCircle = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * wanderRadius;
                Vector3 candidate = circleCenter + onCircle;

                // 从候选点上方向下射线，贴地（限定 groundLayer）
                Vector3 rayOrigin = candidate + Vector3.up * groundRaycastHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    // 命中的地面若在水层上，跳过
                    if (IsInLayer(hit.collider.gameObject.layer, waterLayer))
                    {
                        continue;
                    }
                    // 若存在水系统，进一步用水位高度判断（兼容没有设置水层的情况）
                    if (Water.Instance != null)
                    {
                        float waterHeight = Water.Instance.GetWaterHeight(hit.point);
                        // 使用固定小容差，避免边界闪烁
                        if (hit.point.y <= waterHeight + 0.02f)
                        {
                            continue;
                        }
                    }
                    newTarget = hit.point;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                targetPosition = newTarget;
                hasTarget = true;
                IsAtTarget = false; // 重置到达状态，开始移动向新目标
            }

            yield return new WaitForSeconds(Random.Range(targetChangeRange.x, targetChangeRange.y));
        }
    }

    // 判断 layer 是否包含在 LayerMask 中
    private bool IsInLayer(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // 前向安全 Gizmos 由 ForwardSafetyProbe 负责，这里无需重复绘制
}


