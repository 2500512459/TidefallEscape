using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

public class Wolf : AICharacter
{
	[SerializeField] public float maxHealth = 100f;
	[SerializeField] private GameObject hpInfoPrefab; // 关联 Assets/Prefabs/UI/属性UI/HP_Information.prefab
	[SerializeField] private RectTransform enemyInfoContent; // 敌人信息画布中滚动条的 Content

	private HealthBar spawnedHealthBar;
	private GameObject spawnedHpInfoGO;
	private Character attackTarget = null;

	// 岸上移动组件（基于 CharacterController）
	private GroundSteering groundSteering;
	private PursueGroundBehavior pursueGround;
	private GroundWanderBehavior groundWander;

	// Idle -> Wander 计时
	[SerializeField] private Vector2 idleToWanderTimeRange = new Vector2(1.5f, 3.0f);
	private float idleToWanderTimer = 0f;
	private float idleToWanderThreshold = 2f;
	private bool isWandering = false;

	// 漫游移动速度
	[SerializeField] private float wanderMaxVelocity = 1.5f;  // 漫游时的最大移动速度（比追击慢）
	private float originalMaxVelocity = 3.5f;                 // 保存原始最大速度

	// 近距离保持与回滞
	[SerializeField] private float stopDistance = 1.6f;          // 靠近目标时保持的距离（一般>=攻击半径）
	[SerializeField] private float resumeChaseDistance = 2.6f;   // 脱离保持后再次追击的距离

    [SerializeField] private float attackRecoveryTime = 0.8f; // 攻击后摇（可调）
    private float attackRecoveryTimer = 0f;                  // >0 表示后摇中
	// 重力（用于 CharacterController 地面移动路径）
	private CharacterController characterController;
	private float verticalVelocityY = 0f;
	
	// 前方落海/无地面探测
	[Header("Forward Safety Probe")]
	private ForwardSafetyProbe safetyProbe;			// 可挂在子物体的 ForwardSafetyProbe（用反射调用）

	[Header("溶解效果")]
	[SerializeField] private Material material;
	[SerializeField] private float dissolveSpeed = 0.5f;
	private bool isDissolving = false;
	private float dissolveAmount = 0f;
	[SerializeField] private float deathDisappearDelay = 2.5f;
	private Coroutine deathSequenceCoroutine;

	protected override void Start()
	{
		base.Start();

		// 缓存地面移动组件（如存在则优先使用）
		groundSteering = GetComponent<GroundSteering>();
		pursueGround = GetComponent<PursueGroundBehavior>();
		groundWander = GetComponent<GroundWanderBehavior>();
		characterController = GetComponent<CharacterController>();
		safetyProbe = GetComponentInChildren<ForwardSafetyProbe>();

		// 保存原始最大速度（用于追击时恢复）
		if (groundSteering != null)
		{
			originalMaxVelocity = groundSteering.maxVelocity;
		}

		// 初始化 Idle->Wander 随机阈值
		idleToWanderThreshold = Random.Range(idleToWanderTimeRange.x, idleToWanderTimeRange.y);

		// 初始化属性与（若已有）血条最大生命
		attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0, maxHealth);
		if (spawnedHealthBar != null)
		{
			spawnedHealthBar.SetMaxHealth(maxHealth);
			spawnedHealthBar.SetHealth(maxHealth);
		}

