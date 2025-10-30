using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// ƽ�淴��ϵͳ��Planar Reflection System��
///
/// ʵ��ԭ��
/// Ϊ����ƽ�棨��ˮ�棩����һ�������������
/// ͨ�����������㾵���������̬��
/// ��Ⱦ��������ͼ�����ݸ� Shader ����ʵʱ���䡣
///
/// ���÷�Χ��
/// - ˮ�淴��
/// - ���ӡ��ذ巴��
/// - �κλ���ƽ�淴��ĳ���
/// </summary>
//[ExecuteAlways]  // �ڱ༭����Ҳ��ʵʱִ��
public class PlanarReflections : MonoBehaviour
{
    #region ======== �ɵ����� (Inspector ����) ========

    [Header("Reflection Settings")]
    [Range(0.1f, 1.0f)] public float reflectionQuality = 0.5f;  // ������ͼ�ֱ��ʱ�����������������
    [Range(1, 4)] public int textureID = 1;                     // �󶨵� Shader ����ͼ��ţ���֧�ֶ�㷴�䣩
    public float farClipPlane = 100f;                           // �������Զ�ü���
    public LayerMask reflectionLayers = -1;                     // ����ɼ��Ĳ㣨LayerMask��
    public bool renderSkybox = true;                            // �Ƿ��ڷ�������Ⱦ��պ�

    [Header("Custom Reflection Plane")]
    public bool useCustomNormal = false;                        // �Ƿ�ʹ���Զ���ƽ�淨��
    public Vector3 customNormal = Vector3.up;                   // �Զ��巴��ƽ�淨��

    #endregion

    #region ======== �ڲ�״̬�뻺�� ========

    private Camera reflectionCamera;                            // �������
    private GameObject reflectionCameraGO;                      // ������ض��������ڲ㼶�У�
    private readonly Dictionary<Camera, RenderTexture> cameraTextures = new(); // ÿ���������Ӧ�ķ�����ͼ
    private readonly HashSet<Camera> ignoredCameras = new();    // ���Է����������ϣ�����ݹ飩

    #endregion


    #region ======== MonoBehaviour �������� ========

