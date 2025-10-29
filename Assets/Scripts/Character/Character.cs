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
    // 角色动画控制器引用（用于播放角色动画）
    protected Animator animator;

    // 角色物理刚体引用（用于物理交互与力学控制）
    protected Rigidbody rgBody;

    /// <summary>
    /// Unity生命周期：Awake()
    /// 在所有Start之前调用，常用于组件缓存与初始化（留空供子类扩展）
    /// </summary>
    protected virtual void Awake()
    {

    }

    /// <summary>
    /// Unity生命周期：Start()
    /// 在Awake之后调用，常用于启动逻辑（留空供子类扩展）
    /// </summary>
    protected virtual void Start()
    {

    }

    /// <summary>
    /// Unity生命周期：OnEnable()
    /// 当GameObject启用时自动调用：
    /// - 向全局 CharacterManager 注册自己
    /// - 获取基础组件（Rigidbody、Animator）
    /// </summary>
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

        // 缓存组件
        rgBody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Unity生命周期：OnDisable()
    /// 当GameObject禁用或销毁时自动调用：
    /// - 从CharacterManager反注册（防止内存残留）
    /// </summary>
    protected virtual void OnDisable()
    {
        CharacterManager manager = CharacterManager.Instance;

        if (manager != null)
        {
            manager.Unregister(this);
        }
    }

    /// <summary>
    /// Unity生命周期：Update()
    /// 每帧调用（供子类覆写实现逻辑）
    /// </summary>
    protected virtual void Update()
    {

    }

    /// <summary>
    /// 获取角色刚体引用
    /// </summary>
    public Rigidbody GetRigidBody() { return rgBody; }
}
