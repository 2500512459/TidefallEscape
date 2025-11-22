using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Character（角色基类）
/// 所有可被场景管理的角色对象（包括玩家与AI）都应继承此类。
/// 提供角色注册、刚体、动画机的统一初始化逻辑。
/// </summary>
public class Character : MonoBehaviour
{
    protected AttributesModule attributesModule;    // 属性模块
    protected Animator animator;
    protected Rigidbody rgBody;
    protected bool isDead = false;
    [Header("Treasure Spawn Timing")]
    [Tooltip("角色死亡后生成宝箱的延迟时间（秒）")]
    [SerializeField] protected float treasureSpawnDelay = 1.5f;
    private Coroutine treasureSpawnCoroutine;

    protected virtual void Awake()
    {
        // 缓存组件
        rgBody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        attributesModule = GetComponent<AttributesModule>();
    }

    protected virtual void Start()
    {

    }

    protected virtual void OnEnable()
    {
        // 获取单例管理器实例
        CharacterManager manager = CharacterManager.Instance;

        if (manager != null)
        {
            // 将自身注册到全局角色列表中
            manager.Register(this);
        }
        else
        {
            Debug.Log("CharacterManager is Null!");
        }
    }
    protected virtual void OnDisable()
    {
        CharacterManager manager = CharacterManager.Instance;

        if (manager != null)
        {
            manager.Unregister(this);
        }
    }

    protected virtual void Update()
    {

    }

    /// <summary>
    /// 获取角色刚体引用
    /// </summary>
    public Rigidbody GetRigidBody() { return rgBody; }


    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        // 获取当前角色的属性模块中的生命值属性
        Attribute hp = attributesModule.attributes[AttributeType.Hp];

        float hpValue = hp.Value;
        hpValue -= damage;
        hpValue = Mathf.Max(0, hpValue);
        attributesModule.SetAttributeValue(AttributeType.Hp, hpValue);
        if (hpValue <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        QuestManager.Instance.UpdateQuestProgress(name, 1);
        Debug.Log("角色死亡:" + name);
        EventManager.Raise(new CharacterDeathMessage(this));

        // 生成宝箱（如果配置了预制体和掉落物）
        if (treasureSpawnCoroutine != null)
        {
            StopCoroutine(treasureSpawnCoroutine);
        }
        treasureSpawnCoroutine = StartCoroutine(SpawnTreasureBoxAfterDelay());
    }

    /// <summary>
    /// 在角色死亡位置生成宝箱（子类可重写）
    /// </summary>
    protected virtual void SpawnTreasureBox()
    {
        // 基类默认不生成宝箱，由子类实现
    }

    private IEnumerator SpawnTreasureBoxAfterDelay()
    {
        if (treasureSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(treasureSpawnDelay);
        }
        SpawnTreasureBox();
    }
    public virtual void TakeManaPoints(float manaPoints)
    {
        Attribute mp = attributesModule.attributes[AttributeType.MP];
        
        float mpValue = mp.Value;
        mpValue -= manaPoints;
        attributesModule.SetAttributeValue(AttributeType.MP, mpValue);
    }

    [ContextMenu("调试/打印角色属性")]
    protected void PrintAttributes()
    {
        if (attributesModule == null || attributesModule.attributes == null)
        {
            Debug.LogWarning($"[{name}] AttributesModule 未初始化。");
            return;
        }

        foreach (var pair in attributesModule.attributes)
        {
            Attribute attribute = pair.Value;
            string msg = $"{name} 属性 {pair.Key}: 当前 {attribute.Value}, 范围 [{attribute.MinValue}, {attribute.MaxValue}]";
            Debug.Log(msg);
        }
    }
}
