using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 基于 NavMesh 的陆地移动组件
/// 用于在岸上不平整地形上的 AI 移动
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMovement : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("移动速度")]
    public float moveSpeed = 3.5f;
    
    [Tooltip("旋转速度")]
    public float rotationSpeed = 10f;
    
    [Tooltip("到达目标的判定距离")]
    public float stoppingDistance = 0.5f;

    private NavMeshAgent navAgent;
    private Vector3 currentDestination;
    private bool hasDestination = false;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        
        // 配置 NavMeshAgent
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = rotationSpeed * 50f; // NavMeshAgent 使用度/秒
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.acceleration = 8f;
        
        // 禁用 NavMeshAgent 的自动更新位置和旋转，我们将手动控制
        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
    }

    void Update()
    {
        if (hasDestination && navAgent.isOnNavMesh)
        {
            // 更新 NavMeshAgent 的目标位置
            navAgent.SetDestination(currentDestination);
            
            // 同步 NavMeshAgent 的位置到实际位置
            navAgent.nextPosition = transform.position;
        }
    }

    /// <summary>
    /// 设置目标位置
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        currentDestination = destination;
        hasDestination = true;
        
        if (navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destination);
        }
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    public void Stop()
    {
        hasDestination = false;
        if (navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
        }
    }

    /// <summary>
    /// 获取当前移动方向（用于旋转）
    /// </summary>
    public Vector3 GetMoveDirection()
    {
        if (navAgent.isOnNavMesh && navAgent.hasPath && navAgent.path.corners.Length > 1)
        {
            Vector3 direction = (navAgent.path.corners[1] - transform.position).normalized;
            return direction;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 获取当前速度（用于动画）
    /// </summary>
    public float GetSpeed()
    {
        if (navAgent.isOnNavMesh && navAgent.hasPath)
        {
            return navAgent.velocity.magnitude;
        }
        return 0f;
    }

    /// <summary>
    /// 是否到达目标
    /// </summary>
    public bool HasReachedDestination()
    {
        if (!hasDestination || !navAgent.isOnNavMesh)
            return true;
            
        return !navAgent.pathPending && 
               navAgent.remainingDistance <= stoppingDistance;
    }

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return hasDestination && navAgent.isOnNavMesh && navAgent.hasPath && 
               navAgent.remainingDistance > stoppingDistance;
    }

    /// <summary>
    /// 应用移动（在 FixedUpdate 中调用）
    /// </summary>
    public void ApplyMovement()
    {
        if (!navAgent.isOnNavMesh || !hasDestination)
            return;

        if (navAgent.hasPath)
        {
            // 获取 NavMeshAgent 计算的速度
            Vector3 velocity = navAgent.desiredVelocity;
            
            // 应用移动
            transform.position += velocity * Time.fixedDeltaTime;
            
            // 应用旋转
            if (velocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    /// <summary>
    /// 检查目标位置是否在 NavMesh 上
    /// </summary>
    public bool IsValidDestination(Vector3 destination)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(destination, out hit, 1f, NavMesh.AllAreas);
    }

    /// <summary>
    /// 获取 NavMesh 上的有效位置（如果目标不在 NavMesh 上，返回最近的有效位置）
    /// </summary>
    public Vector3 GetValidPosition(Vector3 position, float maxDistance = 5f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position;
    }
}

