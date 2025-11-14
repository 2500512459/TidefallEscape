using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板：包含装备栏、背包、仓库、战利品等功能
/// </summary>
public class InventoryUI : MonoSingleton<InventoryUI>
{
    [Header("Panel Root")]
    [SerializeField] GameObject panelRoot;

    [Header("Canvas Root")]
    public Transform canvasRoot;

    [Header("Panel节点")]
    public Transform LootGridText;
    public Transform LootGrid;
    public Transform InfoNodeRoot;
    public Transform RightPanel;

    [Header("整理按钮")]
    [SerializeField] Button backpackSortButton;
    [SerializeField] Button storageSortButton;

    [Header("库存面板")]
    [SerializeField] InventoryScrollViewPanel backpackPanel;
    [SerializeField] InventoryScrollViewPanel storagePanel;


    bool initialized;
    public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            Initialize();
            HidePanel();
        }
    }

    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        //InitSlots();

        if (backpackSortButton != null)
        {
            backpackSortButton.onClick.AddListener(OnBackpackSortClicked);
        }

        if (storageSortButton != null)
        {
            storageSortButton.onClick.AddListener(OnStorageSortClicked);
        }
    }

    public void ShowPanel()
    {
        Initialize();
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        //RefreshAll();

        // 根据场景类型显示
        var ctx = InventoryManager.Instance.currenContext;
        bool isHome = ctx == InventoryContext.Home;
        bool isLooting = ctx == InventoryContext.Looting;

        RightPanel.gameObject.SetActive(isHome);

        LootGridText.gameObject.SetActive(isLooting);
        LootGrid.gameObject.SetActive(isLooting);

        InfoNodeRoot.gameObject.SetActive(false);
    }

    public void HidePanel()
    {
        if (!IsVisible) return;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public Transform GetCanvasRoot()
    {
        return canvasRoot != null ? canvasRoot : transform;
    }

    void OnBackpackSortClicked()
    {
        SortInventory(InventoryType.Backpack, backpackPanel);
    }

    void OnStorageSortClicked()
    {
        SortInventory(InventoryType.Storage, storagePanel);
    }

    void SortInventory(InventoryType type, InventoryScrollViewPanel panel)
    {
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;

        var data = inventoryManager.GetInventory(type);
        if (data == null) return;

        data.SortItems();
        inventoryManager.OnInventoryChanged(type);
    }

    void OnDestroy()
    {
        if (backpackSortButton != null)
        {
            backpackSortButton.onClick.RemoveListener(OnBackpackSortClicked);
        }

        if (storageSortButton != null)
        {
            storageSortButton.onClick.RemoveListener(OnStorageSortClicked);
        }
    }

}

