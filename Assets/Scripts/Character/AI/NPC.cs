using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class NPC : Character
{
    [Header("NPC参数")]
    private NPCAnimationController animationController;
    public HealthBar healthBar;
    public Material material;
    public float maxHealth = 100;

    [Header("视野参数")]
    public float viewRadius = 8f;              // 视野半径
    [Range(0, 360f)]
    public float viewAngle = 90f;              // 扇形角度
    public float eyeHeight = 1.6f;
    public LayerMask targetMask;               // Player层
    public LayerMask obstacleMask;             // 障碍层（可选）

    [Header("Rig控制")]
    public Rig lookRig;                        // 子物体下的Rig
    public float rigLerpSpeed = 3f;            // 平滑过渡速度

    [Header("死亡掉落设置")]
    [Tooltip("角色死亡后生成的宝箱预制体（可选）")]
    public GameObject treasureBoxPrefab;
    [Tooltip("宝箱的物品生成库（可选）")]
    public LootContainerSO lootContainerSO;

    [SerializeField] private BaseInteractable interactable;
    private Transform player;
    private Collider npcCollider;
    private bool playerInSight = false;

    private bool isDissolving = false;
    private float dissolveSpeed = 3f;
    private float dissolveAmount = 0f;
    private Renderer re;

    protected override void Awake()
    {
        base.Awake();
        animationController = GetComponent<NPCAnimationController>();
        npcCollider = GetComponent<Collider>();
        re = GetComponentInChildren<Renderer>();
    }

    protected override void Start()
    {
        base.Start();
        attributesModule.AddAttribute(AttributeType.Hp, maxHealth, 0, maxHealth);
        healthBar.SetMaxHealth(maxHealth);
        healthBar.gameObject.SetActive(false);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected override void Update()
    {
        if (player == null) return;
        CheckView();
        UpdateRigWeight();
    }

    /// <summary>
    /// 检测玩家是否在扇形范围内
    /// </summary>
    void CheckView()
    {
        playerInSight = false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = (player.position - eyePos).normalized;
        float distance = Vector3.Distance(eyePos, player.position);

        if (distance <= viewRadius)
        {
            float angleBetween = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleBetween < viewAngle / 2f)
            {
                if (!Physics.Raycast(eyePos, dirToPlayer, distance, obstacleMask))
                {
                    playerInSight = true;
                }
            }
        }
    }

    /// <summary>
    /// 平滑控制Rig权重
    /// </summary>
    void UpdateRigWeight()
    {
        if (lookRig == null) return;
        float targetWeight = playerInSight ? 1f : 0f;
        lookRig.weight = Mathf.Lerp(lookRig.weight, targetWeight, Time.deltaTime * rigLerpSpeed);
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        animationController.PlayHit();
        healthBar.gameObject.SetActive(true);
        healthBar.SetHealth(attributesModule.GetAttributeValue(AttributeType.Hp));

        if (isDead)
        {
            animationController.PlayDeath();
            healthBar.gameObject.SetActive(false);
            if (interactable != null)
                interactable.OnLoseFocus(this);
        }
    }
    protected override void Die()
    {
        base.Die();
        if (npcCollider != null)
            npcCollider.enabled = false;
    }
    // 动画事件中调用
    public void Dissolution()
    {
        if (!isDissolving)
        {
             // 保存原贴图
            Texture tex = re.material.GetTexture("_MainTex");
            // 克隆一个 Dissolve 材质实例避免互相影响
            Material dissolveMat = new Material(material);
            dissolveMat.SetTexture("_MainTex", tex);
            // 切换材质
            re.material = dissolveMat;
            // 使用这个实例进行溶解
            material = dissolveMat;
            StartCoroutine(DissolveCoroutine());
        }
    }

    private IEnumerator DissolveCoroutine()
    {
        isDissolving = true;
        animator.applyRootMotion = isDead;
        while (dissolveAmount < 10f)
        {
            dissolveAmount += Time.deltaTime * dissolveSpeed;
            material.SetFloat("_DissolveAmount", dissolveAmount);
            yield return null;
        }
        material.SetFloat("_DissolveAmount", 0f);
        Destroy(gameObject);
    }

    /// <summary>
    /// 在角色死亡位置生成宝箱
    /// </summary>
    protected override void SpawnTreasureBox()
    {
        // 如果未配置宝箱预制体或掉落物，则不生成
        if (treasureBoxPrefab == null || lootContainerSO == null)
            return;

        // 在角色位置生成宝箱
        Vector3 spawnPosition = transform.position;
        GameObject treasureBoxObj = Instantiate(treasureBoxPrefab, spawnPosition, Quaternion.identity);

        // 获取宝箱组件并设置掉落物
        TreasureBox treasureBox = treasureBoxObj.GetComponent<TreasureBox>();
        if (treasureBox != null)
        {
            treasureBox.lootContainerData = lootContainerSO;
            Debug.Log($"[NPC] {name} 死亡后生成了宝箱，掉落物库: {lootContainerSO.name}");
        }
        else
        {
            Debug.LogWarning($"[NPC] {name} 生成的宝箱预制体缺少 TreasureBox 组件！");
        }
    }

    // 在Scene中绘制视野范围（辅助）
    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(eyePos, viewRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePos, eyePos + leftBoundary * viewRadius);
        Gizmos.DrawLine(eyePos, eyePos + rightBoundary * viewRadius);
    }
}
