using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 碰撞传感器 (Collision Sensor)
// 通过射线检测寻找无碰撞的安全移动方向
public class CollisionSensor : MonoBehaviour
{
    public float rayStart = 0.5f;           // 射线起始偏移（未使用）
    public float rayLength = 10f;           // 射线检测长度
    public int rayCount = 36;               // 射线数量（360度范围内的射线数）
    public LayerMask collisionLayers;       // 检测的碰撞层

    // 未使用的Update方法，用于调试
    //private void Update()
    //{
    //    GetCollisionFreeDirectionOpt(transform.forward);
    //}

    // 方法1：获取无碰撞方向 - 完整搜索最佳方向
    public Vector3 GetCollisionFreeDirection(Vector3 desiredDirection)
    {
        if (desiredDirection == Vector3.zero)
            return Vector3.zero;

        Vector3 bestDirection = Vector3.zero;      // 最佳方向
        float bestAngle = float.MaxValue;          // 最佳角度（与期望方向的最小夹角）

        // 在360度范围内发射多条射线检测碰撞
        for (int i = 0; i < rayCount; i++)
        {
            // 计算当前射线的角度和方向
            float angle = 360f / rayCount * i;
            Vector3 direction = transform.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward;

            // 计算与期望方向的点积，只考虑大致朝向期望方向的方向
            float dotProduct = Vector3.Dot(desiredDirection, direction);

            if (dotProduct > 0) // 只考虑大致朝向期望方向的方向
            {
                // 发射射线检测碰撞
                RaycastHit hit;
                bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

                if (collision)
                {
                    // 绘制红色射线显示碰撞
                    Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
                }
                else
                {
                    // 绘制绿色射线显示安全方向
                    Debug.DrawRay(transform.position, direction * rayLength, Color.green);

                    // 计算当前方向与期望方向的夹角
                    float angleFromDesired = Vector3.Angle(desiredDirection, direction);
                    // 选择与期望方向夹角最小的安全方向
                    if (angleFromDesired < bestAngle)
                    {
                        bestAngle = angleFromDesired;
                        bestDirection = direction;
                    }
                }
            }
        }

        // 如果找到安全方向则返回，否则返回原始期望方向
        return bestDirection != Vector3.zero ? bestDirection : desiredDirection;
    }

    // 方法2：获取无碰撞方向 - 优化版本（双向对称搜索）
    public Vector3 GetCollisionFreeDirectionOpt(Vector3 desiredDirection)
    {
        if (desiredDirection == Vector3.zero)
            return Vector3.zero;

        Vector3 bestDirection = Vector3.zero;

        // 双向对称搜索：同时检查正向和负向角度
        for (int i = 0; i < rayCount / 2; i++)
        {
            // 正向角度检测
            float angle1 = 360f / rayCount * i;
            Vector3 direction1 = transform.rotation * Quaternion.Euler(0, angle1, 0) * Vector3.forward;

            RaycastHit hit;
            bool collision1 = Physics.Raycast(transform.position, direction1, out hit, rayLength, collisionLayers);

            if (collision1)
            {
                Debug.DrawRay(transform.position, direction1 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction1 * rayLength, Color.green);
                bestDirection = direction1;
                break;  // 找到安全方向就退出循环
            }

            // 负向角度检测
            float angle2 = -360f / rayCount * i;
            Vector3 direction2 = transform.rotation * Quaternion.Euler(0, angle2, 0) * Vector3.forward;

            bool collision2 = Physics.Raycast(transform.position, direction2, out hit, rayLength, collisionLayers);

            if (collision2)
            {
                Debug.DrawRay(transform.position, direction2 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction2 * rayLength, Color.green);
                bestDirection = direction2;
                break;  // 找到安全方向就退出循环
            }
        }

        return bestDirection != Vector3.zero ? bestDirection : desiredDirection;
    }

    // 方法3：获取无碰撞方向 - 带输出参数的版本
    public bool GetCollisionFreeDirection(Vector3 desiredDirection, out Vector3 outDirection)
    {
        desiredDirection.Normalize();  // 标准化期望方向
        outDirection = desiredDirection;  // 初始化输出方向

        if (desiredDirection == Vector3.zero)
            return false;

        Vector3 bestDirection = Vector3.zero;

        // 双向对称搜索，以期望方向为基准进行旋转
        for (int i = 0; i < rayCount / 2; i++)
        {
            // 正向旋转期望方向
            float angle1 = 360f / rayCount * i;
            Vector3 direction1 = Quaternion.Euler(0, angle1, 0) * desiredDirection;

            RaycastHit hit;
            bool collision1 = Physics.Raycast(transform.position, direction1, out hit, rayLength, collisionLayers);

            if (collision1)
            {
                Debug.DrawRay(transform.position, direction1 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction1 * rayLength, Color.green);
                bestDirection = direction1;
                break;
            }

            // 负向旋转期望方向
            float angle2 = -360f / rayCount * i;
            Vector3 direction2 = Quaternion.Euler(0, angle2, 0) * desiredDirection;

            bool collision2 = Physics.Raycast(transform.position, direction2, out hit, rayLength, collisionLayers);

            if (collision2)
            {
                Debug.DrawRay(transform.position, direction2 * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction2 * rayLength, Color.green);
                bestDirection = direction2;
                break;
            }
        }

        // 返回是否找到了更好的方向
        if (bestDirection != desiredDirection)
        {
            outDirection = bestDirection;
            return true;
        }
        else
        {
            return false;
        }
    }

    // 方法4：获取无碰撞方向 - 分离正向负向搜索版本
    public bool GetCollisionFreeDirection2(Vector3 desiredDirection, out Vector3 outDirection)
    {
        desiredDirection.Normalize();
        outDirection = desiredDirection;

        if (desiredDirection == Vector3.zero)
            return false;

        Vector3 bestDirection = Vector3.zero;

        // 单独搜索正向角度
        Vector3 bestDirection1 = Vector3.zero;
        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle = 360f / rayCount * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

            if (collision)
            {
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * rayLength, Color.green);
                bestDirection1 = direction;
                break;
            }
        }

        // 单独搜索负向角度
        Vector3 bestDirection2 = Vector3.zero;
        for (int i = 0; i < rayCount / 2; i++)
        {
            float angle = -360f / rayCount * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            bool collision = Physics.Raycast(transform.position, direction, out hit, rayLength, collisionLayers);

            if (collision)
            {
                Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, direction * rayLength, Color.green);
                bestDirection2 = direction;
                break;
            }
        }

        // 选择与当前朝向更接近的方向
        if (Vector3.Dot(transform.forward, bestDirection1) > Vector3.Dot(transform.forward, bestDirection2))
        {
            bestDirection = bestDirection1;
        }
        else
        {
            bestDirection = bestDirection2;
        }

        // 返回结果
        if (bestDirection != desiredDirection)
        {
            outDirection = bestDirection;
            return true;
        }
        else
        {
            return false;
        }
    }
}