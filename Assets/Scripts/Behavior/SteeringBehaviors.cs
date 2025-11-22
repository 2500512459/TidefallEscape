using UnityEngine;
using System.Collections.Generic;

// 转向行为
// 要求组件必须包含Rigidbody，如果不存在会自动添加
[RequireComponent(typeof(Rigidbody))]
public class SteeringBehaviors : MonoBehaviour
{
    [Header("General")]
    public float maxVelocity = 3.5f;        // 最大移动速度
    public float maxAcceleration = 10f;     // 最大加速度
    public float turnSpeed = 20f;           // 转向速度

    [Header("Arrive")]
    public float targetRadius = 0.005f;     // 到达目标点的判定半径
    public float slowRadius = 1f;           // 开始减速的半径范围
    public float timeToTarget = 0.1f;       // 到达目标的时间参数

    private Rigidbody rb;                   // 刚体组件引用

    void Awake()
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody>();
    }

    // 应用转向力到刚体
    public void Steer(Vector3 linearAcceleration)
    {
        // 根据加速度更新速度
        rb.velocity += linearAcceleration * Time.deltaTime;

        // 限制速度不超过最大值
        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;
        }
    }

    // 寻找行为：计算指向目标的加速度
    public Vector3 Seek(Vector3 targetPosition, float maxSeekAccel)
    {
        // 计算指向目标的方向向量
        Vector3 acceleration = targetPosition - transform.position;
        acceleration.Normalize();
        acceleration *= maxSeekAccel;  // 应用最大加速度
        return acceleration;
    }

    // 寻找行为的重载版本，使用默认最大加速度
    public Vector3 Seek(Vector3 targetPosition)
    {
        return Seek(targetPosition, maxAcceleration);
    }

    // 让物体朝向移动方向
    public void LookMoveDirection()
    {
        Vector3 direction = rb.velocity;
        LookAtDirection(direction);
    }

    // 让物体朝向指定方向
    public void LookAtDirection(Vector3 direction)
    {
        direction.Normalize();

        // 只有当方向向量足够大时才进行旋转计算
        if (direction.sqrMagnitude > 0.001f)
        {
            // 计算目标旋转角度（2D平面上的Y轴旋转）
            float toRotation = -1 * (Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) + 90;
            // 平滑插值旋转到目标角度
            float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
            // 应用旋转
            transform.rotation = Quaternion.Euler(0, rotation, 0);
        }
    }

    // 带抬头角度的朝向方法
    public void LookAtDirectionHeadUp(Vector3 direction, float headUp)
    {
        direction.Normalize();

        if (direction.sqrMagnitude > 0.001f)
        {
            // 计算Y轴旋转
            float toRotation = -1 * (Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg) + 90;
            float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
            // 应用包含X轴抬头角度的旋转
            transform.rotation = Quaternion.Euler(-headUp, rotation, 0);
        }
    }

    // 通过四元数指定目标朝向
    public void LookAtDirection(Quaternion toRotation)
    {
        LookAtDirection(toRotation.eulerAngles.y);
    }

    // 通过角度值指定目标朝向
    public void LookAtDirection(float toRotation)
    {
        float rotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, toRotation, Time.deltaTime * turnSpeed);
        transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    // 抵达行为：平滑减速到达目标点
    public Vector3 Arrive(Vector3 targetPosition)
    {
        // 绘制调试线显示目标位置
        Debug.DrawLine(transform.position, targetPosition, Color.cyan, 0f, false);

        // 计算到目标的方向向量
        Vector3 targetVelocity = targetPosition - rb.position;
        float dist = targetVelocity.magnitude;  // 到目标的距离

        // 如果已经在目标半径内，停止移动
        if (dist < targetRadius)
        {
            rb.velocity = Vector3.zero;
            return Vector3.zero;
        }

        // 根据距离计算目标速度
        float targetSpeed;
        if (dist > slowRadius)
        {
            // 在减速半径外，使用最大速度
            targetSpeed = maxVelocity;
        }
        else
        {
            // 在减速半径内，按比例减速
            targetSpeed = maxVelocity * (dist / slowRadius);
        }

        // 计算目标速度向量
        targetVelocity.Normalize();
        targetVelocity *= targetSpeed;

        // 计算需要的加速度来达到目标速度
        Vector3 acceleration = targetVelocity - rb.velocity;
        acceleration *= 1 / timeToTarget;

        // 限制加速度不超过最大值
        if (acceleration.magnitude > maxAcceleration)
        {
            acceleration.Normalize();
            acceleration *= maxAcceleration;
        }

        return acceleration;
    }

    // 拦截行为：预测并移动到两个目标之间的中点
    public Vector3 Interpose(Rigidbody target1, Rigidbody target2)
    {
        // 计算两个目标的中间点
        Vector3 midPoint = (target1.position + target2.position) / 2;

        // 估算到达中间点所需时间
        float timeToReachMidPoint = Vector3.Distance(midPoint, transform.position) / maxVelocity;

        // 预测两个目标未来的位置
        Vector3 futureTarget1Pos = target1.position + target1.velocity * timeToReachMidPoint;
        Vector3 futureTarget2Pos = target2.position + target2.velocity * timeToReachMidPoint;

        // 计算预测的未来中间点
        midPoint = (futureTarget1Pos + futureTarget2Pos) / 2;

        // 使用抵达行为移动到预测的中间点
        return Arrive(midPoint);
    }

    // 检查目标是否在正前方
    public bool IsInFront(Vector3 target)
    {
        return IsFacing(target, 0);
    }

    // 检查是否面向目标（通过余弦阈值判断）
    public bool IsFacing(Vector3 target, float cosineValue)
    {
        Vector3 facing = transform.right.normalized;  // 物体的前方方向
        Vector3 directionToTarget = (target - transform.position).normalized;  // 指向目标的方向
        // 使用点积判断两个向量的夹角
        return Vector3.Dot(facing, directionToTarget) >= cosineValue;
    }

    // 静态方法：将朝向角度转换为方向向量
    public static Vector3 OrientationToVector(float orientation)
    {
        /* 将朝向角度乘以-1，因为在y轴上逆时针是负方向，
         * 但Cos和Sin期望顺时针方向为正方向 */
        return new Vector3(Mathf.Cos(-orientation), 0, Mathf.Sin(-orientation));
    }

    // 静态方法：将方向向量转换为朝向角度
    public static float VectorToOrientation(Vector3 direction)
    {
        /* 乘以-1，因为在y轴上逆时针是负方向 */
        return -1 * Mathf.Atan2(direction.z, direction.x);
    }
}