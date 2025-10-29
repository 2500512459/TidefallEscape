using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

/// <summary>
/// NPCShip：AI 船只逻辑
/// - 继承自 AICharacter（具备通用 AI 行为基础）
/// - 使用 WaypointNavigator 寻路系统进行路径导航
/// - 使用 SteeringBehaviors 实现转向与移动控制
/// - 使用 Fluid Behavior Tree 实现 AI 行为决策
/// </summary>
public class NPCShip : AICharacter
{
    // 该 NPC 船上的路径导航组件（负责路径点导航）
    WaypointNavigator waypointNav;


    /// <summary>
    /// Awake：在对象创建时调用（比 Start 更早）
    /// 用于初始化必要组件引用
    /// </summary>
    protected override void Awake()
    {
        base.Awake();  // 调用父类 AICharacter 的 Awake（一般初始化 steeringBehaviors、colsensor、separation 等）

        // 获取附加在同一物体上的 WaypointNavigator 组件
        waypointNav = GetComponent<WaypointNavigator>();
    }


    /// <summary>
    /// Start：在场景启动时调用
    /// </summary>
    protected override void Start()
    {
        base.Start();  // 调用父类 Start（可能初始化 AI 参数或传感器）

        // 初始化行为树
        InitAI();
    }


    /// <summary>
    /// Update：每帧调用（此处未额外逻辑，保留父类更新）
    /// </summary>
    protected override void Update()
    {
        base.Update();
    }


    /// <summary>
    /// FixedUpdate：物理帧更新（通常用于移动、物理计算）
    /// 在这里驱动行为树（brain.Tick）每帧执行一次决策逻辑。
    /// </summary>
    private void FixedUpdate()
    {
        brain.Tick(); // 执行当前 AI 行为树逻辑
    }


    /// <summary>
    /// 初始化 AI 行为树：
    /// 定义 AI 船只的主要行为逻辑（仅一个简单的“更新船状态”任务）
    /// </summary>
    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            // ======================== [任务节点] =========================
            .Do("Update Ship", () =>
            {
                // 初始化目标位置（默认值为零）
                Vector3 targetPosition = Vector3.zero;

                // 从 WaypointManager 获取场景中所有的路径点
                List<Waypoint> waypoints = WaypointManager.Instance.GetWaypoints();

                // 如果路径点存在，则随机选择一个目标进行导航
                if (waypoints != null && waypoints.Count > 0)
                {
                    // 若当前没有路径，则随机选取一个路径点作为新目的地
                    while (!waypointNav.HasPath)
                    {
                        waypointNav.SetDestination(
                            waypoints[Random.Range(0, waypoints.Count)].Position
                        );
                    }

                    // 获取当前路径的下一个目标点位置
                    targetPosition = waypointNav.CurrentWaypointPosition;
                }

                // 使用 steeringBehaviors 的“到达”行为计算加速度向量
                Vector3 accel = steeringBehaviors.Arrive(targetPosition);


                // =================== [碰撞传感器修正] ====================
                // 若当前船拥有碰撞检测器（colsensor），则修正加速度方向，避开障碍
                if (colsensor)
                {
                    // 提取加速度方向
                    Vector3 accDir = accel.normalized;

                    // 通过碰撞传感器计算安全方向（GetCollisionFreeDirection2）
                    colsensor.GetCollisionFreeDirection2(accDir, out accDir);

                    // 保留原本加速度大小（防止速度突变）
                    accDir *= accel.magnitude;

                    // 更新修正后的加速度
                    accel = accDir;
                }


                // =================== [分离行为修正] ====================
                // 若船存在 separation（群体分离行为），
                // 则在原有加速度基础上叠加分离向量（避免与其他船重叠）
                if (separation)
                {
                    accel += separation.GetSteering();
                }


                // =================== [最终执行移动与转向] ====================
                // 将计算好的总加速度传递给 Steering 系统进行移动控制
                steeringBehaviors.Steer(accel);

                // 让船只朝移动方向平滑转向
                steeringBehaviors.LookMoveDirection();

                // 任务成功（行为树要求返回 TaskStatus）
                return TaskStatus.Success;
            })
            // ============================================================
            .Build(); // 构建完整的行为树结构
    }
}
