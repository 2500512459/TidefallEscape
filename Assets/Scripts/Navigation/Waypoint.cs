using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 表示一个路径点之间的连接关系
/// </summary>
[System.Serializable]
public class WaypointConnection
{
    // 连接的目标路径点
    public Waypoint target;

    // 当前连接的代价（通常是距离）
    public float cost = 1f;

    // 若需要区分双向连接，可启用该参数（当前注释掉）
    // public bool isBidirectional = true;
}


/// <summary>
/// 路径点类：用于定义场景中可通行的节点
/// 支持自动生成与相邻路径点的连接
/// </summary>
public class Waypoint : MonoBehaviour
{
    // 当前节点与其他节点的连接信息（target + cost）
    [SerializeField] private List<WaypointConnection> connections = new List<WaypointConnection>();

    // 连接半径：控制该节点自动连接的范围
    [SerializeField] private float connectionRadius = 10f;

    // 障碍层：用于检测路径中是否有阻挡（如墙体等）
    [SerializeField] private LayerMask obstacleLayer;


    // 节点在世界空间中的位置（简化访问）
    public Vector3 Position => transform.position;

    // 获取当前节点的所有连接（供外部访问）
    public List<WaypointConnection> Connections => connections;


    /// <summary>
    /// 在编辑器中修改属性时自动调用
    /// 用来清理掉无效的连接（防止引用自己或空对象）
    /// </summary>
    private void OnValidate()
    {
        if (connections != null)
        {
            // 从后往前遍历，安全地移除无效连接
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                if (connections[i] == null || connections[i].target == null || connections[i].target == this)
                {
                    connections.RemoveAt(i);
                }
            }
        }
    }


    /// <summary>
    /// 自动生成连接：遍历所有 Waypoint，寻找可见范围内的节点并建立连接
    /// </summary>
    /// <param name="allWaypoints">场景中所有的 Waypoint</param>
    public void AutoGenerateConnections(Waypoint[] allWaypoints)
    {
        // 清空旧连接
        connections.Clear();

        foreach (var waypoint in allWaypoints)
        {
            // 跳过自己
            if (waypoint == this) continue;

            // 计算两点之间距离
            float distance = Vector3.Distance(Position, waypoint.Position);

            // 若在连接范围内
            if (distance <= connectionRadius)
            {
                // 使用射线检测两点间是否有障碍物
                // 如果射线没有碰到障碍层（即可视线通畅）
                if (!Physics.Raycast(Position, waypoint.Position - Position, distance, obstacleLayer))
                {
                    // 添加连接信息
                    connections.Add(new WaypointConnection
                    {
                        target = waypoint,
                        cost = distance // 代价设置为距离
                    });
                }
            }
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中选中该物体时绘制可视化连接（蓝点+绿线）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制自身位置（蓝色圆点）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Position, 0.5f);

        // 绘制与其他节点的连线
        if (connections != null)
        {
            foreach (var connection in connections)
            {
                if (connection.target != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(Position, connection.target.Position);
                }
            }
        }
    }
#endif
}
