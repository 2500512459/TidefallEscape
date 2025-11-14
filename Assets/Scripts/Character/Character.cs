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
    }
    public virtual void TakeManaPoints(float manaPoints)
    {
        Attribute mp = attributesModule.attributes[AttributeType.MP];
        
        float mpValue = mp.Value;
        mpValue -= manaPoints;
        attributesModule.SetAttributeValue(AttributeType.MP, mpValue);
    }
}
