using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapUI : MonoBehaviour
{
    [SerializeField] private GameObject MapPanel;
    // HomeScene 中显示的固定地图
    [SerializeField] private GameObject HomeMapPanel;
    // 其他场景中显示的动态地图背景
    [SerializeField] private GameObject DynamicMapPanel;

    [Header("动态地图配置")]
    [SerializeField] private Sprite islandSprite;
    [SerializeField] private Vector2 islandIconSize = new Vector2(16f, 16f);
    [SerializeField] private float referenceSquareSize = 400f; // 用作地图正方形区域的参考边长（像素）
    [SerializeField] private IslandManager islandManager;

    // 运行时生成的小岛图标，用于刷新时清理
    private readonly List<GameObject> _islandIcons = new List<GameObject>();

    private void OnEnable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.MapPressedEvent += OnMapPressed;
        }
    }

    private void OnDisable()
    {
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.MapPressedEvent -= OnMapPressed;
        }
    }

    public void ShowPanel()
    {
        if (MapPanel == null) return;

        MapPanel.SetActive(true);

        // 根据当前场景显示不同的地图
        if (IsInHomeScene())
        {
            ShowHomeMap();
        }
        else
        {
            ShowDynamicMap();
        }

        // 打开地图时，锁定除地图按键外的所有输入
        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.DisableAllInputsExcept(PlayerInput.Instance.MapInput);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HidePanel()
    {
        if (MapPanel == null) return;

        MapPanel.SetActive(false);

        if (PlayerInput.Instance != null)
        {
            PlayerInput.Instance.EnableAllInputs();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnMapPressed()
    {
        if (MapPanel == null) return;

        if (!MapPanel.activeSelf)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    /// <summary>
    /// 是否处于 HomeScene 场景
    /// </summary>
    private bool IsInHomeScene()
    {
        return SceneManager.GetActiveScene().name == "HomeScene";
    }

    /// <summary>
    /// 显示 HomeScene 中的固定地图
    /// </summary>
    private void ShowHomeMap()
    {
        if (HomeMapPanel != null)
            HomeMapPanel.SetActive(true);

        if (DynamicMapPanel != null)
            DynamicMapPanel.SetActive(false);
    }

    /// <summary>
    /// 显示其他场景中的动态地图背景（后续在这里真正生成动态地图）
    /// </summary>
    private void ShowDynamicMap()
    {
        if (HomeMapPanel != null)
            HomeMapPanel.SetActive(false);

        if (DynamicMapPanel != null)
            DynamicMapPanel.SetActive(true);

        // 预留接口：以后在这里生成 / 刷新动态地图内容
        RefreshDynamicMap();
    }

    /// <summary>
    /// 动态地图刷新接口：根据 IslandManager 生成的岛屿位置，在 DynamicMapPanel 上生成小岛图标
    /// </summary>
    public void RefreshDynamicMap()
    {
        if (DynamicMapPanel == null)
        {
            Debug.LogWarning("[MapUI] DynamicMapPanel 未设置，无法刷新动态地图。");
            return;
        }

        if (islandManager == null)
        {
            islandManager = FindObjectOfType<IslandManager>();
            if (islandManager == null)
            {
                Debug.LogWarning("[MapUI] 未找到 IslandManager，无法获取岛屿位置。");
                return;
            }
        }

        // 获取两类岛屿的位置列表
        var requiredPositions = islandManager.SpawnedRequiredIslandPositions;
        var randomPositions = islandManager.SpawnedRandomIslandPositions;

        bool hasRequired = requiredPositions != null && requiredPositions.Count > 0;
        bool hasRandom = randomPositions != null && randomPositions.Count > 0;

        if (!hasRequired && !hasRandom)
        {
            // 没有真实生成的岛屿，直接清空图标即可
            ClearIslandIcons();
            return;
        }

        var panelRect = DynamicMapPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            Debug.LogWarning("[MapUI] DynamicMapPanel 上缺少 RectTransform，无法进行 UI 映射。");
            return;
        }

        // 先清理旧的图标
        ClearIslandIcons();

        // 计算真实岛屿在世界坐标中的边界，用于归一化到面板大小（包含特定岛屿和随机岛屿）
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        if (hasRandom)
        {
            for (int i = 0; i < randomPositions.Count; i++)
            {
                Vector3 p = randomPositions[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }
        }

        if (hasRequired)
        {
            for (int i = 0; i < requiredPositions.Count; i++)
            {
                Vector3 p = requiredPositions[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }
        }

        // 防止所有点在同一条线上导致除以 0
        if (Mathf.Approximately(maxX, minX)) { maxX = minX + 1f; }
        if (Mathf.Approximately(maxZ, minZ)) { maxZ = minZ + 1f; }

        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;

        // 使用可配置的参考边长；如果未设置或 <=0，则退回到屏幕高度一半
        float referenceSize = referenceSquareSize > 0f ? referenceSquareSize : Screen.height * 0.5f;
        float squareSize = Mathf.Min(referenceSize, panelWidth, panelHeight);

        int iconIndex = 0;

        // 先绘制随机岛屿：使用 MapUI 的 islandSprite
        if (hasRandom)
        {
            if (islandSprite == null)
            {
                Debug.LogWarning("[MapUI] islandSprite 未设置，无法在地图上绘制随机岛屿图标。");
            }

            for (int i = 0; i < randomPositions.Count; i++)
            {
                Vector3 worldPos = randomPositions[i];

                // 归一化到 [0,1]
                float nx = (worldPos.x - minX) / (maxX - minX);
                float nz = (worldPos.z - minZ) / (maxZ - minZ);

                // 转换到 [-0.5, 0.5]，再乘以正方形边长，得到 anchoredPosition
                float localX = (nx - 0.5f) * squareSize;
                float localY = (nz - 0.5f) * squareSize;

                GameObject iconGO = new GameObject($"IslandIcon_Random_{iconIndex++}", typeof(RectTransform));
                var rt = iconGO.GetComponent<RectTransform>();
                rt.SetParent(panelRect, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(localX, localY);
                rt.sizeDelta = islandIconSize;

                var img = iconGO.AddComponent<Image>();
                img.sprite = islandSprite;
                img.raycastTarget = false;

                _islandIcons.Add(iconGO);
            }
        }

        // 再绘制场景特定岛屿：使用 SceneIslandList 中配置的 islandSprites（按顺序对应）
        if (hasRequired)
        {
            var requiredSprites = islandManager.CurrentSceneRequiredIslandSprites;

            for (int i = 0; i < requiredPositions.Count; i++)
            {
                Vector3 worldPos = requiredPositions[i];

                // 归一化到 [0,1]
                float nx = (worldPos.x - minX) / (maxX - minX);
                float nz = (worldPos.z - minZ) / (maxZ - minZ);

                // 转换到 [-0.5, 0.5]，再乘以正方形边长，得到 anchoredPosition
                float localX = (nx - 0.5f) * squareSize;
                float localY = (nz - 0.5f) * squareSize;

                GameObject iconGO = new GameObject($"IslandIcon_Required_{iconIndex++}", typeof(RectTransform));
                var rt = iconGO.GetComponent<RectTransform>();
                rt.SetParent(panelRect, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(localX, localY);
                rt.sizeDelta = islandIconSize;

                var img = iconGO.AddComponent<Image>();
                Sprite spriteToUse = null;
                if (requiredSprites != null && i < requiredSprites.Count)
                {
                    spriteToUse = requiredSprites[i];
                }

                // 如果未配置或越界，则退回使用随机岛屿的图标，以保证至少能显示
                if (spriteToUse == null)
                {
                    spriteToUse = islandSprite;
                }

                img.sprite = spriteToUse;
                img.raycastTarget = false;

                _islandIcons.Add(iconGO);
            }
        }
    }

    /// <summary>
    /// 清理当前动态地图上的所有岛屿图标
    /// </summary>
    private void ClearIslandIcons()
    {
        for (int i = 0; i < _islandIcons.Count; i++)
        {
            if (_islandIcons[i] != null)
            {
                Destroy(_islandIcons[i]);
            }
        }
        _islandIcons.Clear();
    }
}

