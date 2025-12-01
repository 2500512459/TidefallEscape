using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;


public class EnemyShip : AICharacter
{
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public EnemyShipType shipType = EnemyShipType.Small;
    [SerializeField] private GameObject hpInfoPrefab;
    [SerializeField] private RectTransform enemyInfoContent;

    [Header("攻击设置")]
    [Tooltip("炮弹预制体")]
    [SerializeField] private GameObject cannonBallPrefab;
    [Tooltip("发射点（中心/左/右）")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform firePointLeft;
    [SerializeField] private Transform firePointRight;
    [Tooltip("攻击间隔")]
    [SerializeField] private float attackInterval = 3f;
    [Tooltip("抛物线初始速度")]
    [SerializeField] private float parabolaInitVelocity = 20f;
    [Tooltip("多少次攻击后切换环绕方向")]
    [SerializeField] private int directionChangeShotCount = 5;
    [Tooltip("进入攻击状态后，首次开火的延迟时间")]
    [SerializeField] private float attackStartDelay = 1.5f;
    [Tooltip("判定进入环绕圈允许开火的半径容差")]
    [SerializeField] private float surroundFireRadiusTolerance = 2f;

    // 追击/攻击切换的缓冲距离，防止临界点抖动
    [Tooltip("脱离攻击状态的缓冲距离（比AttackRadius大一点）")]
    [SerializeField] private float attackExitBuffer = 2f;
    
    private int currentShotCount = 0;

    [Header("死亡溶解设置")]
    [Tooltip("用于溶解效果的基础材质（类似 NPC 使用的溶解材质）")]
    [SerializeField] private Material dissolveMaterial;
    [Tooltip("溶解速度")]
    [SerializeField] private float dissolveSpeed = 5f;
    [Tooltip("溶解强度最大值")]
    [SerializeField] private float maxDissolveAmount = 50f;
    [Tooltip("可选：仅对该节点下的模型做溶解，不指定则对整艘船下的所有 Renderer 做溶解")]
    [SerializeField] private Transform shipModelRoot;

    private float attackTimer = 0f;
    private HealthBar spawnedHealthBar;
    private GameObject spawnedHpInfoGO;
    private WaypointNavigator waypointNav;
    
    private Character attackTarget = null;
    private Vector3 moveToPosition = Vector3.zero;

    // 攻击状态缓存：用于“进入攻击状态后等待一段时间，并进入环绕圈后再开火”的逻辑
    private float attackStateElapsed = 0f;
    private Character attackStateTarget = null;
    private bool isInAttackState = false;

    // 溶解相关状态
    private bool isDissolving = false;
    private float dissolveAmount = 0f;
    private readonly List<Material> dissolveMaterials = new List<Material>();
    private Renderer[] shipRenderers;

    protected override void Awake()
    {
        base.Awake();
        waypointNav = GetComponent<WaypointNavigator>();

        // 预缓存船体所有 Renderer（如果指定了 shipModelRoot，则只在该节点下查找）
        Transform root = shipModelRoot != null ? shipModelRoot : transform;
        shipRenderers = root.GetComponentsInChildren<Renderer>(true);
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        InitAI();
        InitAttributes();
        
        if (spawnedHealthBar != null)
        {
            spawnedHealthBar.SetMaxHealth(maxHealth);
            spawnedHealthBar.SetHealth(maxHealth);
        }
        
        attackTimer = attackInterval; // 初始冷却完毕或需要等待
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        live = true;

        // 对象从池中复用时，重置溶解状态
        isDissolving = false;
        dissolveAmount = 0f;
        if (dissolveMaterials != null && dissolveMaterials.Count > 0)
        {
            foreach (var mat in dissolveMaterials)
            {
                if (mat != null && mat.HasProperty("_DissolveAmount"))
                {
                    mat.SetFloat("_DissolveAmount", 0f);
                }
            }
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        if (isDead && spawnedHpInfoGO != null)
        {
            Destroy(spawnedHpInfoGO);
            spawnedHpInfoGO = null;
            spawnedHealthBar = null;
            attackTarget = null;
            return;
        }

        if (live)
            UpdateAttackTarget();
    }

    private void FixedUpdate()
    {
        if (!live) return;

        brain.Tick();
    }
    
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        EnsureUiReady();

        if (spawnedHealthBar != null)
        {
            spawnedHealthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));
            if (!spawnedHpInfoGO.activeSelf)
            {
                spawnedHpInfoGO.SetActive(true);
            }
        }
    }

    private void EnsureUiReady()
    {
        // 1) 若 Content 未手动指定，尝试自动查找
        if (enemyInfoContent == null)
        {
            var canvasGo = GameObject.Find("Enemy Information Canvas");
            if (canvasGo != null)
            {
                var scroll = canvasGo.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                {
                    enemyInfoContent = scroll.content;
                }
            }
        }

        // 2) 若尚未生成实例，则实例化一个 HP_Information 到 Content 下
        if (spawnedHpInfoGO == null && hpInfoPrefab != null && enemyInfoContent != null)
        {
            spawnedHpInfoGO = Instantiate(hpInfoPrefab, enemyInfoContent);
            spawnedHpInfoGO.SetActive(true);
            spawnedHealthBar = spawnedHpInfoGO.GetComponentInChildren<HealthBar>(true);
            if (spawnedHealthBar != null)
            {
                spawnedHealthBar.SetMaxHealth(maxHealth);
                spawnedHealthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));
            }
        }
    }

    void InitAI()
    {
        brain = new BehaviorTreeBuilder(gameObject)
            .Selector()
                .Sequence()
                    .Condition("Have Attack Target", () => { return HaveAttackTarget(); })
                    .Selector()
                        // 攻击逻辑 (包含环绕)
                        .Sequence()
                            .Condition("In Attack Range", () => { return IsInAttackRange(attackTarget); })
                            .Do("Attack", () =>
                            {
                                // 攻击目标（同时环绕）
                                DoAttack(attackTarget);
                                return TaskStatus.Success;
                            })
                            .End()
                        // 追击逻辑
                        .Do("Pursuit", () =>
                        {
                            // 追逐目标
                            DoPursuit(attackTarget);
                            return TaskStatus.Success;
                        })
                        .End()
                    .End()
                .Do("Random Move", () =>
                {
                    DoRandomMove();
                    return TaskStatus.Success;
                })
            .Build();
    }

    void InitAttributes()
    {
        if (attributesModule != null)
        {
           attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0, maxHealth);
        }
    }

    //target...
    Character GetNearestAttackTargetInView()
    {
        CharacterTypeFilter typeFilter = (character) => character is PlayerShip;

        List<Character> targets = GetCharactersInView(typeFilter);

        if (targets.Count == 0) return null;

        targets.Sort((characterA, characterB) =>
        {
            float distanceA = Vector3.Distance(characterA.transform.position, transform.position);
            float distanceB = Vector3.Distance(characterB.transform.position, transform.position);

            //Returns the comparison result so that the smaller distance is at the front.
            return distanceA.CompareTo(distanceB);
        });

        return targets[0];
    }

    void UpdateAttackTarget()
    {
        if (attackTarget)
        {
            if (Vector3.Distance(attackTarget.transform.position, transform.position) > viewRadius)
            {
                attackTarget = null;
                ResetAttackState();
            }
        }

        if (attackTarget == null)
        {
            attackTarget = GetNearestAttackTargetInView();

            if (attackTarget == null)
            {
                // 没有可攻击目标时，重置攻击状态
                ResetAttackState();
            }
        }
    }

    bool HaveAttackTarget()
    {
        return attackTarget != null;
    }

    bool IsInAttackRange(Character character)
    {
        if (attackTarget == null) return false;

        float dist = Vector3.Distance(character.transform.position, transform.position);
        
        // 增加缓冲距离，防止在边缘反复横跳
        return dist < (attackRadius + attackExitBuffer); 
    }

    // 判定是否已经“进入环绕范围圈”
    bool IsWithinSurroundRadius(Character character)
    {
        if (character == null) return false;

        // 如果没有环绕组件，则只使用时间延迟，不再额外限制开火位置
        if (surrounding == null) return true;

        float dist = Vector3.Distance(character.transform.position, transform.position);
        return Mathf.Abs(dist - surrounding.radius) <= surroundFireRadiusTolerance;
    }

    void ResetAttackState()
    {
        attackStateElapsed = 0f;
        attackStateTarget = null;
        isInAttackState = false;
    }

    void DoRandomMove()
    {
        if (shipType == EnemyShipType.Large)
        {
            // 大型船只采用漫游逻辑
            Vector3 accel = wander.GetSteering();

            if (colsensor)
            {
                Vector3 accDir = accel.normalized;
                colsensor.GetCollisionFreeDirection2(accDir, out accDir);
                accDir *= accel.magnitude;
                accel = accDir;
            }

            steeringBehaviors.Steer(accel);
            steeringBehaviors.LookMoveDirection();
        }
        else
        {
            // 小型和中型船只采用路径导航逻辑
            if (waypointNav == null) return;

            Vector3 targetPosition = Vector3.zero;

            List<Waypoint> waypoints = WaypointManager.Instance.GetWaypoints();

            if (waypoints != null && waypoints.Count > 0)
            {
                while (!waypointNav.HasPath)
                {
                    waypointNav.SetDestination(
                        waypoints[Random.Range(0, waypoints.Count)].Position
                    );
                }
                targetPosition = waypointNav.CurrentWaypointPosition;
            }

            Vector3 accel = steeringBehaviors.Arrive(targetPosition);

            if (colsensor)
            {
                Vector3 accDir = accel.normalized;
                colsensor.GetCollisionFreeDirection2(accDir, out accDir);
                accDir *= accel.magnitude;
                accel = accDir;
            }

            if (separation)
            {
                accel += separation.GetSteering();
            }

            steeringBehaviors.Steer(accel);
            steeringBehaviors.LookMoveDirection();
        }
    }

    void DoAttack(Character character)
    {
        if (character == null)
        {
            ResetAttackState();
            return;
        }

        // 记录/更新攻击状态时间（用于“进入DoAttack状态后等待一段时间再开火”）
        if (!isInAttackState || attackStateTarget != character)
        {
            isInAttackState = true;
            attackStateTarget = character;
            attackStateElapsed = 0f;
        }

        attackStateElapsed += Time.deltaTime;

        // 持续环绕移动
        // 如果有环绕组件，则使用环绕逻辑
        if (surrounding != null)
        {
            Vector3 accel = surrounding.GetSteering(character.transform.position);
            
            if (colsensor)
            {
                Vector3 accDir = accel.normalized;
                colsensor.GetCollisionFreeDirection2(accDir, out accDir);
                accDir *= accel.magnitude;
                accel = accDir;
            }

            steeringBehaviors.Steer(accel);
            steeringBehaviors.LookMoveDirection(); // 像正常船只一样朝向移动方向旋转
        }
        else
        {
            // 没有环绕组件时的备用逻辑：停止移动并转向目标
            steeringBehaviors.Steer(Vector3.zero);
            steeringBehaviors.LookAtDirection(character.transform.position - transform.position);
        }

        // 只有在：
        // 1）进入攻击状态已超过 attackStartDelay 秒
        // 2）并且已经进入环绕圈（距离接近 surrounding.radius）
        // 时才真正尝试开火
        if (attackStateElapsed >= attackStartDelay && IsWithinSurroundRadius(character))
        {
            FireCannon(character.transform.position);
        }
    }

    void FireCannon(Vector3 targetPos)
    {
        if (attackTimer > 0) return;
        if (cannonBallPrefab == null) return;

        // 根据环绕方向选择发射点：
        // direction = 1 (顺时针) -> 左舷对敌 -> 使用 firePointLeft
        // direction = -1 (逆时针) -> 右舷对敌 -> 使用 firePointRight
        Transform bestFirePoint = firePoint;
        if (surrounding != null)
        {
            if (surrounding.direction < 0 && firePointRight != null)
            {
                bestFirePoint = firePointRight;
            }
            else if (surrounding.direction > 0 && firePointLeft != null)
            {
                bestFirePoint = firePointLeft;
            }
        }

        // 如果上述逻辑未选到或特定点为空，则尝试自动选择或回退到中心
        if (bestFirePoint == null || bestFirePoint == firePoint) 
        {
             // 如果没能根据方向选到侧面炮位，尝试保持原有的距离优先逻辑作为备选，或者直接用中心
             if (firePointLeft != null && firePointRight != null)
             {
                  float distLeft = Vector3.Distance(targetPos, firePointLeft.position);
                  float distRight = Vector3.Distance(targetPos, firePointRight.position);
                  bestFirePoint = distLeft < distRight ? firePointLeft : firePointRight;
             }
             else if (firePointLeft != null) bestFirePoint = firePointLeft;
             else if (firePointRight != null) bestFirePoint = firePointRight;
        }

        if (bestFirePoint == null) bestFirePoint = transform;

        // 计算射程参数
        float distance = Vector3.Distance(targetPos, bestFirePoint.position);
        float velocity = parabolaInitVelocity;
        
        float G = 9.8f;
        float maxRange = parabolaInitVelocity * parabolaInitVelocity / G;
        float currentVelocity = parabolaInitVelocity;

        // 限制最大距离
        if (distance > maxRange) distance = maxRange;
        
        // 计算实际发射速度
        currentVelocity = parabolaInitVelocity * Mathf.Sqrt(distance / maxRange);

        // 生成炮弹
        GameObject obj = Instantiate(cannonBallPrefab, bestFirePoint.position, Quaternion.identity);
        
        // 设置伤害源所有者
        DamageVolume dv = obj.GetComponent<DamageVolume>();
        if (dv == null) dv = obj.GetComponentInChildren<DamageVolume>();
        if (dv != null)
        {
            dv.Setup(gameObject);
        }

        CannonBall ball = obj.GetComponent<CannonBall>();
        if (ball != null)
        {
            ball.speed = currentVelocity;
            ball.Launch(GetShootDirection(bestFirePoint.position, targetPos));
        }

        // 重置计时器
        attackTimer = attackInterval;
        
        // 更新开炮计数，达到阈值切换环绕方向
        currentShotCount++;
        if (currentShotCount >= directionChangeShotCount)
        {
            currentShotCount = 0;
            if (surrounding != null)
            {
                surrounding.direction *= -1f; // 反转方向
            }
        }
    }
    
    // 计算带仰角的发射方向
    Vector3 GetShootDirection(Vector3 origin, Vector3 target)
    {
        Vector3 dir = target - origin;
        dir.y = 0; 
        if (dir.sqrMagnitude < 0.001f) return transform.forward;

        Quaternion baseRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // 45度仰角
        Quaternion launchRot = baseRot * Quaternion.Euler(-45f, 0, 0);

        return launchRot * Vector3.forward;
    }

    void DoPursuit(Character character)
    {
        // 切换到追击行为时，重置攻击状态（重新进入攻击时需要重新等待）
        ResetAttackState();

        if (character == null) return;
        //Debug.Log("Pursuiting!");
        //if (animator != null) { animator.SetBool("Attack", false); }

        Vector3 accel = pursue.GetSteering(character.GetRigidBody());

        if (colsensor)
        {
            Vector3 accDir = accel.normalized;
            colsensor.GetCollisionFreeDirection2(accDir, out accDir);
            accDir *= accel.magnitude;
            accel = accDir;
        }

        steeringBehaviors.Steer(accel);
        steeringBehaviors.LookMoveDirection();
    }


    void Dying()
    {
        //gameObject.Recycle();
        StartCoroutine(DelayedRecycle());
    }

    IEnumerator DelayedRecycle()
    {
        // 若配置了溶解材质，则等待溶解度达到最大值后再回收
        if (dissolveMaterial != null)
        {
            yield return new WaitUntil(() => dissolveAmount >= maxDissolveAmount);
        }
        else
        {
            // 没有溶解材质时，为避免卡死，使用固定时间回收
            yield return new WaitForSeconds(5f);
        }
        
        gameObject.Recycle();
    }
    
    protected override void Die()
    {
        if(isDead) return;
        live = false;
        base.Die();
        
        // 启动溶解效果（材质替换 + 参数驱动）
        StartDissolve();

        // 保持原有的延迟回收逻辑（5 秒后回收到对象池）
        Dying();
    }

    /// <summary>
    /// 准备并启动死亡溶解（将船上所有模型的材质替换为溶解材质副本）
    /// </summary>
    private void StartDissolve()
    {
        if (isDissolving) return;
        if (dissolveMaterial == null) return;

        // 如果之前没缓存 Renderer，或者在运行时有变动，重新获取一次
        if (shipRenderers == null || shipRenderers.Length == 0)
        {
            Transform root = shipModelRoot != null ? shipModelRoot : transform;
            shipRenderers = root.GetComponentsInChildren<Renderer>(true);
        }

        if (shipRenderers == null || shipRenderers.Length == 0) return;

        // 只在第一次死亡时进行材质替换并缓存所有实例，避免反复 new Material 造成内存压力
        if (dissolveMaterials.Count == 0)
        {
            foreach (var renderer in shipRenderers)
            {
                if (renderer == null) continue;

                var mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material originalMat = mats[i];
                    if (originalMat == null) continue;

                    // 读取原材质贴图（如果有）
                    Texture mainTex = null;
                    if (originalMat.HasProperty("_MainTex"))
                    {
                        mainTex = originalMat.GetTexture("_MainTex");
                    }

                    // 克隆一个溶解材质实例，避免所有敌船共用一个实例互相影响
                    Material dissolveMatInstance = new Material(dissolveMaterial);
                    if (mainTex != null && dissolveMatInstance.HasProperty("_MainTex"))
                    {
                        dissolveMatInstance.SetTexture("_MainTex", mainTex);
                    }

                    // 初始溶解参数为 0（完全不溶解）
                    if (dissolveMatInstance.HasProperty("_DissolveAmount"))
                    {
                        dissolveMatInstance.SetFloat("_DissolveAmount", 0f);
                    }

                    mats[i] = dissolveMatInstance;
                    dissolveMaterials.Add(dissolveMatInstance);
                }

                renderer.materials = mats;
            }
        }

        // 启动协程驱动 _DissolveAmount
        StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        isDissolving = true;
        dissolveAmount = 0f;

        while (dissolveAmount < maxDissolveAmount)
        {
            dissolveAmount += Time.deltaTime * dissolveSpeed;

            foreach (var mat in dissolveMaterials)
            {
                if (mat != null && mat.HasProperty("_DissolveAmount"))
                {
                    mat.SetFloat("_DissolveAmount", dissolveAmount);
                }
            }

            yield return null;
        }

        // 保持在最大溶解值，直到对象被回收到池里
        foreach (var mat in dissolveMaterials)
        {
            if (mat != null && mat.HasProperty("_DissolveAmount"))
            {
                mat.SetFloat("_DissolveAmount", maxDissolveAmount);
            }
        }

        isDissolving = false;
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制 AttackRadius (黄色)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // 绘制 环绕半径 (红色)
        // 注意：环绕半径是相对于目标的距离，不是AI自己的半径，但为了调试方便，
        if (surrounding != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, surrounding.radius);

            // 如果有攻击目标，在目标周围画一个圈，直观地显示AI试图保持的轨迹
            if (attackTarget != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 半透明红
                Gizmos.DrawWireSphere(attackTarget.transform.position, surrounding.radius);
            }
        }
    }
}
