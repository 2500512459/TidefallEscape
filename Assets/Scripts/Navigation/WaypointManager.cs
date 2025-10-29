using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Waypoint 管理器（单例）
/// 用于初始化、管理所有路径点，并执行路径搜索
/// </summary>
public class WaypointManager : MonoSingleton<WaypointManager>
{
    // 当前场景中的所有 Waypoint
    private List<Waypoint> waypoints = new List<Waypoint>();


    private void Start()
    {
        // 游戏开始时初始化节点网络
        InitializeWaypoints();
    }


    /// <summary>
    /// 初始化路径点：收集所有 Waypoint，并自动生成连接关系
    /// </summary>
    public void InitializeWaypoints()
    {
        waypoints.Clear();

        // 找出场景中所有的 Waypoint
        Waypoint[] waypointArray = FindObjectsOfType<Waypoint>();

        // 对每个节点自动建立与附近节点的连接
        foreach (Waypoint wp in waypointArray)
        {
            wp.AutoGenerateConnections(waypointArray);
        }

        // 加入管理列表
        waypoints.AddRange(waypointArray);
    }


    /// <summary>
    /// 获取当前所有 Waypoint 列表
    /// 若列表为空，则重新初始化
    /// </summary>
    public List<Waypoint> GetWaypoints()
    {
        if (waypoints.Count == 0)
        {
            InitializeWaypoints();
        }
        return waypoints;
    }


    /// <summary>
    /// 获取距离某个位置最近的 Waypoint
    /// </summary>
    public Waypoint GetNearestWaypoint(Vector3 position)
    {
        Waypoint nearestWaypoint = null;
        float minDistance = float.MaxValue;

        foreach (var waypoint in waypoints)
        {
            float distance = Vector3.Distance(waypoint.Position, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestWaypoint = waypoint;
            }
        }

        return nearestWaypoint;
    }


    /// <summary>
    /// 使用 A* 算法在两个节点之间寻找最短路径
    /// </summary>
    public List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        // 防御性检查
        if (start == null || goal == null)
            return new List<Waypoint>();

        // frontier：待探索的节点列表（相当于 openList）
        var frontier = new List<PathNode>();

        // visitedNodes：已探索节点集合（closeList）
        var visitedNodes = new HashSet<Waypoint>();

        // cameFrom：用于记录每个节点的前驱节点（用于回溯路径）
        var cameFrom = new Dictionary<Waypoint, Waypoint>();

        // costSoFar：记录从起点到某节点的累计代价
        var costSoFar = new Dictionary<Waypoint, float>();


        // 起点节点初始化
        var startNode = new PathNode(start, 0, Vector3.Distance(start.Position, goal.Position));
        frontier.Add(startNode);
        costSoFar[start] = 0;


        // 开始循环搜索
        while (frontier.Count > 0)
        {
            // 找出当前代价最小的节点（即 fCost 最小的）
            PathNode current = frontier[0];
            int currentIndex = 0;

            for (int i = 1; i < frontier.Count; i++)
            {
                if (frontier[i].fCost < current.fCost)
                {
                    current = frontier[i];
                    currentIndex = i;
                }
            }

            // 从待探索列表中移除该节点
            frontier.RemoveAt(currentIndex);
            visitedNodes.Add(current.waypoint);

            // 若已到达目标，则结束
            if (current.waypoint == goal)
                break;

            // 遍历当前节点的所有连接（邻居节点）
            foreach (var connection in current.waypoint.Connections)
            {
                var next = connection.target;
                if (visitedNodes.Contains(next)) continue;

                // 计算从起点到当前邻居节点的新代价
                float newCost = costSoFar[current.waypoint] + connection.cost;

                // 如果该邻居节点未访问过或发现了更短路径
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;

                    // 启发式函数（H）：到目标点的直线距离
                    float heuristic = Vector3.Distance(next.Position, goal.Position);

                    // 创建新的路径节点（包含G与H）
                    var nextNode = new PathNode(next, newCost, heuristic);

                    // 加入 frontier 等待探索
                    frontier.Add(nextNode);

                    // 更新 cameFrom 表，记录路径来源
                    if (cameFrom.ContainsKey(next))
                        cameFrom[next] = current.waypoint;
                    else
                        cameFrom.Add(next, current.waypoint);
                }
            }
        }

        // 通过回溯表生成完整路径
        return ReconstructPath(cameFrom, start, goal);
    }


    /// <summary>
    /// 根据 cameFrom 字典回溯出完整路径
    /// </summary>
    private List<Waypoint> ReconstructPath(Dictionary<Waypoint, Waypoint> cameFrom, Waypoint start, Waypoint goal)
    {
        var path = new List<Waypoint>();
        var current = goal;

        // 从目标节点向前回溯直到起点
        while (current != start)
        {
            path.Add(current);

            // 若当前节点没有前驱，说明路径断开，返回 null
            if (!cameFrom.ContainsKey(current))
                return null;

            current = cameFrom[current];
        }

        // 把起点加入
        path.Add(start);

        // 反转顺序，使路径从起点到终点
        path.Reverse();

        return path;
    }


    /// <summary>
    /// 内部类：用于 A* 搜索过程的节点数据结构
    /// </summary>
    private class PathNode
    {
        public Waypoint waypoint;
        public float gCost; // 从起点到该节点的实际代价（G）
        public float hCost; // 启发式估算代价（H：与目标点的直线距离）
        public float fCost => gCost + hCost; // 总代价（F = G + H）

        public PathNode(Waypoint wp, float g, float h)
        {
            waypoint = wp;
            gCost = g;
            hCost = h;
        }
    }
}
