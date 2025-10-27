using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 海洋几何模块（Clipmap 网格系统）
/// 通过多级 LOD 圆环（Ring）与过渡区（Trim）动态拼接出无限水面。
/// 本模块主要负责：
///  - 几何网格的初始化与更新
///  - 材质属性更新（反射、泡沫、SSS 等）
///  - 根据观察者（viewer）位置动态移动网格，保证视觉上无缝延展
/// </summary>
public partial class Water : MonoBehaviour
{
    [SerializeField] Transform viewer;                // 观察者（通常为主相机）
    [SerializeField] Material waterMaterial;          // 基础水材质
    [SerializeField] bool updateMaterialProperties;   // 是否每帧同步材质属性（调试用）

    [SerializeField] float lengthScale = 100;         // 每级 LOD 的基础尺寸
    [SerializeField, Range(1, 40)] int vertexDensity = 30; // 单级网格的密度（越大越精细）
    [SerializeField, Range(0, 8)] int clipLevels = 8; // Clipmap 层级数（LOD 级别数）
    [SerializeField, Range(0, 100)] float skirtSize = 50;  // 最外层遮罩边界（防止远处空隙）

    // --- 次表面散射（SSS）参数 ---
    [SerializeField, Range(0, 1)] float sssStrength = 1;        // SSS 强度
    [SerializeField, Range(0.01f, 20.0f)] float sssScale = 0.35f; // SSS 散射范围（控制柔和度）
    [SerializeField, Range(0, 2.0f)] float sssBase = 2f;         // SSS 基础亮度

    // --- 水下可见度（深度衰减）---
    [SerializeField, Range(1, 500)] float visibility = 10;       // 最大可见深度（用于透射/折射计算）

    // --- 泡沫 ---
    [SerializeField, Range(0, 500)] float foamStrength = 1;      // 泡沫强度（整体）
    [SerializeField, Range(0, 10)] float contactFoamStrength = 1; // 接触泡沫强度（物体与水面交互处）

    // --- 反射 ---
    [SerializeField, Range(0, 1)] float reflectionStrength = 1f; // 水面反射强度

    // --- 距离淡化 ---
    [SerializeField, Range(2000, 10000)] float normalFadeFar = 3000; // 法线淡出距离
    [SerializeField, Range(2000, 10000)] float foamFadeFar = 6000;   // 泡沫淡出距离

    [SerializeField, Range(0.01f, 5)] float geomteryScale = 1;       // 整体几何缩放系数

    // 几何缓存
    List<Element> rings = new List<Element>(); // 各级圆环区域（LOD 主体）
    List<Element> trims = new List<Element>(); // 圆环之间的过渡区域（Trim）
    Element center;                            // 中心网格
    Element skirt;                             // 最外围遮罩网格（防止LOD空隙）
    Quaternion[] trimRotations;                // Trim 模块的旋转状态表
    int previousVertexDensity;                 // 上一帧顶点密度（用于检测变化）
    float previousSkirtSize;                   // 上一帧裙边尺寸（检测是否需重建）

    Material[] materials;                      // 按远近分的LOD材质（近、中、远）
    public Transform geoRoot { get; private set; } // 所有几何对象的根节点

    // ------------------------------------------------------
    // Viewer 相关接口
    // ------------------------------------------------------

    /// <summary>
    /// 设置观察者（通常是主相机）
    /// </summary>
    public void SetViewer(Transform t)
    {
        if (t)
            viewer = t;
    }

    public Transform GetViewer() => viewer;

    // ------------------------------------------------------
    // 初始化与更新几何
    // ------------------------------------------------------

