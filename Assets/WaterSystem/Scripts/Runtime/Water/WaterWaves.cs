using UnityEngine;

/// <summary>
/// 存储波浪采样后的位移与法线信息
/// </summary>
public struct WaveStruct
{
    public Vector3 position; // 波浪位移（位移后的坐标偏移量）
    public Vector3 normal;   // 波面法线方向

    public WaveStruct(Vector3 position, Vector3 normal)
    {
        this.position = position;
        this.normal = normal;
    }

    /// <summary>
    /// 重置波浪信息
    /// </summary>
    public void Clear()
    {
        position = Vector3.zero;
        normal.Set(0, 1, 0); // 默认法线指向上方（平面朝上）
    }
}

/// <summary>
/// 水体（海洋）系统的主要波浪计算逻辑
/// 实现基于 Gerstner 波模型，可模拟叠加的多方向波动
/// </summary>
public partial class Water : MonoBehaviour
{
    [SerializeField]
    WavesSettings wavesSettings; // 波浪配置数据（幅度、方向、波长等）

    private int waveCount = 0;   // 当前启用的波浪数量
    private WaveStruct waveOut;  // 临时采样输出结构体

    /// <summary>
    /// 更新全局波浪数据（在每帧或波浪参数变化时调用）
    /// </summary>
    public void UpdateWaves()
    {
        // 从配置更新波浪数据
        wavesSettings.UpdateWavesData();

        waveCount = wavesSettings.GetWaveCount();

        // 向全局Shader传入波浪参数，用于GPU侧水面渲染
        if (waveCount > 0)
        {
            Shader.SetGlobalInt("_WaveCount", waveCount);
            Shader.SetGlobalVectorArray("waveData", wavesSettings.wavesData);
        }
    }

    /// <summary>
    /// 根据给定世界坐标(x, z)，计算该点的水面高度(y)
    /// 使用迭代修正法获得更精确的高度
    /// </summary>
    /// <param name="position">输入世界坐标（y值可任意）</param>
    /// <returns>当前时刻水面的高度</returns>
    public float GetWaterHeight(Vector3 position)
    {
        // 通过多次位移迭代逼近真实波面
        Vector3 displacement = GetWaterDisplacement(position);
        displacement = GetWaterDisplacement(position - displacement);
        displacement = GetWaterDisplacement(position - displacement);

        // 最终返回叠加后的Y方向位移（高度值）
        return GetWaterDisplacement(position - displacement).y;
    }

    /// <summary>
    /// 获取指定点的波浪位移（不包含原始位置）
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>该点的位移向量</returns>
    public Vector3 GetWaterDisplacement(Vector3 position)
    {
        waveOut.Clear();
        SampleWaves(position, out waveOut);
        return waveOut.position;
    }

    /// <summary>
    /// 计算单个 Gerstner 波在指定位置的位移与法线
    /// </summary>
    /// <param name="pos">二维位置（x,z）</param>
    /// <param name="waveCountMulti">波数量的倒数，用于控制叠加权重</param>
    /// <param name="amplitude">波高（振幅）</param>
    /// <param name="direction">波浪传播方向（角度，单位°）</param>
    /// <param name="wavelength">波长</param>
    /// <returns>该波的位移与法线</returns>
    public WaveStruct GerstnerWave(Vector2 pos, float waveCountMulti, float amplitude, float direction, float wavelength)
    {
        WaveStruct waveOut = new WaveStruct(Vector3.zero, Vector3.zero);

        float time = Time.time;

        // w = 2π / 波长（波数）
        float w = 6.28318f / wavelength;

        // 波速：根据重力加速度和波数推算（深水波理论）
        float wSpeed = Mathf.Sqrt(9.8f * w);

        // 峰值因子（用于控制波形锐度）
        float peak = 0.2f;

        // qi：波的“斜率因子”，影响水平位移幅度
        float qi = peak / (amplitude * w * waveCountMulti);

        // 将方向角从度数转为弧度
        direction = Mathf.Deg2Rad * direction;

        // 根据角度得到传播方向向量（注意：此处使用sin/cos组合）
        Vector2 dirWaveInput = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
        Vector2 windDir = dirWaveInput.normalized;

        // 投影到波的方向上，用于计算相位
        float dir = Vector2.Dot(windDir, pos);

        // 波动相位：kx - ωt
        float calc = dir * w - time * wSpeed;

        // 三角函数项
        float cosCalc = Mathf.Cos(calc);
        float sinCalc = Mathf.Sin(calc);

        // 位移计算：
        // x,z方向受cos影响（水平偏移），y方向受sin影响（高度）
        Vector3 wave = new Vector3(
            qi * amplitude * windDir.x * cosCalc,
            sinCalc * amplitude * waveCountMulti, // 高度缩放根据波数量平分
            qi * amplitude * windDir.y * cosCalc
        );

        // 法线计算：对Gerstner波曲面求偏导后归一化
        Vector3 n = new Vector3(
            -(windDir.x * w * amplitude * cosCalc),
            1 - (qi * w * amplitude * sinCalc),
            -(windDir.y * w * amplitude * cosCalc)
        ).normalized;

        waveOut.position = wave;
        waveOut.normal = n * waveCountMulti; // 平均化法线（多波叠加）

        return waveOut;
    }

    /// <summary>
    /// 对所有波进行叠加采样，输出综合位移与法线
    /// </summary>
    /// <param name="position">输入世界坐标</param>
    /// <param name="waveOut">输出：叠加后的波信息</param>
    public void SampleWaves(Vector3 position, out WaveStruct waveOut)
    {
        Vector2 pos = new Vector2(position.x, position.z);
        waveOut = new WaveStruct(Vector3.zero, Vector3.zero);

        if (waveCount == 0)
            return;

        // 每个波的权重因子（平均分布）
        float waveCountMulti = 1.0f / waveCount;

        // 遍历所有波参数叠加计算
        for (int i = 0; i < waveCount; i++)
        {
            Wave w = wavesSettings.waves[i];
            WaveStruct wave = GerstnerWave(pos, waveCountMulti, w.amplitude, w.direction, w.wavelength);

            // 叠加位移与法线
            waveOut.position += wave.position;
            waveOut.normal += wave.normal;
        }
    }
}
