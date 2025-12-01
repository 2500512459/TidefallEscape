using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 场景特定的岛屿列表配置
/// </summary>
[System.Serializable]
public class SceneIslandList
{
    [Tooltip("场景名称（需要与Unity场景名称完全匹配）")]
    public string sceneName;

    [Tooltip("该场景中特定岛屿在小地图上使用的图标列表（顺序与 requiredIslands 对应）")]
    public List<Sprite> islandSprites = new List<Sprite>();

    [Tooltip("该场景必定生成的岛屿预制体列表")]
    public List<GameObject> requiredIslands = new List<GameObject>();
}

/// <summary>
/// 负责在场景中心周围生成规则网格，并在网格中心随机放置岛屿预制体。
/// 假设你有大约 30x30 的岛屿预制体，本脚本使用 40x40 的逻辑网格，给岛之间留出一点间距。
/// </summary>
public class IslandManager : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("要生成多少个岛屿")]
    [Min(1)]
    public int islandCount = 20;

    [Tooltip("每个网格格子的边长（世界单位）")]
    public float cellSize = 40f;

    [Tooltip("网格每一边有多少个格子(总格子数 = gridSide * gridSide)")]
    [Min(1)]
    public int gridSide = 15;

    [Tooltip("网格的中心点")]
    public Transform center;

    [Header("岛屿预制体")]
    [Tooltip("可供随机选择的岛屿预制体")]
    public List<GameObject> islandPrefabs = new List<GameObject>();

    [Header("场景特定岛屿")]
    [Tooltip("不同场景必定生成的岛屿列表（根据场景名称匹配）")]
    public List<SceneIslandList> sceneSpecificIslands = new List<SceneIslandList>();

    [Header("生成选项")]
    [Tooltip("进入场景后是否自动生成岛屿")]
    public bool autoGenerateOnStart = true;

    [Tooltip("是否使用固定随机种子（方便调试可重复结果）")]
    public bool useFixedSeed = false;

    [Tooltip("固定随机种子值")]
    public int seed = 0;

    [Header("调试可视化")]
    [Tooltip("在 Scene 视图中绘制网格（由 4 个三角形构成的正方形）")]
    public bool drawGizmos = true;

    [Tooltip("只绘制前多少个格子的 Gizmos避免场景太乱")]
    public int gizmoMaxCells = 200;

    [Header("间距约束")]
    [Tooltip("两个岛之间在网格坐标上至少相隔多少个格子")]
    [Min(0)]
    public int minCellDistance = 0;

    [Tooltip("禁止在中心周围多少个格子内生成岛屿")]
    [Min(0)]
    public int centerBlockRadius = 0;

    /// <summary>
    /// 记录所有网格中心位置（已经按距离中心排序）
    /// </summary>
    public List<Vector3> gridCenters = new List<Vector3>();

    /// <summary>
    /// 与 gridCenters 一一对应的网格整数坐标（x,z），用于计算格子间距
    /// </summary>
    private List<Vector2Int> gridCoords = new List<Vector2Int>();

    /// <summary>
    /// 只读访问网格中心
    /// </summary>
    public IReadOnlyList<Vector3> GridCenters => gridCenters;

    /// <summary>
    /// 真实生成的岛屿世界坐标（包含场景特定岛屿和随机岛屿）
    /// </summary>
    private readonly List<Vector3> spawnedIslandPositions = new List<Vector3>();

    /// <summary>
    /// 真实生成的【场景特定岛屿】世界坐标
    /// </summary>
    private readonly List<Vector3> spawnedRequiredIslandPositions = new List<Vector3>();

    /// <summary>
    /// 真实生成的【随机岛屿】世界坐标
    /// </summary>
    private readonly List<Vector3> spawnedRandomIslandPositions = new List<Vector3>();

    /// <summary>
    /// 对外只读访问真实生成的岛屿坐标
    /// </summary>
    public IReadOnlyList<Vector3> SpawnedIslandPositions => spawnedIslandPositions;

    /// <summary>
    /// 对外只读访问真实生成的【场景特定岛屿】坐标
    /// </summary>
    public IReadOnlyList<Vector3> SpawnedRequiredIslandPositions => spawnedRequiredIslandPositions;

    /// <summary>
    /// 对外只读访问真实生成的【随机岛屿】坐标
    /// </summary>
    public IReadOnlyList<Vector3> SpawnedRandomIslandPositions => spawnedRandomIslandPositions;

    /// <summary>
    /// 当前场景中【场景特定岛屿】在小地图上使用的图标列表
    public List<Sprite> CurrentSceneRequiredIslandSprites { get; private set; } = new List<Sprite>();

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateIslands();
        }
    }

    /// <summary>
    /// 对外公开的方法：重新生成网格并随机放置岛屿。
    /// </summary>
    [ContextMenu("Regenerate Islands")]
    public void GenerateIslands()
    {
        // 清空旧数据与旧生成的岛屿
        ClearGeneratedIslands();

        GenerateGridCenters();
        SpawnIslands();
    }

    /// <summary>
    /// 删除本物体下已经生成的岛屿（所有子物体）。
    /// </summary>
    private void ClearGeneratedIslands()
    {
        // 清理记录的已生成岛屿坐标
        spawnedIslandPositions.Clear();
        spawnedRequiredIslandPositions.Clear();
        spawnedRandomIslandPositions.Clear();

        var toDestroy = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            toDestroy.Add(transform.GetChild(i).gameObject);
        }

        // 分开循环是避免在遍历 Transform 子节点时修改层级结构
        foreach (var go in toDestroy)
        {
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }

    /// <summary>
    /// 从中心向四周生成正方形网格，记录并按距离中心排序每个网格的中心位置。
    /// 每个格子大小为 cellSize x cellSize，可以想象被对角线划分成 4 个三角形。
    /// </summary>
    private void GenerateGridCenters()
    {
        gridCenters.Clear();
        gridCoords.Clear();
    
        Vector3 origin = center != null ? center.position : Vector3.zero;
    
        int side = Mathf.Max(1, gridSide);
        int half = side / 2;
    
        // 1. 先将 grid center 和 grid coord 绑定在一起
        List<(Vector3 pos, Vector2Int coord)> cells = new();
    
        for (int x = -half; x <= half; x++)
        {
            for (int z = -half; z <= half; z++)
            {
                Vector3 cellCenter = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                cells.Add((cellCenter, new Vector2Int(x, z)));
            }
        }
    
        // 2. 按距离排序（pos 和 coord 一起排序）
        cells = cells.OrderBy(c => (c.pos - origin).sqrMagnitude).ToList();
    
        // 3. 拆回 gridCenters 和 gridCoords
        foreach (var c in cells)
        {
            gridCenters.Add(c.pos);
            gridCoords.Add(c.coord);
        }
    }


    /// <summary>
    /// 在网格中心随机放置岛屿预制体。
    /// 默认随机不重复地从 islandPrefabs 中取出 islandCount 个预制体，
    /// 同时随机选择网格中心，让岛屿更加"散落"而不是全部挤在正中心。
    /// 优先生成场景特定的必定岛屿。
    /// </summary>
    private void SpawnIslands()
    {
        if (gridCenters == null || gridCenters.Count == 0)
        {
            Debug.LogWarning("[IslandManager] 还没有生成网格中心，请先调用 GenerateGridCenters。");
            return;
        }

        if (islandPrefabs == null || islandPrefabs.Count == 0)
        {
            Debug.LogWarning("[IslandManager] islandPrefabs 为空，请在 Inspector 中指定岛屿预制体。");
            return;
        }

        // 固定随机种子（可选），方便调试时得到完全一致的生成结果
        System.Random rng = useFixedSeed ? new System.Random(seed) : new System.Random();

        // 获取当前场景名称
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[IslandManager] 当前场景名称: {currentSceneName}");

        // 查找当前场景的特定岛屿列表
        List<GameObject> requiredIslandsForScene = null;
        CurrentSceneRequiredIslandSprites.Clear();
        if (sceneSpecificIslands != null && sceneSpecificIslands.Count > 0)
        {
            var sceneIslandConfig = sceneSpecificIslands.FirstOrDefault(s => s.sceneName == currentSceneName);
            if (sceneIslandConfig != null && sceneIslandConfig.requiredIslands != null && sceneIslandConfig.requiredIslands.Count > 0)
            {
                requiredIslandsForScene = sceneIslandConfig.requiredIslands.Where(go => go != null).ToList();
                // 按顺序记录当前场景特定岛屿的图标列表（长度与 requiredIslandsForScene 对齐）
                if (sceneIslandConfig.islandSprites != null && sceneIslandConfig.islandSprites.Count > 0)
                {
                    for (int i = 0; i < requiredIslandsForScene.Count; i++)
                    {
                        Sprite spriteForThisIsland;
                        if (i < sceneIslandConfig.islandSprites.Count)
                        {
                            spriteForThisIsland = sceneIslandConfig.islandSprites[i];
                        }
                        else
                        {
                            // 如果图标数量不足，则复用最后一个，避免越界
                            spriteForThisIsland = sceneIslandConfig.islandSprites[sceneIslandConfig.islandSprites.Count - 1];
                        }
                        CurrentSceneRequiredIslandSprites.Add(spriteForThisIsland);
                    }
                }
                Debug.Log($"[IslandManager] 找到场景特定岛屿列表，共 {requiredIslandsForScene.Count} 个必定生成的岛屿");
            }
        }

        // 生成格子索引列表，并同样洗牌，让岛屿分散在整个网格中
        var cellIndices = Enumerable.Range(0, gridCenters.Count).ToList();
        for (int i = cellIndices.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (cellIndices[i], cellIndices[j]) = (cellIndices[j], cellIndices[i]);
        }

        // 按中心禁区 + 最小格子间距约束，依次选择合适的格子
        var selectedCellIndices = new List<int>();
        int minDist = Mathf.Max(0, minCellDistance);
        int blockRadius = Mathf.Max(0, centerBlockRadius);

        // 先为场景特定的必定岛屿选择位置
        int requiredIslandCount = requiredIslandsForScene != null ? requiredIslandsForScene.Count : 0;
        int totalRequiredCount = islandCount + requiredIslandCount;

        foreach (int candidateIndex in cellIndices)
        {
            if (selectedCellIndices.Count >= totalRequiredCount)
                break;

            // 1）先判断是否落在中心禁区内（围绕 (0,0) 的方形区域）
            if (blockRadius > 0 && gridCoords != null && gridCoords.Count == gridCenters.Count)
            {
                Vector2Int candCoord = gridCoords[candidateIndex];
                int centerDist = Mathf.Max(Mathf.Abs(candCoord.x), Mathf.Abs(candCoord.y)); // 棋盘距离到原点
                if (centerDist <= blockRadius)
                {
                    continue;
                }
            }

            // 2）再判断与已经选中的格子之间是否满足最小间距
            if (minDist > 0 && gridCoords != null && gridCoords.Count == gridCenters.Count)
            {
                Vector2Int candCoord = gridCoords[candidateIndex];
                bool tooClose = false;

                foreach (int usedIndex in selectedCellIndices)
                {
                    Vector2Int usedCoord = gridCoords[usedIndex];
                    int dist = Mathf.Max(Mathf.Abs(candCoord.x - usedCoord.x), Mathf.Abs(candCoord.y - usedCoord.y)); // 棋盘距离
                    if (dist < minDist)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;
            }

            selectedCellIndices.Add(candidateIndex);
        }

        if (selectedCellIndices.Count < totalRequiredCount)
        {
            Debug.LogWarning($"[IslandManager] 由于最小格子间距 minCellDistance={minDist} 过大，只能在 {selectedCellIndices.Count} 个格子上生成岛屿（目标为 {totalRequiredCount}）。");
        }

        int spawnIndex = 0;

        // 优先生成场景特定的必定岛屿
        if (requiredIslandsForScene != null && requiredIslandsForScene.Count > 0)
        {
            for (int i = 0; i < requiredIslandsForScene.Count && spawnIndex < selectedCellIndices.Count; i++)
            {
                GameObject prefab = requiredIslandsForScene[i];
                if (prefab == null)
                {
                    Debug.LogWarning($"[IslandManager] 场景特定岛屿列表中的第 {i} 个预制体为空，已跳过。");
                    continue;
                }

                int cellIndex = selectedCellIndices[spawnIndex];
                Vector3 position = gridCenters[cellIndex];
                Quaternion rotation = Quaternion.identity;

                GameObject instance = Instantiate(prefab, position, rotation, transform);
                instance.name = $"{prefab.name}_Required_{i}";
                // 记录真实生成的岛屿位置
                spawnedIslandPositions.Add(position);
                spawnedRequiredIslandPositions.Add(position);
                spawnIndex++;
            }
        }

        // 生成剩余的随机岛屿
        int remainingCount = Mathf.Min(islandCount, selectedCellIndices.Count - spawnIndex);
        if (remainingCount > 0 && islandPrefabs.Count > 0)
        {
            // 生成预制体索引列表，并用 Fisher-Yates 洗牌，保证随机且不重复
            var prefabIndices = Enumerable.Range(0, islandPrefabs.Count).ToList();
            for (int i = prefabIndices.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (prefabIndices[i], prefabIndices[j]) = (prefabIndices[j], prefabIndices[i]);
            }

            for (int i = 0; i < remainingCount && spawnIndex < selectedCellIndices.Count; i++)
            {
                int prefabIndex = prefabIndices[i % prefabIndices.Count];
                GameObject prefab = islandPrefabs[prefabIndex];
                if (prefab == null)
                {
                    Debug.LogWarning($"[IslandManager] islandPrefabs[{prefabIndex}] 为空，已跳过。");
                    continue;
                }

                int cellIndex = selectedCellIndices[spawnIndex];
                Vector3 position = gridCenters[cellIndex];
                Quaternion rotation = Quaternion.identity;

                GameObject instance = Instantiate(prefab, position, rotation, transform);
                instance.name = $"{prefab.name}_Island_{i}";
                // 记录真实生成的岛屿位置
                spawnedIslandPositions.Add(position);
                spawnedRandomIslandPositions.Add(position);
                spawnIndex++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || gridCenters == null || gridCenters.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        int count = Mathf.Min(gridCenters.Count, gizmoMaxCells);
        float half = cellSize * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 centerPos = gridCenters[i];

            // 计算四个角点（Y 使用中心点的 Y）
            Vector3 c = centerPos;
            Vector3 p0 = c + new Vector3(-half, 0f, -half);
            Vector3 p1 = c + new Vector3(-half, 0f, half);
            Vector3 p2 = c + new Vector3(half, 0f, half);
            Vector3 p3 = c + new Vector3(half, 0f, -half);

            // 绘制外边框
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);

            // 绘制两条对角线，把正方形分成 4 个三角形
            Gizmos.DrawLine(p0, p2);
            Gizmos.DrawLine(p1, p3);
        }
    }
}