    /// <summary>
    /// 初始化Clipmap几何结构与材质
    /// </summary>
    private void InitGeometry()
    {
        if (viewer == null)
            viewer = Camera.main.transform;

        UpdateVariables(); // 更新Shader全局参数

        // 初始化三个LOD级别材质（近、中、远）
        materials = new Material[3];
        materials[0] = new Material(waterMaterial);
        materials[0].EnableKeyword("CLOSE"); // 近距离材质

        materials[1] = new Material(waterMaterial);
        materials[1].EnableKeyword("MID");   // 中距离
        materials[1].DisableKeyword("CLOSE");

        materials[2] = new Material(waterMaterial);
        materials[2].DisableKeyword("MID");  // 远距离
        materials[2].DisableKeyword("CLOSE");

        // Trim 拼接部分的四种旋转状态（用于拼环）
        trimRotations = new Quaternion[]
        {
            Quaternion.AngleAxis(180, Vector3.up),
            Quaternion.AngleAxis(90, Vector3.up),
            Quaternion.AngleAxis(270, Vector3.up),
            Quaternion.identity,
        };

        InstantiateMeshes(); // 构建网格结构
    }

    /// <summary>
    /// 每帧更新Clipmap几何与材质
    /// 检查参数变化、移动viewer位置时调整网格布局
    /// </summary>
    private void UpdateGeometry()
    {
        // 若LOD层级、密度或裙边尺寸改变，则重建网格
        if (rings.Count != clipLevels || trims.Count != clipLevels
            || previousVertexDensity != vertexDensity || !Mathf.Approximately(previousSkirtSize, skirtSize))
        {
            InstantiateMeshes();
            previousVertexDensity = vertexDensity;
            previousSkirtSize = skirtSize;
        }

        UpdatePositions(); // 按viewer位置移动各级环
        UpdateMaterials(); // 更新材质属性与LOD选择
    }

    // ------------------------------------------------------
    // 材质更新逻辑
    // ------------------------------------------------------

    /// <summary>
    /// 更新各级材质（同步Shader属性、设置LOD材质）
    /// </summary>
    void UpdateMaterials()
    {
        if (updateMaterialProperties)
        {
            // 同步材质属性（便于调试实时修改）
            for (int i = 0; i < 3; i++)
                materials[i].CopyPropertiesFromMaterial(waterMaterial);

            materials[0].EnableKeyword("CLOSE");
            materials[1].EnableKeyword("MID");
            materials[1].DisableKeyword("CLOSE");
            materials[2].DisableKeyword("MID");
            materials[2].DisableKeyword("CLOSE");
        }

        // 根据LOD层数计算当前激活层级数量
        int activeLevels = ActiveLodlevels();

        // 中心网格材质
        center.MeshRenderer.material = GetMaterial(clipLevels - activeLevels - 1);

        // 为每级Ring与Trim分配对应LOD材质
        for (int i = 0; i < rings.Count; i++)
        {
            rings[i].MeshRenderer.material = GetMaterial(clipLevels - activeLevels + i);
            trims[i].MeshRenderer.material = GetMaterial(clipLevels - activeLevels + i);
        }

        UpdateVariables();
    }

    /// <summary>
    /// 更新Shader全局变量
    /// （反射、泡沫、SSS、深度衰减等）
    /// </summary>
    void UpdateVariables()
    {
        Shader.SetGlobalFloat("_GeometryScale", geomteryScale);

        Shader.SetGlobalFloat("_SSSStrength", sssStrength);
        Shader.SetGlobalFloat("_SSSScale", sssScale);
        Shader.SetGlobalFloat("_SSSBase", sssBase);

        Shader.SetGlobalFloat("_MaxDepth", visibility);

        // Foam随时间变化的因子（控制泡沫随波动强度变化）
        float timeFactor = Mathf.Clamp01((waterTime - 1.0f) / 3.0f);

        Shader.SetGlobalFloat("_FoamScale", foamStrength * timeFactor);
        Shader.SetGlobalFloat("_ContactFoam", contactFoamStrength);

        Shader.SetGlobalFloat("_ReflectionStrength", reflectionStrength);

        Shader.SetGlobalFloat("_NormalFadeFar", normalFadeFar);
        Shader.SetGlobalFloat("_FoamFadeFar", foamFadeFar);
    }

    /// <summary>
    /// 根据LOD层级选择合适的材质（近/中/远）
    /// </summary>
    Material GetMaterial(int lodLevel)
    {
        if (lodLevel - 2 <= 0) return materials[0];   // 近
        if (lodLevel - 2 <= 2) return materials[1];   // 中
        return materials[2];                          // 远
    }

    // ------------------------------------------------------
    // 几何布局更新
    // ------------------------------------------------------

