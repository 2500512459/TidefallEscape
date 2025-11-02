using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 随机游走行为 (Wander Behavior)
// 要求组件必须包含SteeringBehaviors，如果不存在会自动添加
[RequireComponent(typeof(SteeringBehaviors))]
public class WanderBehavior : MonoBehaviour
{
    // 目标位置变化的时间间隔范围（最小值和最大值）
    public Vector2 targetChangeRange = new Vector2(2.0f, 6.0f);
    
    // 游走半径，决定目标点距离角色多远的圆形范围内
    public float wanderRadius = 1.2f;
    
    // 目标点的高度位置（Y坐标）
    public float targetHeight = -10;

    // 当前的目标位置
    Vector3 targetPosition;

    // 转向行为组件的引用
    SteeringBehaviors steeringBehaviors;

    // 刚体组件的引用
    Rigidbody rb;

    void Awake()
    {
        // 获取转向行为组件
        steeringBehaviors = GetComponent<SteeringBehaviors>();
        
        // 获取刚体组件
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // 启动目标位置变化的协程
        StartCoroutine(TargetPositionChange());
    }

    // 获取当前帧的转向力 - 随机游走行为的核心方法
    public Vector3 GetSteering()
    {
        // 绘制调试线显示当前目标位置
        Debug.DrawLine(transform.position, targetPosition, Color.gray);

        // 使用寻找行为计算朝向目标点的加速度
        return steeringBehaviors.Seek(targetPosition);
    }

    // 协程：定期随机改变目标位置，实现游走行为
    IEnumerator TargetPositionChange()
    {
        // 无限循环，持续改变目标位置
        while (true)
        {
            Vector3 wanderTarget;

            // 生成一个随机角度（0到2π弧度）
            float theta = Random.value * 2 * Mathf.PI;
            
            /* 在游走圆上创建一个指向目标位置的向量 */
            // 使用三角函数计算圆上的点坐标
            wanderTarget = new Vector3(wanderRadius * Mathf.Cos(theta), 0f, wanderRadius * Mathf.Sin(theta));

            // 标准化向量（虽然已经是在圆上，但为了确保）
            wanderTarget.Normalize();
            // 乘以半径得到最终的目标偏移量
            wanderTarget *= wanderRadius;

            // 计算世界空间中的目标位置（当前位置 + 偏移量）
            targetPosition = transform.position + wanderTarget;

            // 设置目标点的高度
            targetPosition.y = targetHeight;

            // 等待随机时间后再次改变目标位置
            yield return new WaitForSeconds(Random.Range(targetChangeRange.x, targetChangeRange.y));
        }
    }
}