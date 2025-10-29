using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Waypoint 导航器
/// 用于角色或AI实体根据 WaypointManager 生成的路径进行路径导航。
/// - 提供目标点设置函数 (SetDestination)
/// - 自动判断是否到达当前节点并前往下一个节点
/// - 可在编辑器中绘制调试路径
/// </summary>
public class WaypointNavigator : MonoBehaviour
{
    // 若启用，可定时重新计算路径（用于目标在移动或存在动态障碍时）
    //[SerializeField] private float pathUpdateInterval = 0.5f;

    // 当对象距离当前路径点小于该数值时，认为已“到达”该路径点并前往下一个。
    [SerializeField] private float waypointReachDistance = 1f;

    // 是否在 Scene 视图中绘制路径调试线（用于可视化当前路径）
    [SerializeField] private bool debugPath = false;

    // 当前完整路径（由 WaypointManager.FindPath() 生成）
    private List<Waypoint> currentPath;

    // 当前正在前往的路径点索引（在 currentPath 中的下标）
    private int currentWaypointIndex;

    // 若启用动态路径更新机制，将记录上次更新时间。
    //private float lastPathUpdateTime;

    /// <summary>
    /// 当前是否拥有可用路径
    /// </summary>
    public bool HasPath => currentPath != null && currentPath.Count > 0;


    /// <summary>
    /// 获取当前路径点的世界坐标
    /// 若没有路径，则返回自身位置（防止报错）
    /// </summary>
    public Vector3 CurrentWaypointPosition =>
        HasPath ? currentPath[currentWaypointIndex].Position : transform.position;


    /// <summary>
    /// 设置目标点（外部调用接口）
    /// 自动寻找“离目标点最近的Waypoint”并计算路径。
    /// </summary>
    /// <param name="worldPosition">目标的世界坐标</param>
    public void SetDestination(Vector3 worldPosition)
    {
        // 找到距离目标点最近的路径点
        var targetWaypoint = WaypointManager.Instance.GetNearestWaypoint(worldPosition);

        // 找到距离自身最近的路径点
        var startWaypoint = WaypointManager.Instance.GetNearestWaypoint(transform.position);

        // 若两者均存在，则执行寻路
        if (targetWaypoint != null && startWaypoint != null)
        {
            // 调用 WaypointManager 的 A* 寻路系统生成路径
            currentPath = WaypointManager.Instance.FindPath(startWaypoint, targetWaypoint);

            // 重置路径索引，从第一个点开始行进
            currentWaypointIndex = 0;
        }
    }


    /// <summary>
    /// 每帧更新逻辑：
    /// 检测是否到达当前路径点；若到达则切换到下一个。
    /// （不包含移动逻辑，需外部脚本根据 CurrentWaypointPosition 实现移动）
    /// </summary>
    private void Update()
    {
        if (!HasPath) return; // 若无路径则直接退出

        // 计算当前位置到当前路径点的距离
        float distanceToWaypoint = Vector3.Distance(transform.position, CurrentWaypointPosition);

        // 若距离小于阈值，说明已到达该路径点
        if (distanceToWaypoint <= waypointReachDistance)
        {
            // 前往下一个路径点
            currentWaypointIndex++;

            // 若索引超出范围（即已到达终点）
            if (currentWaypointIndex >= currentPath.Count)
            {
                // 清空路径，表示导航完成
                currentPath = null;
            }
        }

        //// （可选功能）定期更新路径
        //if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
        //{
        //    UpdatePath();
        //    lastPathUpdateTime = Time.time;
        //}
    }


    /*
    /// <summary>
    /// 动态更新路径（可选功能）
    /// 若目标在移动或存在障碍变化，可定时重新计算路径。
    /// </summary>
    private void UpdatePath()
    {
        if (!HasPath) return;

        // 找出离当前位置最近的Waypoint
        var startWaypoint = WaypointManager.Instance.GetNearestWaypoint(transform.position);

        // 当前路径的终点（目标Waypoint）
        var targetWaypoint = currentPath[currentPath.Count - 1];

        // 重新计算路径
        var newPath = WaypointManager.Instance.FindPath(startWaypoint, targetWaypoint);

        // 若新路径有效，则替换旧路径
        if (newPath != null)
        {
            currentPath = newPath;
            currentWaypointIndex = 0;
        }
    }
    */


#if UNITY_EDITOR
    /// <summary>
    /// 在 Scene 视图中绘制路径调试信息
    /// （仅编辑器下显示，不影响运行）
    /// </summary>
    private void OnDrawGizmos()
    {
        // 若未开启调试或没有路径则不绘制
        if (!debugPath || !HasPath) return;

        // 绘制路径连线（黄色）
        Gizmos.color = Color.yellow;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i].Position, currentPath[i + 1].Position);
        }

        // 绘制当前目标路径点（红色圆圈）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(CurrentWaypointPosition, waypointReachDistance);
    }
#endif
}
