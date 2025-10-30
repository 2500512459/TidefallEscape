using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// 平面反射系统（Planar Reflection System）
///
/// 实现原理：
/// 为反射平面（如水面）创建一个镜像摄像机，
/// 通过反射矩阵计算镜像相机的姿态，
/// 渲染出反射贴图并传递给 Shader 用于实时反射。
///
/// 适用范围：
/// - 水面反射
/// - 镜子、地板反射
/// - 任何基于平面反射的场景
/// </summary>
//[ExecuteAlways]  // 在编辑器中也能实时执行
public class PlanarReflections : MonoBehaviour
{
    #region ======== 可调参数 (Inspector 设置) ========

    [Header("Reflection Settings")]
    [Range(0.1f, 1.0f)] public float reflectionQuality = 0.5f;  // 反射贴图分辨率比例（相对于主相机）
    [Range(1, 4)] public int textureID = 1;                     // 绑定到 Shader 的贴图编号（可支持多层反射）
    public float farClipPlane = 100f;                           // 反射相机远裁剪面
    public LayerMask reflectionLayers = -1;                     // 反射可见的层（LayerMask）
    public bool renderSkybox = true;                            // 是否在反射中渲染天空盒

    [Header("Custom Reflection Plane")]
    public bool useCustomNormal = false;                        // 是否使用自定义平面法线
    public Vector3 customNormal = Vector3.up;                   // 自定义反射平面法线

    #endregion

    #region ======== 内部状态与缓存 ========

    private Camera reflectionCamera;                            // 反射相机
    private GameObject reflectionCameraGO;                      // 相机挂载对象（隐藏在层级中）
    private readonly Dictionary<Camera, RenderTexture> cameraTextures = new(); // 每个主相机对应的反射贴图
    private readonly HashSet<Camera> ignoredCameras = new();    // 忽略反射的相机集合（避免递归）

    #endregion


    #region ======== MonoBehaviour 生命周期 ========

    private void OnEnable()
    {
        // 可在此注册 SRP 回调：
        // RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        Cleanup();
        // RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion


    #region ======== 反射相机管理 ========

    /// <summary>
    /// 初始化反射相机（仅在首次调用时创建）
    /// </summary>
    private void InitializeReflectionCamera()
    {
        if (reflectionCamera != null) return;

        reflectionCameraGO = new GameObject("ReflectionCamera", typeof(Camera));
        reflectionCameraGO.hideFlags = HideFlags.HideAndDontSave; // 不显示在层级面板中
        reflectionCamera = reflectionCameraGO.GetComponent<Camera>();
        reflectionCamera.enabled = false;                         // 手动控制渲染
    }

    /// <summary>
    /// 释放反射贴图与相机资源
    /// </summary>
    private void Cleanup()
    {
        foreach (var texture in cameraTextures.Values)
            texture.Release();
        cameraTextures.Clear();

        if (reflectionCamera == null) return;

        if (Application.isEditor)
            DestroyImmediate(reflectionCameraGO);
        else
            Destroy(reflectionCameraGO);
    }

    #endregion


    #region ======== 反射渲染流程 ========

    /// <summary>
    /// 每帧更新：生成反射相机的视图并渲染反射贴图
    /// </summary>
    private void Update()
    {
        InitializeReflectionCamera();

        // 获取反射平面的法线方向（可自定义）
        var normal = GetReflectionNormal();

        // 复制主摄像机的设置到反射摄像机
        ConfigureReflectionCamera(Camera.main);

        // 创建/更新反射贴图
        UpdateRenderTexture(Camera.main);

        // 更新反射相机的位置与朝向（镜像）
        UpdateReflectionCameraTransform(Camera.main, normal);

        // 设置带倾斜裁剪的投影矩阵，使反射面上方的内容被剔除
        SetupObliqueProjMatrix(normal);

        // 创建标准渲染请求（RenderPipeline API）
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

        if (RenderPipeline.SupportsRenderRequest(reflectionCamera, request))
        {
            // 输出目标设置为相应反射贴图
            request.destination = cameraTextures[Camera.main];

            // 执行渲染请求（等价于单独渲染此相机）
            RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
        }

        // 将生成的反射贴图绑定到 Shader 全局属性
        var textureName = $"_PlanarReflectionsTex{textureID}";
        reflectionCamera.targetTexture.SetGlobalShaderProperty(textureName);
    }

    /// <summary>
    /// 判断是否跳过某个相机的反射渲染
    /// 避免递归反射、编辑器预览相机等
    /// </summary>
    private bool ShouldSkipCamera(Camera camera)
    {
        return camera.cameraType == CameraType.Reflection ||
               camera.cameraType == CameraType.Preview ||
               ignoredCameras.Contains(camera);
    }

    /// <summary>
    /// 从主相机复制设置到反射相机
    /// </summary>
    private void ConfigureReflectionCamera(Camera sourceCamera)
    {
        reflectionCamera.CopyFrom(sourceCamera);
        reflectionCamera.cameraType = CameraType.Reflection;
        reflectionCamera.usePhysicalProperties = false;
        reflectionCamera.farClipPlane = farClipPlane;
        reflectionCamera.cullingMask = reflectionLayers;

        // 是否渲染天空盒（否则使用纯色清空）
        reflectionCamera.clearFlags = renderSkybox
            ? sourceCamera.clearFlags
            : CameraClearFlags.SolidColor;
    }

    /// <summary>
    /// 根据主相机分辨率与质量设置更新反射贴图
    /// </summary>
    private void UpdateRenderTexture(Camera sourceCamera)
    {
        int width = Mathf.RoundToInt(sourceCamera.pixelWidth * reflectionQuality);
        int height = Mathf.RoundToInt(sourceCamera.pixelHeight * reflectionQuality);

        // 若当前不存在或尺寸不匹配，则重新创建
        if (!cameraTextures.TryGetValue(sourceCamera, out RenderTexture texture) ||
            texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null)
            {
                texture.Release();
                cameraTextures.Remove(sourceCamera);
            }

            texture = new RenderTexture(width, height, 24)
            {
                name = $"PlanarReflection_{sourceCamera.name}",
                autoGenerateMips = true
            };
            texture.Create();

            cameraTextures[sourceCamera] = texture;
        }

        reflectionCamera.targetTexture = texture;
    }

