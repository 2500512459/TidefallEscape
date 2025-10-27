using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 用于管理每个 LOD（层级）水波模拟区域的空间变换信息
/// 主要负责在世界空间中对每个级联（Cascade）的网格位置进行对齐（Snap）
/// 以确保水面波纹在摄像机移动时不会“滑动”
/// </summary>
public class CascadeTransform
{
    /// <summary>
    /// 单个级联层的渲染数据
    /// 记录当前 LOD 区域在世界空间中的坐标、分辨率、以及贴图采样精度
    /// </summary>
    [System.Serializable]
    public struct RenderData
    {
        // 单个像素对应的世界空间宽度（即每个 texel 的尺寸）
        public float _texelWidth;

        // 当前层级纹理分辨率
        public float _textureRes;

        // 当前层级在世界空间中对齐后的中心位置（已 Snap）
        public Vector3 _posSnapped;

        /// <summary>
        /// 计算当前级联层覆盖的 XZ 平面区域矩形
        /// 用于判断某区域是否在当前水面模拟范围内
        /// </summary>
        public Rect RectXZ
        {
            get
            {
                float w = _texelWidth * _textureRes;
                return new Rect(_posSnapped.x - w / 2f, _posSnapped.z - w / 2f, w, w);
            }
        }
    }

    // 渲染用的当前帧数据
    public RenderData[] _renderData = null;

    // 上一帧的渲染数据（用于插值或稳定更新）
    public RenderData[] _renderDataSource = null;

    // 级联层数量（通常 1~4）
    public int CascadeCount { get; private set; }

    // 每个 LOD 的世界到相机矩阵（相当于 ViewMatrix）
    Matrix4x4[] _worldToCameraMatrix;

    // 每个 LOD 的正交投影矩阵
    Matrix4x4[] _projectionMatrix;

    public Matrix4x4 GetWorldToCameraMatrix(int lodIdx) { return _worldToCameraMatrix[lodIdx]; }
    public Matrix4x4 GetProjectionMatrix(int lodIdx) { return _projectionMatrix[lodIdx]; }

    /// <summary>
    /// 初始化级联数据结构
    /// </summary>
    public void InitCascadeData(int count)
    {
        CascadeCount = count;

        _renderData = new RenderData[count];
        _renderDataSource = new RenderData[count];
        _worldToCameraMatrix = new Matrix4x4[count];
        _projectionMatrix = new Matrix4x4[count];
    }

    /// <summary>
    /// 更新所有级联层的空间变换
    /// 根据观察者（Viewer）位置动态对齐网格
    /// 以保证模拟网格相对视角不滑动
    /// </summary>
    public void UpdateTransforms()
    {
        // 获取观察者（摄像机）的位置
        Transform viewerTransform = Water.Instance.GetViewer();
        if (viewerTransform == null)
        {
            Debug.Log("Set a Viewer for Water!");
            viewerTransform = Water.Instance.transform;
        }

        // 遍历所有级联层
        for (int lodIdx = 0; lodIdx < CascadeCount; lodIdx++)
        {
            // 保存上一帧数据
            _renderDataSource[lodIdx] = _renderData[lodIdx];

            // 当前 LOD 的缩放比例（决定每层覆盖的水面面积）
            var lodScale = Water.Instance.CalcLodScale(lodIdx);

            // 摄像机正交视锥高度（以层级比例决定）
            var camOrthSize = 2f * lodScale;

            // 当前层的纹理分辨率
            _renderData[lodIdx]._textureRes = Water.Instance.CascadeResolution;

            // 单个像素对应的世界空间宽度（分辨率越高，单个像素越小）
            _renderData[lodIdx]._texelWidth = 2f * camOrthSize / _renderData[lodIdx]._textureRes;

            // Snap 操作：将水面网格位置对齐到 texel 边界
            // 使水波在视角移动时不会“滑动”
            _renderData[lodIdx]._posSnapped = viewerTransform.position
                - new Vector3(
                    Mathf.Repeat(viewerTransform.position.x, _renderData[lodIdx]._texelWidth),
                    0f,
                    Mathf.Repeat(viewerTransform.position.z, _renderData[lodIdx]._texelWidth)
                  );

            // 首帧初始化（防止未赋值造成的数值问题）
            if (_renderDataSource[lodIdx]._textureRes == 0f)
            {
                _renderDataSource[lodIdx]._posSnapped = _renderData[lodIdx]._posSnapped;
                _renderDataSource[lodIdx]._texelWidth = _renderData[lodIdx]._texelWidth;
                _renderDataSource[lodIdx]._textureRes = _renderData[lodIdx]._textureRes;
            }

            // 构建当前 LOD 的世界到相机矩阵（右手坐标系）
            // 相机位于水面上方（+Y 方向 100），朝下（绕 X 轴旋转 90°）
            _worldToCameraMatrix[lodIdx] = CalculateWorldToCameraMatrixRHS(
                _renderData[lodIdx]._posSnapped + Vector3.up * 100f,
                Quaternion.AngleAxis(90f, Vector3.right)
            );

            // 构建正交投影矩阵，覆盖当前级联区域
            _projectionMatrix[lodIdx] = Matrix4x4.Ortho(
                -camOrthSize, camOrthSize,
                -camOrthSize, camOrthSize,
                1f, 200f
            );
        }
    }

    /// <summary>
    /// 计算右手坐标系的世界到相机矩阵
    /// 注意 Unity 默认是左手系，需要在 Z 轴取反
    /// </summary>
    public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
    {
        return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
    }

    /// <summary>
    /// 将当前 LOD 的视图矩阵与投影矩阵绑定到命令缓冲中
    /// 供动态波纹绘制使用
    /// </summary>
    public void SetViewProjectionMatrices(int lodIdx, CommandBuffer cmd)
    {
        cmd.SetViewProjectionMatrices(GetWorldToCameraMatrix(lodIdx), GetProjectionMatrix(lodIdx));
    }
}