		InitAI();
	}

	protected override void Update()
	{
        base.Update();
        // 更新后摇计时
        if (attackRecoveryTimer > 0f)
        {
            attackRecoveryTimer -= Time.deltaTime;
        }
		// 死亡时删除 UI（而非仅隐藏）
		if (isDead && spawnedHpInfoGO != null)
		{
			Destroy(spawnedHpInfoGO);
			spawnedHpInfoGO = null;
			spawnedHealthBar = null;
			attackTarget = null;
			return;
		}
	}

	private void FixedUpdate()
	{
		if (isDead) return;
		ApplyGravityIfNeeded();
		if (brain != null) brain.Tick();
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

	/// <summary>
	/// 确保 Content、实例与 HealthBar 可用；若未设置则自动查找并创建
	/// </summary>
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

	// ================== 行为树与基础行为（Idle/移动/攻击） ==================
	private void InitAI()
	{
		brain = new BehaviorTreeBuilder(gameObject)
			.Selector()
				.Sequence("Attack Tree")
					.Condition("Have Attack Target", () => { return HaveAttackTarget(); })
					.Selector("Want to Attack")
						.Sequence("Try Attack")
							.Condition("In Attack Range", () => { return IsInAttackRange(attackTarget); })
							.Do("Attack", () =>
							{
								DoAttack(attackTarget);
								return TaskStatus.Success;
							})
							.End()
						.Do("Pursuit", () =>
						{
							DoPursuit(attackTarget);
							return TaskStatus.Success;
						})
						.End()
					.End()
				.Selector("Wander or Idle")
					.Sequence("Wander")
						.Condition("Is Wandering", () => { return isWandering; })
						.Do("Wander", () =>
						{
							DoWander();
							return TaskStatus.Success;
						})
						.End()
					.Do("Idle", () =>
					{
						DoIdle();
						return TaskStatus.Success;
					})
			.Build();
	}

	private bool HaveAttackTarget()
	{
		UpdateAttackTarget();
		bool has = attackTarget != null;
		if (has)
		{
			// 发现玩家：退出漫游并切换移动动画
			if (isWandering)
			{
				isWandering = false;
				idleToWanderTimer = 0f;
				// 恢复原始速度（用于追击）
				if (groundSteering != null)
				{
					groundSteering.maxVelocity = originalMaxVelocity;
				}
			}
			if (animator != null)
			{
				animator.SetBool("IsWalk", false);
				animator.SetBool("IsMove", true);
			}
		}
		return has;
	}

	private void UpdateAttackTarget()
	{
		// 目标丢失或超出视野则清空
		if (attackTarget)
		{
			if (Vector3.Distance(attackTarget.transform.position, transform.position) > viewRadius)
			{
				attackTarget = null;
			}
		}

		// 未有目标则尝试在视野内寻找最近玩家
		if (attackTarget == null)
		{
			attackTarget = GetNearestAttackTargetInView();
		}
	}

	private Character GetNearestAttackTargetInView()
	{
		CharacterTypeFilter typeFilter = (actor) => actor is Player;
		List<Character> targets = GetCharactersInView(typeFilter);
		if (targets == null || targets.Count == 0) return null;

		targets.Sort((actorA, actorB) =>
		{
			float distanceA = Vector3.Distance(actorA.transform.position, transform.position);
			float distanceB = Vector3.Distance(actorB.transform.position, transform.position);
			return distanceA.CompareTo(distanceB);
		});
		return targets[0];
	}

	private bool IsInAttackRange(Character actor)
	{
		if (actor == null) return false;

		float distance = Vector3.Distance(actor.transform.position, transform.position);
		bool inRange = distance < attackRadius;
		return inRange;
	}

	private void DoIdle()
	{
		if (animator != null)
		{
			animator.SetBool("Attack", false);
			// Idle 阶段不处于追击移动
			animator.SetBool("IsMove", false);
			animator.SetBool("IsWalk", false);
		}

		// 空闲状态：计时，超过阈值进入漫游
		idleToWanderTimer += Time.deltaTime;
		if (idleToWanderTimer >= idleToWanderThreshold)
		{
			isWandering = true;
			idleToWanderTimer = 0f;
			// 下次的阈值随机化，形成节奏变化
			idleToWanderThreshold = Random.Range(idleToWanderTimeRange.x, idleToWanderTimeRange.y);
			// 设置漫游速度（比追击慢）
			if (groundSteering != null)
			{
				groundSteering.maxVelocity = wanderMaxVelocity;
			}
		}

		// 保持完全静止
		if (groundSteering != null)
		{
			groundSteering.Steer(Vector3.zero);
		}
		else if (steeringBehaviors != null)
		{
			steeringBehaviors.Steer(Vector3.zero);
		}
	}

	private void DoWander()
	{
		if (animator != null)
		{
			animator.SetBool("Attack", false);
		}

		// 漫游逻辑（仅在地面系统可用时）
		if (groundSteering != null && groundWander != null)
		{
			Vector3 accel = ApplyCollisionAvoidance(groundWander.GetSteering());
			bool hasMove = accel.sqrMagnitude > 0.0001f;

			// 若前方不安全或到达目标点，立即清除速度并停止移动
			if (!hasMove)
			{
				groundSteering.ClearHorizontalVelocity();
			}

			groundSteering.Steer(accel);
			if (hasMove)
			{
				groundSteering.LookMoveDirection();
			}

			// 漫游动画：步行动画开启
			if (animator != null)
			{
				animator.SetBool("IsWalk", hasMove);
			}

			// 如果到达目标点附近，停止漫游回到Idle
			if (groundWander.IsAtTarget)
			{
				isWandering = false;
				// 恢复原始速度（虽然现在在Idle，但为下次追击做准备）
				if (groundSteering != null)
				{
					groundSteering.maxVelocity = originalMaxVelocity;
				}
			}
		}
		else
		{
			// 不可漫游时，保持完全静止并停止漫游
			isWandering = false;
			if (groundSteering != null)
			{
				groundSteering.Steer(Vector3.zero);
			}
			else if (steeringBehaviors != null)
			{
				steeringBehaviors.Steer(Vector3.zero);
			}
			if (animator != null)
			{
				animator.SetBool("IsWalk", false);
			}
		}
	}

	private void DoAttack(Character actor)
	{
        // 开始后摇计时
        attackRecoveryTimer = attackRecoveryTime;

		if (actor == null) return;
		if (animator != null)
		{
			animator.SetBool("Attack", true);
			animator.SetBool("IsMove", false);
		}

		if (groundSteering != null)
		{
			// 攻击时清除水平速度并朝向目标，避免刚结束攻击就被侧向速度带走
			groundSteering.ClearHorizontalVelocity();
			groundSteering.Steer(Vector3.zero);
			groundSteering.LookAtDirection(actor.transform.position - transform.position);
		}
		else if (steeringBehaviors != null)
		{
			steeringBehaviors.Steer(Vector3.zero);
			steeringBehaviors.LookAtDirection(actor.transform.position - transform.position);
		}
	}

	private void DoPursuit(Character actor)
	{
		if (attackRecoveryTimer > 0f) 
		{
			// 后摇中禁止移动
			return;
		}

		if (actor == null) return;
		if (animator != null) animator.SetBool("Attack", false);

		// 安全性检测：若前方可能掉落/入海，则立即停止前进
		bool forwardUnsafe = safetyProbe != null && safetyProbe.IsForwardUnsafe();
		if (forwardUnsafe)
		{
			if (groundSteering != null)
			{
				groundSteering.ClearHorizontalVelocity();
				groundSteering.Steer(Vector3.zero);
			}
			else if (steeringBehaviors != null)
			{
				steeringBehaviors.Steer(Vector3.zero);
			}
			if (animator != null)
			{
				animator.SetBool("IsMove", false);
			}
			return;
		}

		// 优先使用地面追逐/移动逻辑（CharacterController）
		if (groundSteering != null && pursueGround != null)
		{
			float distance = Vector3.Distance(actor.transform.position, transform.position);

			// 接近目标时改用 Arrive 减速至停，避免贴脸后侧滑绕圈
			if (distance <= stopDistance)
			{
				Vector3 keepOutSpot = GetKeepOutSpot(actor.transform.position, stopDistance);
				Vector3 accelArrive = groundSteering.Arrive(keepOutSpot);
				groundSteering.Steer(accelArrive);
				groundSteering.LookAtDirection(actor.transform.position - transform.position);
				return;
			}

			// 距离拉大到追击阈值再追（回滞，防止来回切）
			if (distance >= resumeChaseDistance)
			{
				if (animator != null) animator.SetBool("IsMove", true);
				Vector3 accel = pursueGround.GetSteering(actor.transform, actor.GetRigidBody());
				groundSteering.Steer(accel);
				groundSteering.LookMoveDirection();
				return;
			}

			// 处于 stop 与 resume 之间时：缓慢靠近到 keepOutSpot
			{
				Vector3 keepOutSpot = GetKeepOutSpot(actor.transform.position, stopDistance);
				Vector3 accelArrive = groundSteering.Arrive(keepOutSpot);
				groundSteering.Steer(accelArrive);
				groundSteering.LookAtDirection(actor.transform.position - transform.position);
				return;
			}
		}

		// 回退：使用基于刚体的海上追逐/移动逻辑
		if (steeringBehaviors != null && pursue != null)
		{
			if (animator != null) animator.SetBool("IsMove", true);
			Vector3 accel = pursue.GetSteering(actor.GetRigidBody());
			steeringBehaviors.Steer(accel);
			steeringBehaviors.LookMoveDirection();
		}
	}

	/// <summary>
	/// 使用碰撞传感器修正加速度方向，避开障碍物
	/// </summary>
	private Vector3 ApplyCollisionAvoidance(Vector3 accel)
	{
		if (colsensor != null && accel.sqrMagnitude > 0.0001f)
		{
			Vector3 accDir = accel.normalized;
			colsensor.GetCollisionFreeDirection2(accDir, out accDir);
			accDir *= accel.magnitude;
			return accDir;
		}
		return accel;
	}

	// 计算与目标保持 stopDist 的站位点（在水平面上，沿与目标连线的反方向退开）
	private Vector3 GetKeepOutSpot(Vector3 targetPos, float stopDist)
	{
		Vector3 dir = transform.position - targetPos;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f)
		{
			dir = -transform.forward; // 防止重合时的零向量
		}
		dir.Normalize();
		Vector3 spot = targetPos + dir * stopDist;
		spot.y = transform.position.y;
		return spot;
	}

	// 当使用 CharacterController 地面移动时，手动应用重力以避免悬空漂浮
	private void ApplyGravityIfNeeded()
	{
		if (groundSteering == null || characterController == null) return;

		// 如果接地，轻推向下以稳定贴地；否则持续叠加重力
		if (characterController.isGrounded)
		{
			if (verticalVelocityY < -2f) verticalVelocityY = -2f;
			verticalVelocityY = Mathf.Min(verticalVelocityY, -2f);
		}
		else
		{
			verticalVelocityY += Physics.gravity.y * Time.deltaTime;
		}

		// 仅应用垂直位移；水平由 GroundSteering 负责
		Vector3 verticalMove = new Vector3(0f, verticalVelocityY, 0f) * Time.deltaTime;
		characterController.Move(verticalMove);
	}

	public void Disappearance()
	{
        if (!isDissolving)
        {
            StartCoroutine(DissolveCoroutine());
        }
	}

	protected override void Die()
	{
		if (isDead) return;

		base.Die();
		if (animator != null)
		{
			animator.SetBool("IsDead", true);
			animator.SetBool("IsMove", false);
			animator.SetBool("IsWalk", false);
			animator.SetBool("Attack", false);
		}

		if (deathSequenceCoroutine != null)
		{
			StopCoroutine(deathSequenceCoroutine);
		}
		deathSequenceCoroutine = StartCoroutine(DeathSequenceCoroutine());
	}

	private IEnumerator DeathSequenceCoroutine()
	{
		yield return new WaitForSeconds(deathDisappearDelay);
		Disappearance();
	}

	private IEnumerator DissolveCoroutine()
	{
        isDissolving = true;
        animator.applyRootMotion = isDead;
        while (dissolveAmount < 2f)
        {
            dissolveAmount += Time.deltaTime * dissolveSpeed;
            material.SetFloat("_DissolveAmount", dissolveAmount);
            yield return null;
        }
        material.SetFloat("_DissolveAmount", 0f);
        Destroy(gameObject);
	}
}