    #endregion


    #region ======== 反射数学计算部分 ========

    /// <summary>
    /// 获取反射平面的法线
    /// </summary>
    private Vector3 GetReflectionNormal()
    {
        if (!useCustomNormal) return transform.up;
        return customNormal.normalized;
    }

    /// <summary>
    /// 更新反射相机的空间位置与旋转
    /// 通过向法线反射主相机的位置与方向
    /// </summary>
    private void UpdateReflectionCameraTransform(Camera sourceCamera, Vector3 normal)
    {
        // 主相机到平面的投影距离
        Vector3 proj = normal * Vector3.Dot(
            normal, sourceCamera.transform.position - transform.position);

        // 将相机位置沿法线方向对称
        reflectionCamera.transform.position = sourceCamera.transform.position - 2 * proj;

        // 将相机方向向量沿法线反射
        Vector3 forward = Vector3.Reflect(sourceCamera.transform.forward, normal);
        Vector3 up = Vector3.Reflect(sourceCamera.transform.up, normal);

        // 设置反射相机的朝向
        reflectionCamera.transform.LookAt(
            reflectionCamera.transform.position + forward, up);
    }

    /// <summary>
    /// 设置带“倾斜平面裁剪”的投影矩阵
    /// 防止反射平面下方内容出现在反射贴图中
    /// </summary>
    private void SetupObliqueProjMatrix(Vector3 normal)
    {
        Matrix4x4 viewMatrix = reflectionCamera.worldToCameraMatrix;

        // 将平面位置与法线从世界空间转换到相机空间
        Vector3 viewPosition = viewMatrix.MultiplyPoint(transform.position);
        Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized;

        // 定义相机空间下的裁剪平面 (Ax + By + Cz + D = 0)
        Vector4 plane = new Vector4(
            viewNormal.x,
            viewNormal.y,
            viewNormal.z,
            -Vector3.Dot(viewPosition, viewNormal)
        );

        // 设置带有此平面裁剪的投影矩阵
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(plane);
    }

    #endregion


    #region ======== 外部 API (供其他组件控制) ========

    public void IgnoreCamera(Camera camera) => ignoredCameras.Add(camera);
    public void UnignoreCamera(Camera camera) => ignoredCameras.Remove(camera);
    public void ClearIgnoredCameras() => ignoredCameras.Clear();
    public bool IsIgnoringCamera(Camera camera) => ignoredCameras.Contains(camera);

    #endregion
}