    /// <summary>
    /// 根据viewer位置动态更新每级Ring与Trim的位置与缩放
    /// 确保视野中心始终无缝衔接
    /// </summary>
    void UpdatePositions()
    {
        int k = GridSize();
        int activeLevels = ActiveLodlevels();

        float scale = ClipLevelScale(-1, activeLevels);
        Vector3 previousSnappedPosition = Snap(viewer.position, scale * 2);

        // 更新中心块位置
        center.Transform.position = previousSnappedPosition + OffsetFromCenter(-1, activeLevels);
        center.Transform.localScale = new Vector3(scale, 1, scale);

        // 遍历每一级Ring与Trim
        for (int i = 0; i < clipLevels; i++)
        {
            bool active = i < activeLevels;
            rings[i].Transform.gameObject.SetActive(active);
            trims[i].Transform.gameObject.SetActive(active);
            if (!active) continue;

            scale = ClipLevelScale(i, activeLevels);
            Vector3 centerOffset = OffsetFromCenter(i, activeLevels);
            Vector3 snappedPosition = Snap(viewer.position, scale * 2);

            // 计算Trim位置（边界补丁）
            Vector3 trimPosition = centerOffset + snappedPosition + scale * (k - 1) / 2 * new Vector3(1, 0, 1);
            int shiftX = previousSnappedPosition.x - snappedPosition.x < float.Epsilon ? 1 : 0;
            int shiftZ = previousSnappedPosition.z - snappedPosition.z < float.Epsilon ? 1 : 0;
            trimPosition += shiftX * (k + 1) * scale * Vector3.right;
            trimPosition += shiftZ * (k + 1) * scale * Vector3.forward;

            trims[i].Transform.position = trimPosition;
            trims[i].Transform.rotation = trimRotations[shiftX + 2 * shiftZ];
            trims[i].Transform.localScale = new Vector3(scale, 1, scale);

            rings[i].Transform.position = snappedPosition + centerOffset;
            rings[i].Transform.localScale = new Vector3(scale, 1, scale);
            previousSnappedPosition = snappedPosition;
        }

        // 最外层Skirt防止可见空洞
        scale = lengthScale * 2 * Mathf.Pow(2, clipLevels);
        skirt.Transform.position = new Vector3(-1, 0, -1) * scale * (skirtSize + 0.5f - 0.5f / GridSize()) + previousSnappedPosition;
        skirt.Transform.localScale = new Vector3(scale, 1, scale);
    }

    // ------------------------------------------------------
    // 数学与工具函数
    // ------------------------------------------------------

    /// <summary>
    /// 根据摄像机高度与ClipLevel动态计算激活的LOD层数
    /// </summary>
    int ActiveLodlevels()
    {
        return clipLevels - Mathf.Clamp((int)Mathf.Log((1.7f * Mathf.Abs(viewer.position.y) + 1) / lengthScale, 2), 0, clipLevels);
    }

    /// <summary>
    /// 获取指定LOD层级的缩放比例
    /// </summary>
    float ClipLevelScale(int level, int activeLevels)
    {
        return lengthScale / GridSize() * Mathf.Pow(2, clipLevels - activeLevels + level + 1);
    }

    /// <summary>
    /// 根据层级计算该层网格的世界偏移位置（用于环拼接）
    /// </summary>
    Vector3 OffsetFromCenter(int level, int activeLevels)
    {
        return (Mathf.Pow(2, clipLevels) + GeometricProgressionSum(2, 2, clipLevels - activeLevels + level + 1, clipLevels - 1))
               * lengthScale / GridSize() * (GridSize() - 1) / 2 * new Vector3(-1, 0, -1);
    }

    /// <summary>
    /// 等比数列求和（用于计算偏移累计）
    /// </summary>
    float GeometricProgressionSum(float b0, float q, int n1, int n2)
    {
        return b0 / (1 - q) * (Mathf.Pow(q, n2) - Mathf.Pow(q, n1));
    }

    /// <summary>
    /// Clipmap网格基础边长
    /// </summary>
    int GridSize() => 4 * vertexDensity + 1;

