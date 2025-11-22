using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 追逐行为 (Pursue Behavior)
// 要求组件必须包含SteeringBehaviors，如果不存在会自动添加
[RequireComponent(typeof(SteeringBehaviors))]
public class PursueBehavior : MonoBehaviour
{
    /// <summary>
    /// 追逐行为预测目标未来位置的最大时间限制
    /// </summary>
    public float maxPrediction = 1f;

    // 当前物体的刚体组件
    Rigidbody rb;
    // 转向行为组件
    SteeringBehaviors steeringBehaviors;

    void Awake()
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody>();
        // 获取转向行为组件
        steeringBehaviors = GetComponent<SteeringBehaviors>();
    }

    // 获取追逐行为产生的转向力
    public Vector3 GetSteering(Rigidbody target)
    {
        /* 计算到目标的距离向量 */
        Vector3 displacement = target.position - transform.position;
        float distance = displacement.magnitude;  // 计算实际距离

        /* 获取当前角色的速度大小 */
        float speed = rb.velocity.magnitude;

        /* 计算预测时间 - 核心算法 */
        float prediction;
        // 如果速度较慢，使用最大预测时间
        if (speed <= distance / maxPrediction)
        {
            prediction = maxPrediction;
        }
        // 如果速度较快，根据距离和速度计算预测时间
        else
        {
            prediction = distance / speed;
        }

        /* 基于预测的目标未来位置进行计算 */
        // 预测目标在未来prediction时间后的位置
        Vector3 explicitTarget = target.position + target.velocity * prediction;

        // 调试绘制线，显示预测的目标位置（注释掉的调试代码）
        //Debug.DrawLine(transform.position, explicitTarget);

        // 使用寻找行为朝向预测的目标位置移动
        return steeringBehaviors.Seek(explicitTarget);
    }
}