    private void OnEnable()
    {
        // ���ڴ�ע�� SRP �ص���
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


    #region ======== ����������� ========

    /// <summary>
    /// ��ʼ����������������״ε���ʱ������
    /// </summary>
    private void InitializeReflectionCamera()
    {
        if (reflectionCamera != null) return;

        reflectionCameraGO = new GameObject("ReflectionCamera", typeof(Camera));
        reflectionCameraGO.hideFlags = HideFlags.HideAndDontSave; // ����ʾ�ڲ㼶�����
        reflectionCamera = reflectionCameraGO.GetComponent<Camera>();
        reflectionCamera.enabled = false;                         // �ֶ�������Ⱦ
    }

    /// <summary>
    /// �ͷŷ�����ͼ�������Դ
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


    #region ======== ������Ⱦ���� ========

    /// <summary>
    /// ÿ֡���£����ɷ����������ͼ����Ⱦ������ͼ
    /// </summary>
    private void Update()
    {
        InitializeReflectionCamera();

        // ��ȡ����ƽ��ķ��߷��򣨿��Զ��壩
        var normal = GetReflectionNormal();

        // ����������������õ����������
        ConfigureReflectionCamera(Camera.main);

        // ����/���·�����ͼ
        UpdateRenderTexture(Camera.main);

        // ���·��������λ���볯�򣨾���
        UpdateReflectionCameraTransform(Camera.main, normal);

        // ���ô���б�ü���ͶӰ����ʹ�������Ϸ������ݱ��޳�
        SetupObliqueProjMatrix(normal);

        // ������׼��Ⱦ����RenderPipeline API��
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

        if (RenderPipeline.SupportsRenderRequest(reflectionCamera, request))
        {
            // ���Ŀ������Ϊ��Ӧ������ͼ
            request.destination = cameraTextures[Camera.main];

            // ִ����Ⱦ���󣨵ȼ��ڵ�����Ⱦ�������
            RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
        }

        // �����ɵķ�����ͼ�󶨵� Shader ȫ������
        var textureName = $"_PlanarReflectionsTex{textureID}";
        reflectionCamera.targetTexture.SetGlobalShaderProperty(textureName);
    }

    /// <summary>
    /// �ж��Ƿ�����ĳ������ķ�����Ⱦ
    /// ����ݹ鷴�䡢�༭��Ԥ�������
    /// </summary>
    private bool ShouldSkipCamera(Camera camera)
    {
        return camera.cameraType == CameraType.Reflection ||
               camera.cameraType == CameraType.Preview ||
               ignoredCameras.Contains(camera);
    }

    /// <summary>
    /// ��������������õ��������
    /// </summary>
    private void ConfigureReflectionCamera(Camera sourceCamera)
    {
        reflectionCamera.CopyFrom(sourceCamera);
        reflectionCamera.cameraType = CameraType.Reflection;
        reflectionCamera.usePhysicalProperties = false;
        reflectionCamera.farClipPlane = farClipPlane;
        reflectionCamera.cullingMask = reflectionLayers;

        // �Ƿ���Ⱦ��պУ�����ʹ�ô�ɫ��գ�
        reflectionCamera.clearFlags = renderSkybox
            ? sourceCamera.clearFlags
            : CameraClearFlags.SolidColor;
    }

    /// <summary>
    /// ����������ֱ������������ø��·�����ͼ
    /// </summary>
    private void UpdateRenderTexture(Camera sourceCamera)
    {
        int width = Mathf.RoundToInt(sourceCamera.pixelWidth * reflectionQuality);
        int height = Mathf.RoundToInt(sourceCamera.pixelHeight * reflectionQuality);

        // ����ǰ�����ڻ�ߴ粻ƥ�䣬�����´���
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


    #region ======== ������ѧ���㲿�� ========

    /// <summary>
    /// ��ȡ����ƽ��ķ���
    /// </summary>
    private Vector3 GetReflectionNormal()
    {
        if (!useCustomNormal) return transform.up;
        return customNormal.normalized;
    }

    /// <summary>
    /// ���·�������Ŀռ�λ������ת
    /// ͨ�����߷����������λ���뷽��
    /// </summary>
    private void UpdateReflectionCameraTransform(Camera sourceCamera, Vector3 normal)
    {
        // �������ƽ���ͶӰ����
        Vector3 proj = normal * Vector3.Dot(
            normal, sourceCamera.transform.position - transform.position);

        // �����λ���ط��߷���Գ�
        reflectionCamera.transform.position = sourceCamera.transform.position - 2 * proj;

        // ��������������ط��߷���
        Vector3 forward = Vector3.Reflect(sourceCamera.transform.forward, normal);
        Vector3 up = Vector3.Reflect(sourceCamera.transform.up, normal);

        // ���÷�������ĳ���
        reflectionCamera.transform.LookAt(
            reflectionCamera.transform.position + forward, up);
    }

    /// <summary>
    /// ���ô�����бƽ��ü�����ͶӰ����
    /// ��ֹ����ƽ���·����ݳ����ڷ�����ͼ��
    /// </summary>
    private void SetupObliqueProjMatrix(Vector3 normal)
    {
        Matrix4x4 viewMatrix = reflectionCamera.worldToCameraMatrix;

        // ��ƽ��λ���뷨�ߴ�����ռ�ת��������ռ�
        Vector3 viewPosition = viewMatrix.MultiplyPoint(transform.position);
        Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized;

        // ��������ռ��µĲü�ƽ�� (Ax + By + Cz + D = 0)
        Vector4 plane = new Vector4(
            viewNormal.x,
            viewNormal.y,
            viewNormal.z,
            -Vector3.Dot(viewPosition, viewNormal)
        );

        // ���ô��д�ƽ��ü���ͶӰ����
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(plane);
    }

    #endregion


    #region ======== �ⲿ API (�������������) ========

    public void IgnoreCamera(Camera camera) => ignoredCameras.Add(camera);
    public void UnignoreCamera(Camera camera) => ignoredCameras.Remove(camera);
    public void ClearIgnoredCameras() => ignoredCameras.Clear();
    public bool IsIgnoringCamera(Camera camera) => ignoredCameras.Contains(camera);

    #endregion
}