    /// <summary>
    /// 将viewer位置对齐到指定步进（防止抖动）
    /// </summary>
    Vector3 Snap(Vector3 coords, float scale)
    {
        if (coords.x >= 0)
            coords.x = Mathf.Floor(coords.x / scale) * scale;
        else
            coords.x = Mathf.Ceil((coords.x - scale + 1) / scale) * scale;

        if (coords.z < 0)
            coords.z = Mathf.Floor(coords.z / scale) * scale;
        else
            coords.z = Mathf.Ceil((coords.z - scale + 1) / scale) * scale;

        coords.y = 0;
        return coords;
    }

    // ------------------------------------------------------
    // 几何对象创建与销毁
    // ------------------------------------------------------

    /// <summary>
    /// 安全销毁GameObject（兼容Editor模式与运行时）
    /// </summary>
    void DestroyGO(Transform go)
    {
        go.parent = null;

#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying)
            Object.Destroy(go.gameObject);
        else
            Object.DestroyImmediate(go.gameObject);
#else
        Object.Destroy(go.gameObject);
#endif
    }

    /// <summary>
    /// 清理已有几何结构
    /// </summary>
    void CleanMeshes()
    {
        geoRoot = transform.Find("Geometry Root");
        if (geoRoot == null) return;

        foreach (var child in geoRoot.GetComponentsInChildren<Transform>())
        {
            if (child != geoRoot)
                DestroyGO(child);
        }

        DestroyGO(geoRoot);
    }

    /// <summary>
    /// 重新实例化所有Clipmap几何结构
    /// 包括Center、Rings、Trims、Skirt
    /// </summary>
    void InstantiateMeshes()
    {
        CleanMeshes();
        rings.Clear();
        trims.Clear();

        GameObject root = new GameObject("Geometry Root");
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.parent = transform;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        geoRoot = root.transform;

        int k = GridSize();
        center = InstantiateElement("Center", CreatePlaneMesh(2 * k, 2 * k, 1, Seams.All), materials[materials.Length - 1]);

        Mesh ring = CreateRingMesh(k, 1);
        Mesh trim = CreateTrimMesh(k, 1);
        for (int i = 0; i < clipLevels; i++)
        {
            rings.Add(InstantiateElement($"Ring {i}", ring, materials[materials.Length - 1]));
            trims.Add(InstantiateElement($"Trim {i}", trim, materials[materials.Length - 1]));
        }

        skirt = InstantiateElement("Skirt", CreateSkirtMesh(k, skirtSize), materials[materials.Length - 1]);
    }

    /// <summary>
    /// 实例化单个几何单元（带MeshRenderer）
    /// </summary>
    Element InstantiateElement(string name, Mesh mesh, Material mat)
    {
        GameObject go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.layer = gameObject.layer; // 保持相同渲染层（便于反射剔除）
        go.transform.SetParent(geoRoot);
        go.transform.localPosition = Vector3.zero;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        mr.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
        mr.material = mat;
        mr.allowOcclusionWhenDynamic = false;

        return new Element(go.transform, mr);
    }
    /// <summary>
    /// 创建最外层“裙边”网格，用于视觉封闭边界（Clipmap 最外圈的填充块）
    /// 这个方法用 CombineInstance 将若干小平面拼接成一个大的“角-边-角-边-角...”矩形裙边。
    /// outerBorderScale 表示外边界相对于单元格的放大倍数（通常很大）。
    /// </summary>
    Mesh CreateSkirtMesh(int k, float outerBorderScale)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap skirt";

        // 将要合并的子网格数量：4 个角 + 4 条边 = 8
        CombineInstance[] combine = new CombineInstance[8];

        // 基础子网格：1x1 的四边形（角），以及 k x 1 水平条、1 x k 垂直条
        Mesh quad = CreatePlaneMesh(1, 1, 1);
        Mesh hStrip = CreatePlaneMesh(k, 1, 1);
        Mesh vStrip = CreatePlaneMesh(1, k, 1);

