using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Trees; // 外部AI行为树插件引用

/// <summary>
/// AICharacter（AI角色基类）
/// 继承自Character，扩展出AI相关的属性与行为：
/// - 视野范围
/// - 攻击范围
/// - 行为树（BehaviorTree）
/// - 各种转向与避障行为组件
/// </summary>
public class AICharacter : Character
{
    // AI视野半径：能感知的最大距离（用于检测附近目标）
    [Range(0.1f, 100)]
    public float viewRadius = 10;

    // 攻击半径：能进行攻击的距离阈值
    [Range(0.1f, 100)]
    public float attackRadius = 1;

    // AI的行为树逻辑控制器（外部插件 Fluid Behavior Tree）
    [SerializeField]
    protected BehaviorTree brain;

    // 各种AI运动组件
    protected SteeringBehaviors steeringBehaviors;   // 负责移动方向与力的融合
    protected WanderBehavior wander;                 // 随机漫游
    protected PursueBehavior pursue;                 // 追踪目标
    protected CollisionSensor colsensor;             // 碰撞感知（防止撞墙）
    protected SeparationBehavior separation;         // 分离行为（避免与其他AI重叠）

    // 存活状态标识
    protected bool live = true;

    /// <summary>
    /// 初始化AI角色，缓存所有AI行为组件。
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // 缓存AI行为相关组件
        steeringBehaviors = GetComponent<SteeringBehaviors>();
        wander = GetComponent<WanderBehavior>();
        pursue = GetComponent<PursueBehavior>();
        colsensor = GetComponent<CollisionSensor>();
        separation = GetComponent<SeparationBehavior>();
    }

    /// <summary>
    /// 获取范围内的所有角色
    /// （无过滤）
    /// </summary>
    public List<Character> GetCharactersInView()
    {
        if (CharacterManager.Instance)
        {
            return CharacterManager.Instance.GetCharactersWithinRange(this, transform.position, viewRadius);
        }

        // 若无管理器，则返回空列表
        return new List<Character>();
    }

    /// <summary>
    /// 获取范围内的所有角色（带类型过滤器）
    /// 例如：只检测敌人或玩家
    /// </summary>
    public List<Character> GetCharactersInView(CharacterTypeFilter typeFilter)
    {
        if (CharacterManager.Instance)
        {
            return CharacterManager.Instance.GetCharactersWithinRange(this, transform.position, viewRadius, typeFilter);
        }

        return new List<Character>();
    }
}
