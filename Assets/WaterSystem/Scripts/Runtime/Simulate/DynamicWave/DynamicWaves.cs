using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DynamicWaves
{
    // ==========================================================
    // Dynamic Waves - GPU级动态波浪数据系统
    // ==========================================================
    // 功能说明：
    // 1️⃣ 管理多级波浪贴图（Cascade LOD）的GPU存储 (RenderTextureArray)
    // 2️⃣ 构建每帧波浪渲染命令（CommandBuffer）
    // 3️⃣ 根据场景粒子特效生成扰动贴图 (Displacement / Foam)
    // 4️⃣ 向Shader提供波浪空间映射参数，用于采样与计算
    // ==========================================================

    private int _cascadeCount = 1;                        // 当前级联层数
    private RenderTexture _targets;                       // 存储波浪模拟结果的贴图数组（Tex2DArray）
    private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBHalf; // 半精度浮点纹理格式

    // 当前动态波纹贴图的分辨率（由 Water.Instance.CascadeResolution 控制）
    private int _resolution = -1;

    // 是否允许Compute Shader直接写入贴图数据（如果开启则enableRandomWrite = true）
    private bool _needToReadWriteTextureData = false;

    // 支持的最大LOD数量（防止Shader数组越界）
    public const int MAX_LOD_COUNT = 15;

    // 每级级联的空间参数
    private Vector4[] _param_CascadePosScales = new Vector4[MAX_LOD_COUNT + 1]; // (x,z:位置, y:未用, z:scale)
    private Vector4[] _param_CascadeSize = new Vector4[MAX_LOD_COUNT + 1];      // (texelWidth, textureRes, 1, 1/textureRes)

    // 上一帧数据（用于过渡/插值），第一帧不会初始化
    private Vector4[] _param_PrevCascadePosScales = new Vector4[MAX_LOD_COUNT + 1];
    private Vector4[] _param_PrevCascadeSize = new Vector4[MAX_LOD_COUNT + 1];

    // ----------------------------------------------------------
    // 构造函数
    // ----------------------------------------------------------
    public DynamicWaves() { }

    /// <summary>
    /// 初始化波浪系统。
    /// </summary>
    public void Init(int count)
    {
        _cascadeCount = count;
        InitData();
    }

    // ==========================================================
    // GPU资源创建
    // ==========================================================

    /// <summary>
    /// 创建用于存储每级级联波浪数据的 RenderTextureArray。
    /// 每个Array层代表一个 LOD（波浪贴图级联）。
    /// </summary>
    public static RenderTexture CreateCascadeDataTextures(
        int count,
        RenderTextureDescriptor desc,
        string name,
        bool needToReadWriteTextureData)
    {
        RenderTexture result = new RenderTexture(desc)
        {
            wrapMode = TextureWrapMode.Clamp,           // 边界处不重复
            antiAliasing = 1,                           // 不需要多重采样
            filterMode = FilterMode.Bilinear,           // 双线性插值采样
            anisoLevel = 0,
            useMipMap = false,
            name = name,
            dimension = TextureDimension.Tex2DArray,    // 关键点：多层级联数据
            volumeDepth = count,                        // 层数 = 级联数量
            enableRandomWrite = needToReadWriteTextureData // 是否可Compute写入
        };
        result.Create();
        return result;
    }

    /// <summary>
    /// 初始化波浪RenderTexture及全局Shader绑定。
    /// </summary>
    void InitData()
    {
        var resolution = Water.Instance.CascadeResolution;
        var desc = new RenderTextureDescriptor(resolution, resolution, _textureFormat, 0);

        // 创建波浪模拟贴图数组
        _targets = CreateCascadeDataTextures(_cascadeCount, desc, "Water Dynamic Wave Data", _needToReadWriteTextureData);

        // 向全局Shader注册该贴图
        Shader.SetGlobalTexture("Water_DynamicDisplacement", _targets);
    }

    // ==========================================================
    // 数据更新逻辑
    // ==========================================================

    /// <summary>
    /// 每帧更新波浪数据（更新位置缩放等参数，检查分辨率变化）
    /// </summary>
    public void UpdateData()
    {
        int width = Water.Instance.CascadeResolution;

        // 检查是否首次初始化
        if (_resolution == -1)
        {
            _resolution = width;
        }
        // 检测分辨率变化（例如在编辑器修改参数）
        else if (width != _resolution)
        {
            _targets.Release();
            _targets.width = _targets.height = _resolution;
            _targets.Create();

            _resolution = width;
        }

        var lt = Water.Instance._cascadeTransform;

        // 遍历所有级联层，更新每级的位置信息与空间参数
        for (int i = 0; i < _cascadeCount; i++)
        {
            _param_CascadePosScales[i] = new Vector4(
                lt._renderData[i]._posSnapped.x,  // X 方向世界坐标
                lt._renderData[i]._posSnapped.z,  // Z 方向世界坐标
                Water.Instance.CalcLodScale(i),   // 级联缩放比例
                0f);

            _param_CascadeSize[i] = new Vector4(
                lt._renderData[i]._texelWidth,    // 单像素对应的世界尺寸
                lt._renderData[i]._textureRes,    // 当前贴图分辨率
                1f,
                1f / lt._renderData[i]._textureRes);
        }

        // 🔧 复制最后一个元素，防止Shader访问越界（如访问 index+1）
        _param_CascadePosScales[_cascadeCount] = _param_CascadePosScales[_cascadeCount - 1];
        _param_CascadeSize[_cascadeCount] = _param_CascadeSize[_cascadeCount - 1];
        _param_CascadeSize[_cascadeCount].z = 0f; // 标记为无效层
    }

    // ==========================================================
    // CommandBuffer 构建逻辑
    // ==========================================================

    /// <summary>
    /// 为每个级联构建渲染命令：
    /// - 清空目标贴图
    /// - 绘制所有波浪扰动粒子（ParticleSystem）
    /// </summary>
    public void BuildCommandBuffer(CommandBuffer cmd)
    {
        for (int i = _cascadeCount - 1; i >= 0; i--)
        {
            // 将当前级联的RenderTexture层作为渲染目标
            cmd.SetRenderTarget(_targets, _targets, 0, CubemapFace.Unknown, i);

            // 清空当前层的颜色缓存（不清深度）
            cmd.ClearRenderTarget(false, true, new Color(0f, 0f, 0f, 0f));

            // 绘制波浪扰动（粒子特效）
            SubmitDynamicDraws(i, cmd);
        }
    }

    /// <summary>
    /// 实际执行绘制的函数：
    /// - 设置相机视图与投影矩阵（由CascadeTransform计算）
    /// - 绘制所有波浪粒子系统
    /// </summary>
    void SubmitDynamicDraws(int id, CommandBuffer cmd)
    {
        var lt = Water.Instance._cascadeTransform;

        // 设置当前级联的ViewProjection矩阵到Shader
        lt.SetViewProjectionMatrices(id, cmd);

        // 遍历所有粒子系统（波纹扰动）
        foreach (var particle in Water.Instance.GetWaveParticles())
        {
            var renderer = particle.GetComponent<ParticleSystemRenderer>();
            if (renderer && renderer.sharedMaterial)
            {
                // 提交绘制命令：粒子渲染器 + 材质
                cmd.DrawRenderer(renderer, renderer.sharedMaterial, 0, 0);
            }
        }
    }

    // ==========================================================
    // 向Shader传递全局变量
    // ==========================================================

    /// <summary>
    /// 将所有级联的位置信息和尺寸参数推送到Shader中。
    /// Shader中通常通过 Water_CascadePosScale[] 与 Water_CascadeSize[] 数组读取。
    /// </summary>
    public void SetGlobalShaderVariables()
    {
        Shader.SetGlobalVectorArray("Water_CascadePosScale", _param_CascadePosScales);
        Shader.SetGlobalVectorArray("Water_CascadeSize", _param_CascadeSize);
    }
}
