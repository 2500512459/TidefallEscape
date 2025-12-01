using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SteeringBehaviors))]
public class SurroundingBehavior : MonoBehaviour
{
    [Header("环绕半径")]
    public float radius = 12f;

    [Header("环绕方向 (1=顺时针, -1=逆时针)")]
    public float direction = 1f;

    [Header("环绕角速度")]
    public float angularSpeed = 3f;

    [Header("半径维持力度（越大越贴着半径走）")]
    public float radiusSpring = 5f;

    private SteeringBehaviors steeringBehaviors;

    private void Awake()
    {
        steeringBehaviors = GetComponent<SteeringBehaviors>();
    }

    public Vector3 GetSteering(Vector3 targetPosition)
    {
        // 1. AI → 目标 的向量
        Vector3 toAI = transform.position - targetPosition;
        toAI.y = 0;
        float dist = toAI.magnitude;
        if (dist < 0.1f) return Vector3.zero;

        Vector3 dirToAI = toAI / dist;

        // ------------------------------
        // ① 半径校正力（让船不会缩圈）
        // ------------------------------
        float radiusError = radius - dist;
        Vector3 radiusForce = dirToAI * radiusError * radiusSpring;

        // ------------------------------
        // ② 切线方向（真正产生“绕圈”速度）
        // ------------------------------
        Vector3 tangent = new Vector3(-dirToAI.z, 0, dirToAI.x) * direction;

        Vector3 orbitForce = tangent * angularSpeed;

        // ------------------------------
        // ③ 合成行为（角动量 + 半径修正）
        // ------------------------------
        Vector3 desiredVelocity = orbitForce + radiusForce;

        // ------------------------------
        // ④ 转成 SteeringBehaviors 加速度
        // ------------------------------
        return steeringBehaviors.Seek(transform.position + desiredVelocity);
    }
}
