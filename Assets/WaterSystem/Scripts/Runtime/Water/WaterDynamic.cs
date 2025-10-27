using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class Water : MonoBehaviour
{
    // ==========================================================
    // 动态水面系统 (Dynamic Waves System)
    // ==========================================================
    // 该模块负责：
    // 1️⃣ 管理级联（Cascade）区域的空间变换与分辨率配置
    // 2️⃣ 初始化 GPU 波浪模拟器 (DynamicWaves)
    // 3️⃣ 通过 CommandBuffer 驱动波浪更新与渲染数据传输
    // 4️⃣ 管理场景中额外的波浪粒子（如冲击波、扰动粒子系统）
    // ----------------------------------------------------------

    // ========== 【区域与分辨率参数】 ==========

    [Header("Area Transform")]
    [Tooltip("用于控制水体模拟区域的整体缩放比例（影响LOD间距与波长）")]
    public float _dynamicScale = 10;

    //[Range(1, 16)]
    [Tooltip("级联层数，决定波浪LOD层数（通常为1~4）")]
    public int _cascadeCount = 1;

    /// <summary>
    /// 枚举定义每级波浪模拟贴图的分辨率。
    /// 高分辨率提供更精细的波浪细节，但会显著增加显存与计算负担。
    /// </summary>
    public enum CascadeResolutionLevel
    {
        RES_256x256,
        RES_512x512,
        RES_1024x1024,
    }

    [SerializeField, Tooltip("级联波浪贴图的基础分辨率")]
    public CascadeResolutionLevel _cascadeResolution = CascadeResolutionLevel.RES_512x512;

    /// <summary>
    /// 将枚举转换为整数分辨率值。
    /// </summary>
    public int CascadeResolution
    {
        get
        {
            switch (_cascadeResolution)
            {
                case CascadeResolutionLevel.RES_256x256:
                    return 256;
                case CascadeResolutionLevel.RES_512x512:
                    return 512;
                case CascadeResolutionLevel.RES_1024x1024:
                    return 1024;
            }
            return 512;
        }
    }

    /// <summary>
    /// 当前级联的基础缩放比例。
    /// 控制每一级LOD波浪区域的物理尺寸。
    /// </summary>
    public float Scale
    {
        get { return _dynamicScale; }
        set { _dynamicScale = value; }
    }

    /// <summary>
    /// 计算某级LOD的缩放比例。
    /// 通常使用 2^LOD 递增，使每级范围扩大一倍（常见于海洋级联渲染算法）。
    /// </summary>
    public float CalcLodScale(float lodIndex)
    {
        return Scale * Mathf.Pow(2f, lodIndex);
    }

    /// <summary>
    /// 计算指定LOD的网格大小（每个像素在世界空间的大小）。
    /// 值越小 → 波浪模拟越精细。
    /// </summary>
    public float CalcGridSize(int lodIndex)
    {
        return CalcLodScale(lodIndex) / CascadeResolution;
    }

    // ==========================================================
    // 动态波浪模拟组件
    // ==========================================================

    //[HideInInspector] 
    [Tooltip("级联变换控制器，管理每级波浪区域的平移/缩放/中心位置")]
    [HideInInspector]
    public CascadeTransform _cascadeTransform;

    [Tooltip("波浪模拟控制器（可能包含FFT/Gerstner/高度场模拟等）")]
    private DynamicWaves _waveMgr;

    // 存储场景中附加的波浪粒子特效（如物体落水、子弹击水等）
    private List<ParticleSystem> _waveParticles = new List<ParticleSystem>();

    // ==========================================================
    // 初始化函数
    // ==========================================================

    /// <summary>
    /// 初始化动态波浪系统：
    /// - 创建并初始化 CascadeTransform（空间级联管理器）
    /// - 创建并初始化 DynamicWaves（波浪数据模拟器）
    /// </summary>
    public void InitDynamicWaves()
    {
        // 初始化级联空间变换
        if (_cascadeTransform == null)
        {
            _cascadeTransform = new CascadeTransform();
            _cascadeTransform.InitCascadeData(_cascadeCount);
        }

        // 初始化波浪模拟器
        if (_waveMgr == null)
        {
            _waveMgr = new DynamicWaves();
            _waveMgr.Init(_cascadeCount);
        }
    }

    // ==========================================================
    // 波浪模拟更新函数
    // ==========================================================

    /// <summary>
    /// 每帧调用，用于执行波浪模拟的 GPU 更新。
    /// 核心流程：
    /// 1️⃣ 获取 CommandBuffer
    /// 2️⃣ 更新级联空间变换（CascadeTransform）
    /// 3️⃣ 更新波浪模拟数据（DynamicWaves.UpdateData）
    /// 4️⃣ 向全局Shader推送波浪参数
    /// 5️⃣ 构建波浪模拟的渲染命令
    /// 6️⃣ 执行并释放 CommandBuffer
    /// </summary>
    public void UpdateDynamicWaves()
    {
        // 从对象池中获取命令缓冲区（避免频繁分配内存）
        CommandBuffer cmd = CommandBufferPool.Get("Water Dynamic Simulation");

        // 更新空间与数据
        _cascadeTransform?.UpdateTransforms();
        _waveMgr?.UpdateData();
        _waveMgr?.SetGlobalShaderVariables();

        // 生成波浪模拟命令（如 FFT 计算 / 高度贴图更新）
        _waveMgr?.BuildCommandBuffer(cmd);

        // 提交渲染命令到 GPU
        Graphics.ExecuteCommandBuffer(cmd);

        // 回收命令缓冲区
        CommandBufferPool.Release(cmd);
    }

    // ==========================================================
    // 波浪粒子系统接口
    // ==========================================================

    /// <summary>
    /// 获取当前所有波浪粒子系统引用（如水花特效）
    /// </summary>
    public List<ParticleSystem> GetWaveParticles()
    {
        return _waveParticles;
    }

    /// <summary>
    /// 注册一个新的波浪粒子（例如：落水冲击波特效）
    /// </summary>
    public void AddWaveParticle(ParticleSystem particle)
    {
        _waveParticles.Add(particle);
    }

    /// <summary>
    /// 从波浪粒子列表中移除指定特效
    /// </summary>
    public void RemoveWaveParticle(ParticleSystem particle)
    {
        _waveParticles.Remove(particle);
    }
}
