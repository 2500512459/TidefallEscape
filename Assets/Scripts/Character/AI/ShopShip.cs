using System.Collections.Generic;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

/// <summary>
/// ShopShip（商船AI）
///
/// 功能说明：
/// - 平时在海上随机航行（Waypoint 或 Wander）
/// - 当检测到玩家进入范围时：
///     → 朝向并靠近玩家
///     → 保持安全距离停止移动
/// - 无 UI 或交易逻辑
/// </summary>
[RequireComponent(typeof(SteeringBehaviors))]
public class ShopShip : AICharacter
{
    public float maxHealth = 100f;
    public HealthBar healthBar;

    [Header("商船检测参数")]
    [Tooltip("检测到玩家后开始靠近的范围")]
    public float detectPlayerRange = 15f;

    [Tooltip("保持停止的距离（太近就停）")]
    public float stopDistance = 8f;

    [Header("提示UI")]
    public TreasureHintUI HintUI;

    WaypointNavigator waypointNav;

    private Transform playerTransform;   // 玩家引用
    private bool isApproachingPlayer = false;
    // 提示UI 是否当前可见
    private bool isUIVisible  = false;
    /// <summary>
    /// Awake：初始化组件
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        waypointNav = GetComponent<WaypointNavigator>();
    }

    /// <summary>
    /// Start：初始化AI逻辑
    /// </summary>
    protected override void Start()
    {
        base.Start();

        attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0, maxHealth);
        healthBar.SetMaxHealth(maxHealth);

        // 查找玩家（假设玩家物体有Tag "Player"）
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        InitAI();
    }
    /// <summary>
    /// Update：每帧调用（此处未额外逻辑，保留父类更新）
    /// </summary>
    protected override void Update()
    {
        base.Update();
        if (isDead)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// FixedUpdate：驱动行为树
    /// </summary>
    private void FixedUpdate()
    {
        if (brain != null && live)
            brain.Tick();
    }

    #region 交互UI
    public void ShowShopUI()
    {
        var shopPanel = ShopUI.Instance;
        if (shopPanel == null)
        {
            Debug.LogError("ShopUI 实例不存在");
            return;
        }

        if (!shopPanel.IsVisible)
        {
            shopPanel.ShowPanel();
        }
    }
    public void HideShopUI()
    {
        var shopPanel = ShopUI.Instance;
        if (shopPanel == null)
        {
            Debug.LogError("ShopUI 实例不存在");
            return;
        }

        if (shopPanel.IsVisible)
        {
            shopPanel.HidePanel();
        }
    }
    public void ShowHint()
    {
        if (isUIVisible == false)
        {
            HintUI.ShowUI();
            isUIVisible = true;
        }
    }
    public void HideHint()
    {
        if (isUIVisible)
        {
            HintUI.HideUI();
            isUIVisible = false;
        }
    }
    #endregion


    /// <summary>
    /// 初始化商船AI行为树逻辑
    /// </summary>
    private void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Selector("ShopShip Main Selector")

                // ① 检测玩家并靠近
                .Sequence("Detect and Approach Player")
                    .Condition("Player In Range", () =>
                    {
                        if (!playerTransform) return false;
                        float dist = Vector3.Distance(transform.position, playerTransform.position);
                        return dist <= detectPlayerRange;
                    })
                    .Do("Approach Player", () =>
                    {
                        if (!playerTransform) return TaskStatus.Failure;

                        float dist = Vector3.Distance(transform.position, playerTransform.position);
                        Vector3 targetPos = playerTransform.position;

                        // 过近则停止
                        if (dist <= stopDistance)
                        {
                            steeringBehaviors.Steer(Vector3.zero);
                            isApproachingPlayer = true;
                        }
                        else
                        {
                            // 靠近玩家
                            Vector3 accel = steeringBehaviors.Arrive(targetPos);

                            //steeringBehaviors.Steer(accel);
                            steeringBehaviors.LookAtDirection(accel);
                            isApproachingPlayer = true;
                        }

                        return TaskStatus.Success;
                    })
                .End()

                // ② 否则巡逻逻辑
                .Do("Patrol Sea", () =>
                {
                    if (isApproachingPlayer)
                        isApproachingPlayer = false;

                    Vector3 accel = Vector3.zero;

                    // 使用路径导航巡逻
                    if (waypointNav)
                    {
                        List<Waypoint> waypoints = WaypointManager.Instance.GetWaypoints();
                        if (waypoints != null && waypoints.Count > 0 && !waypointNav.HasPath)
                        {
                            waypointNav.SetDestination(
                                waypoints[Random.Range(0, waypoints.Count)].Position
                            );
                        }

                        Vector3 targetPos = waypointNav.CurrentWaypointPosition;
                        accel = steeringBehaviors.Arrive(targetPos);
                    }
                    // 若无路径导航则使用随机游走
                    else if (wander)
                    {
                        accel = wander.GetSteering();
                    }

                    // 避障与分离修正
                    if (colsensor)
                    {
                        Vector3 accDir = accel.normalized;
                        colsensor.GetCollisionFreeDirection2(accDir, out accDir);
                        accel = accDir * accel.magnitude;
                    }

                    if (separation)
                        accel += separation.GetSteering();

                    steeringBehaviors.Steer(accel);
                    steeringBehaviors.LookMoveDirection();

                    return TaskStatus.Success;
                })
            .End()
            .Build();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        healthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));

        if(healthBar.gameObject.activeSelf == false)
            healthBar.gameObject.SetActive(true);
    }
}