        // 缩放矩阵：角是 outerBorderScale x outerBorderScale
        // 中间条：为了拼接使得长度方向被拉伸，另一方向按 1/k 缩放以匹配顶点数
        Vector3 cornerQuadScale = new Vector3(outerBorderScale, 1, outerBorderScale);
        Vector3 midQuadScaleVert = new Vector3(1f / k, 1, outerBorderScale); // 水平拉伸条垂直方向缩放
        Vector3 midQuadScaleHor = new Vector3(outerBorderScale, 1, 1f / k);   // 垂直拉伸条水平方向缩放

        // 四角与四边按顺时针或逆时针放置（注意坐标和平移量）
        combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, cornerQuadScale);
        combine[0].mesh = quad;

        combine[1].transform = Matrix4x4.TRS(Vector3.right * outerBorderScale, Quaternion.identity, midQuadScaleVert);
        combine[1].mesh = hStrip;

        combine[2].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);
        combine[2].mesh = quad;

        combine[3].transform = Matrix4x4.TRS(Vector3.forward * outerBorderScale, Quaternion.identity, midQuadScaleHor);
        combine[3].mesh = vStrip;

        combine[4].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1)
            + Vector3.forward * outerBorderScale, Quaternion.identity, midQuadScaleHor);
        combine[4].mesh = vStrip;

        combine[5].transform = Matrix4x4.TRS(Vector3.forward * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);
        combine[5].mesh = quad;

        combine[6].transform = Matrix4x4.TRS(Vector3.right * outerBorderScale
            + Vector3.forward * (outerBorderScale + 1), Quaternion.identity, midQuadScaleVert);
        combine[6].mesh = hStrip;

        combine[7].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1)
            + Vector3.forward * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);
        combine[7].mesh = quad;

        // 将上面的 8 个子网格合并为一个大网格（合并顶点）
        mesh.CombineMeshes(combine, true);
        return mesh;
    }

    /// <summary>
    /// 创建Trim连接条（用于两个LOD间过渡）
    /// Trim由两个长条网格拼成：一条水平（k+1 x 1）和一条垂直（1 x k）
    /// 通过平移组合成一个角落状的过渡块（便于四个方向旋转拼接）。
    /// </summary>
    Mesh CreateTrimMesh(int k, float lengthScale)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap trim";

        CombineInstance[] combine = new CombineInstance[2];

        // 注意：CreatePlaneMesh 的参数含义 (width, height, lengthScale, seams, trianglesShift)
        // 这里第一块是横向条 (k+1,1)，放到 (-k-1, 0, -1) * lengthScale 处形成左上角的一部分
        combine[0].mesh = CreatePlaneMesh(k + 1, 1, lengthScale, Seams.None, 1);
        combine[0].transform = Matrix4x4.TRS(new Vector3(-k - 1, 0, -1) * lengthScale, Quaternion.identity, Vector3.one);

        // 第二块是纵向条 (1,k)，放到 (-1, 0, -k-1) * lengthScale 处
        combine[1].mesh = CreatePlaneMesh(1, k, lengthScale, Seams.None, 1);
        combine[1].transform = Matrix4x4.TRS(new Vector3(-1, 0, -k - 1) * lengthScale, Quaternion.identity, Vector3.one);

        mesh.CombineMeshes(combine, true);
        return mesh;
    }

    /// <summary>
    /// 创建环形Clipmap层主体网格（将四个象限的平面拼成环形）
    /// 通过组合不同的 PlaneMesh 片段构成一个 “环”（中间留空）。
    /// </summary>
    Mesh CreateRingMesh(int k, float lengthScale)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap ring";

        // 当顶点数量非常大时需要使用 32 位索引
        if ((2 * k + 1) * (2 * k + 1) >= 256 * 256)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CombineInstance[] combine = new CombineInstance[4];

        // 四个方向的片段：下半（Bottom）与左右边界，顶半（Top）与左右边界，中左、中右
        // seams 标识用于在边界处将顶点做对齐处理（避免缝隙）
        combine[0].mesh = CreatePlaneMesh(2 * k, (k - 1) / 2, lengthScale, Seams.Bottom | Seams.Right | Seams.Left);
        combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        combine[1].mesh = CreatePlaneMesh(2 * k, (k - 1) / 2, lengthScale, Seams.Top | Seams.Right | Seams.Left);
        combine[1].transform = Matrix4x4.TRS(new Vector3(0, 0, k + 1 + (k - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

        combine[2].mesh = CreatePlaneMesh((k - 1) / 2, k + 1, lengthScale, Seams.Left);
        combine[2].transform = Matrix4x4.TRS(new Vector3(0, 0, (k - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

        combine[3].mesh = CreatePlaneMesh((k - 1) / 2, k + 1, lengthScale, Seams.Right);
        combine[3].transform = Matrix4x4.TRS(new Vector3(k + 1 + (k - 1) / 2, 0, (k - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

        mesh.CombineMeshes(combine, true);
        return mesh;
    }

    /// <summary>
    /// 创建基础平面网格（格点化）
    /// width,height 表示格子数量（格子数，最终顶点数为 (width+1)*(height+1)）
    /// lengthScale 是每个格子的实际世界大小。
    /// seams 用于控制边缘顶点如何对齐（避免与相邻patch接缝错位）。
    /// trianglesShift 可以用于切换三角形分割方向，减少网格“条纹”或视觉伪影。
    /// </summary>
    Mesh CreatePlaneMesh(int width, int height, float lengthScale, Seams seams = Seams.None, int trianglesShift = 0)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Clipmap plane";

        // 如果顶点过多，必须使用 32 位索引（Unity 默认16位）
        if ((width + 1) * (height + 1) >= 256 * 256)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[(width + 1) * (height + 1)];
        int[] triangles = new int[width * height * 2 * 3];
        Vector3[] normals = new Vector3[(width + 1) * (height + 1)];

        // ---------- 顶点与法线生成 ----------
        // i 对应 Z 方向行，j 对应 X 方向列
        for (int i = 0; i < height + 1; i++)
        {
            for (int j = 0; j < width + 1; j++)
            {
                int x = j;
                int z = i;

                // seams 处理说明：
                // 当某一边需要“接缝对齐”时，这里使用 x = x / 2 * 2 的方式将该边的顶点索引向下取偶数（即重复某些顶点）
                // 目的：在拼接两张网格时使边沿顶点在位置上能够对齐（避免顶点索引不一致造成的缝隙）
                // 这里的写法会导致边上多个顶点具有相同坐标（重复顶点），以保证不同片段在边缘处的网格拓扑一致。
                if ((i == 0 && seams.HasFlag(Seams.Bottom)) || (i == height && seams.HasFlag(Seams.Top)))
                    x = x / 2 * 2;
                if ((j == 0 && seams.HasFlag(Seams.Left)) || (j == width && seams.HasFlag(Seams.Right)))
                    z = z / 2 * 2;

                // 生成顶点（注意：这里顶点坐标是以格子索引乘以 lengthScale）
                vertices[j + i * (width + 1)] = new Vector3(x, 0, z) * lengthScale;
                normals[j + i * (width + 1)] = Vector3.up; // 初始法线向上，后续可根据需求计算真实法线
            }
        }

        // ---------- 三角形索引生成 ----------
        int tris = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int k = j + i * (width + 1);

                // 使用交错的三角形分裂（棋盘式），通过 trianglesShift 可以改变分裂相位以减少连续patch出现视觉格状纹理
                if ((i + j + trianglesShift) % 2 == 0)
                {
                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + width + 2;

                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 2;
                    triangles[tris++] = k + 1;
                }
                else
                {
                    triangles[tris++] = k;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + 1;

                    triangles[tris++] = k + 1;
                    triangles[tris++] = k + width + 1;
                    triangles[tris++] = k + width + 2;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals; // 这里直接赋默认法线（上），如果需要更精确法线可调用 RecalculateNormals()
        return mesh;
    }

    // 包装类：单个几何元素（含 Transform 与 MeshRenderer）
    class Element
    {
        public Transform Transform;
        public MeshRenderer MeshRenderer;

        public Element(Transform transform, MeshRenderer meshRenderer)
        {
            Transform = transform;
            MeshRenderer = meshRenderer;
        }
    }

    // 接缝标志：用于指示平面某条边是否需要按特定方式对齐（避免拼接缝）
    [System.Flags]
    enum Seams
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        All = Left | Right | Top | Bottom
    };
}



