using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public partial class Water : MonoBehaviour
{
    // ================================
    // 水体颜色 LUT (Color Ramp) 系统
    // ================================
    // 本模块主要用于生成和维护一张 1D LUT 贴图（实际上是 2 行 128 列的 2D 贴图）
    // 用于水体 Shader 内进行“吸收”与“散射”颜色查找。
    // LUT 的上半行存储吸收色 (Absorption)，下半行存储散射色 (Scattering)。
    // Shader 在采样时通过不同行（或坐标偏移）获得两种颜色效果。

    [SerializeField]
    private ColorsPreset colorsPreset; // 引用 ScriptableObject，定义颜色渐变参数（吸收/散射）

    private Texture2D rampTexture; // 存放最终生成的 LUT 纹理（128x2 RGBA 贴图）

    /// <summary>
    /// 初始化 LUT 贴图并设置到全局 Shader 变量中。
    /// 如果还未生成，则自动调用 GenerateColorRamp() 生成新的 ramp 贴图。
    /// </summary>
    private void InitLUT()
    {
        // 如果 rampTexture 尚未生成，先创建
        if (!rampTexture)
            GenerateColorRamp();

        // 将生成的 LUT 设置为全局纹理，供所有水体 Shader 使用
        // Shader 内部可通过 _AbsorptionScatteringRamp 获取此贴图
        Shader.SetGlobalTexture("_AbsorptionScatteringRamp", rampTexture);
    }

    /// <summary>
    /// 销毁 LUT 纹理以释放内存。
    /// 在编辑器模式下使用 DestroyImmediate（避免延迟销毁），
    /// 在运行时使用普通 Destroy。
    /// </summary>
    private void DestroyLUT()
    {
        if (rampTexture)
        {
            if (Application.isEditor)
                DestroyImmediate(rampTexture);
            else
                Destroy(rampTexture);
        }
    }

    /// <summary>
    /// 生成颜色渐变查找表 (Color Ramp LUT)
    /// LUT 分辨率：宽 128，高 2
    ///   第一行 (y = 0)：吸收颜色（_absorptionRamp）
    ///   第二行 (y = 1)：散射颜色（_scatterRamp）
    /// 渐变采样范围：[0, 1)
    /// </summary>
    private void GenerateColorRamp()
    {
        // 创建一张 128x2 RGBA8_SRGB 纹理，非线性空间（因为颜色数据通常是 SRGB）
        if (rampTexture == null)
            rampTexture = new Texture2D(128, 2, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);

        // 防止重复边界采样时产生接缝
        rampTexture.wrapMode = TextureWrapMode.Clamp;

        // 颜色缓存区，总大小为 128 * 2 = 256
        var cols = new Color[256];

        // ---------- 第一行：吸收色 ----------
        // 根据颜色渐变曲线（Gradient.Evaluate）按比例采样
        for (var i = 0; i < 128; i++)
        {
            // Evaluate 参数范围 [0,1)，除以 128f 获取插值因子
            cols[i] = colorsPreset._absorptionRamp.Evaluate(i / 128f);
        }

        // ---------- 第二行：散射色 ----------
        for (var i = 0; i < 128; i++)
        {
            cols[i + 128] = colorsPreset._scatterRamp.Evaluate(i / 128f);
        }

        // 将颜色数组写入纹理并应用更改
        rampTexture.SetPixels(cols);
        rampTexture.Apply();
    }
}
