using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ShopScrollViewPanel : ScrollViewPanel
{
    public ShopType shopType;   // 当前显示的商店类型(武器店、药品店等)
    public InventoryType showType;      // 当前显示的库存类型(背包、仓库、商店)

    public int curSelectIndex = -1;   // 当前选中的索引

    [Header("当前选中的节点信息数据")]
    public GameObject InfoNode;  // 选中物品面板
    public Image selectItemIcon;    // 选中图标
    public TextMeshProUGUI selectItemName;  // 选中物品名称
    public TextMeshProUGUI selectItemNum;   // 选中物品数量
    public TextMeshProUGUI selectItemPrice; // 选中物品价格
    public TextMeshProUGUI selectItemDescription; // 选中物品描述

    [Header("购买/出售按钮")]
    public Button BuyOrSellBtn;
    public TextMeshProUGUI BuyOrSellText;

    [Header("交易配置")]
    [SerializeField] private CurrencyDataSO currencyData;
    [SerializeField] private InventoryType buyReceiveInventory = InventoryType.Backpack;
    // 库存数据
    private InventoryDataSO BackpackData;   // 背包数据
    private InventoryDataSO StorageData;    // 仓库数据
    private InventoryDataSO ShopData;       // 商店数据

    private InventoryDataSO currentData;    // 当前展示的数据源
    private bool hasInitializedView = false;
    private bool isScrollViewInitialized = false;
    private float buyPriceMultiplier = 1f;
    private float sellPriceMultiplier = 1f;

    private void Awake()
    {
        if (currencyData == null)
        {
            var gamePanel = global::GamePanel.Instance;
            if (gamePanel != null)
            {
                currencyData = gamePanel.CurrencyData;
            }
        }
        if (currencyData != null)
        {
            currencyData.EnsureLoaded();
        }
        else
        {
            Debug.LogWarning("[ShopScrollViewPanel] 未配置 CurrencyDataSO，交易将无法结算。", this);
        }

        if (BuyOrSellBtn != null)
        {
            BuyOrSellBtn.onClick.AddListener(OnBuyOrSellButtonClicked);
        }
        else
        {
            Debug.LogWarning("[ShopScrollViewPanel] 未绑定 BuyOrSellBtn。", this);
        }

        var inv = InventoryManager.Instance;
        BackpackData = inv.GetInventory(InventoryType.Backpack);
        StorageData = inv.GetInventory(InventoryType.Storage);
        BackpackData.EnsureSlotCount(BackpackData.maxCount);    // 确保数据数量足够
        StorageData.EnsureSlotCount(StorageData.maxCount);
        RefreshShopInventory();
    }

    private void OnDestroy()
    {
        if (BuyOrSellBtn != null)
        {
            BuyOrSellBtn.onClick.RemoveListener(OnBuyOrSellButtonClicked);
        }
    }
    protected override void Start()
    {
        base.Start();

        hasInitializedView = true;
        // 默认显示商店全部物品
        showType = InventoryType.Shop;
        currentData = ShopData;

        InitScrollView(currentData);
        UpdateBuyOrSellText();
    }
    private void RefreshShopInventory()
    {
        // 兼容旧接口：从ShopManager获取库存数据
        var shopMgr = ShopManager.Instance;
        if (shopMgr == null)
        {
            Debug.LogError("ShopManager 实例不存在，无法刷新商店数据");
            ShopData = null;
            return;
        }

        ShopData = shopMgr.GetInventory(shopType);
        if (ShopData == null)
        {
            Debug.LogError($"未配置 {shopType} 的商店库存数据");
            return;
        }

        ShopData.EnsureSlotCount(ShopData.maxCount);

        if (showType == InventoryType.Shop || currentData == null)
        {
            currentData = ShopData;
        }
    }

    private void ApplyShopContext(ShopType newType, float buyMultiplier, float sellMultiplier)
    {
        shopType = newType;
        buyPriceMultiplier = Mathf.Max(0f, buyMultiplier);
        sellPriceMultiplier = Mathf.Max(0f, sellMultiplier);

        RefreshShopInventory();

        if (!hasInitializedView || ShopData == null)
        {
            return;
        }

        OnShopButtonClick();
        if (showType == InventoryType.Shop)
        {
            ForceRefreshCurrentInventoryView();
        }
    }

    /// <summary>
    /// 设置商店库存数据（直接使用传入的库存数据，不从ShopManager获取）
    /// </summary>
    public void SetShopInventory(InventoryDataSO inventoryData, ShopType newType, float buyMultiplier, float sellMultiplier)
    {
        shopType = newType;
        buyPriceMultiplier = Mathf.Max(0f, buyMultiplier);
        sellPriceMultiplier = Mathf.Max(0f, sellMultiplier);

        if (inventoryData == null)
        {
            Debug.LogError($"[ShopScrollViewPanel] 传入的库存数据为空");
            ShopData = null;
            return;
        }

        ShopData = inventoryData;
        ShopData.EnsureSlotCount(ShopData.maxCount);

        if (showType == InventoryType.Shop || currentData == null)
        {
            currentData = ShopData;
        }

        if (!hasInitializedView || ShopData == null)
        {
            return;
        }

        OnShopButtonClick();
        if (showType == InventoryType.Shop)
        {
            ForceRefreshCurrentInventoryView();
        }
    }

    public void SetShopType(ShopType newType)
    {
        ApplyShopContext(newType, buyPriceMultiplier, sellPriceMultiplier);
    }

    public void SetShopType(ShopType newType, float buyMultiplier, float sellMultiplier)
    {
        ApplyShopContext(newType, buyMultiplier, sellMultiplier);
    }
    
    /// <summary>
    /// 初始化或刷新滚动视图
    /// </summary>
    private void InitScrollView(InventoryDataSO data)
    {
        if (data == null || loopScrollView == null) return;

        if (!isScrollViewInitialized)
        {
            loopScrollView.InitXScrollView(data.maxCount);
            loopScrollView.AddUpdateCellAction(OnUpdateScrollItemAction);
            loopScrollView.AddCellClickAction(OnClickScrollItemAction);
            isScrollViewInitialized = true;
        }
        else
        {
            loopScrollView.RefreshData(data.maxCount);
            loopScrollView.ForceRefreshVisibleItems();
        }
    }
    /// <summary>
    /// 更新item信息
    /// </summary>
    protected override void OnUpdateScrollItemAction(StorageItem item, int index)
    {
        base.OnUpdateScrollItemAction(item, index);
        if (currentData == null) return;

        item.SetItem(currentData.items[index], showType, index);

        var bagScrollViewItem = item as StorageItem;
        bagScrollViewItem.UpdateCellSelect(index == curSelectIndex);
    }

    /// <summary>
    /// 点击item事件
    /// </summary>
    /// <param name="index"></param>
    protected override void OnClickScrollItemAction(int index)
    {
        base.OnClickScrollItemAction(index);
        if (index == curSelectIndex)
        {
            return;
        }

        curSelectIndex = index;

        UpdateSelectItemInfo();

        loopScrollView.UpdateScrollView(true);

    }

    /// <summary>
    /// 更新选中信息
    /// </summary>
    private void UpdateSelectItemInfo()
    {
        if (currentData == null || curSelectIndex < 0 || curSelectIndex >= currentData.items.Count)
        {
            InfoNode.SetActive(false);
            selectItemPrice.text = "--";
            return;
        }
        ItemStack stack = currentData.items[curSelectIndex];
        if (stack == null || stack.item == null)
        {
            InfoNode.SetActive(false);
            selectItemPrice.text = "--";
            return;
        }
        InfoNode.SetActive(true);
        selectItemIcon.sprite = stack.item.icon;
        selectItemName.text = stack.item.itemName;
        selectItemNum.text = stack.count.ToString();
        selectItemDescription.text = stack.item.description;
        var itemPrice = CalculateUnitPrice(stack);
        selectItemPrice.text = itemPrice > 0 ? itemPrice.ToString() : "0";
    }

    private int CalculateUnitPrice(ItemStack stack)
    {
        if (stack == null || stack.item == null)
        {
            return 0;
        }

        float multiplier = showType == InventoryType.Shop ? buyPriceMultiplier : sellPriceMultiplier;
        multiplier = Mathf.Max(0f, multiplier);

        int baseValue = Mathf.Max(0, stack.item.baseValue);
        int price = Mathf.RoundToInt(baseValue * multiplier);
        return price;
    }

    /// <summary>
    /// 切换商店物品类型
    /// </summary>
    public void OnSwitchButtonClick(InventoryType newType)
    {
        if (newType == showType) return; // 重复点击不处理

        showType = newType;

        switch (showType)
        {
            case InventoryType.Backpack:
                currentData = BackpackData;
                break;
            case InventoryType.Storage:
                currentData = StorageData;
                break;
            case InventoryType.Shop:
                currentData = ShopData;
                break;
        }

        ForceRefreshCurrentInventoryView();
    }
    /// <summary>
    /// 更新按钮文本
    /// </summary>
    private void UpdateBuyOrSellText()
    {
        if (showType == InventoryType.Shop)
        {
            BuyOrSellText.text = "购买";
        }
        else
        {
            BuyOrSellText.text = "出售";
        }
    }

    #region ===== 切换按钮点击事件 =====

    /// <summary>
    /// 点击“商店”按钮
    /// </summary>
    public void OnShopButtonClick()
    {
        OnSwitchButtonClick(InventoryType.Shop);
    }

    /// <summary>
    /// 点击“背包”按钮
    /// </summary>
    public void OnBackpackButtonClick()
    {
        OnSwitchButtonClick(InventoryType.Backpack);
    }

    /// <summary>
    /// 点击“仓库”按钮
    /// </summary>
    public void OnStorageButtonClick()
    {
        OnSwitchButtonClick(InventoryType.Storage);
    }

    #endregion

    private void ForceRefreshCurrentInventoryView()
    {
        curSelectIndex = -1;
        if (InfoNode != null)
        {
            InfoNode.SetActive(false);
        }

        UpdateBuyOrSellText();

        if (loopScrollView == null || currentData == null)
        {
            return;
        }

        if (!isScrollViewInitialized || currentData == null)
        {
            InitScrollView(currentData);
            return;
        }

        loopScrollView.RefreshData(currentData.maxCount);
        loopScrollView.ForceRefreshVisibleItems();
    }

    private void OnBuyOrSellButtonClicked()
    {
        if (!IsSelectionValid())
        {
            Debug.LogWarning("[ShopScrollViewPanel] 尚未选中物品，无法进行交易。", this);
            return;
        }

        var stack = currentData.items[curSelectIndex];
        if (stack == null || stack.item == null)
        {
            Debug.LogWarning("[ShopScrollViewPanel] 当前选中物品无效。", this);
            return;
        }

        if (showType == InventoryType.Shop)
        {
            TryBuyItem(stack);
        }
        else
        {
            TrySellItem(stack);
        }
    }

    private void TryBuyItem(ItemStack stack)
    {
        if (stack.count <= 0)
        {
            Debug.LogWarning("[ShopScrollViewPanel] 该商品已售罄。", this);
            return;
        }

        int unitPrice = CalculateUnitPrice(stack);
        if (unitPrice > 0 && GetCurrentGoldAmount() < unitPrice)
        {
            Debug.LogWarning("[ShopScrollViewPanel] 金币不足，无法购买。", this);
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("[ShopScrollViewPanel] InventoryManager 不存在，无法完成购买。", this);
            return;
        }

        var targetInventory = buyReceiveInventory;
        if (targetInventory == InventoryType.Shop)
        {
            targetInventory = InventoryType.Backpack;
        }

        bool addSuccess = inventoryManager.AddItem(stack.item, 1, targetInventory);
        if (!addSuccess)
        {
            Debug.LogWarning("[ShopScrollViewPanel] 背包空间不足，无法购买。", this);
            return;
        }

        if (currencyData != null && unitPrice != 0)
        {
            AdjustGoldCoins(-unitPrice);
        }

        stack.count -= 1;
        if (stack.count <= 0)
        {
            currentData.items[curSelectIndex] = null;
        }

        ShopManager.Instance?.OnInventoryChanged(shopType);
        ForceRefreshCurrentInventoryView();
    }

    private void TrySellItem(ItemStack stack)
    {
        if (stack.count <= 0)
        {
            Debug.LogWarning("[ShopScrollViewPanel] 物品数量不足，无法出售。", this);
            return;
        }

        int unitPrice = CalculateUnitPrice(stack);

        stack.count -= 1;
        if (stack.count <= 0)
        {
            currentData.items[curSelectIndex] = null;
        }

        if (currencyData != null && unitPrice != 0)
        {
            AdjustGoldCoins(unitPrice);
        }

        InventoryManager.Instance?.OnInventoryChanged(showType);
        ForceRefreshCurrentInventoryView();
    }

    private bool IsSelectionValid()
    {
        return currentData != null
            && curSelectIndex >= 0
            && curSelectIndex < currentData.items.Count;
    }

    private int GetCurrentGoldAmount()
    {
        var panel = global::GamePanel.Instance;
        if (panel != null)
        {
            return panel.GoldCoinAmount;
        }

        return currencyData != null ? currencyData.GoldCoinAmount : 0;
    }

    private void AdjustGoldCoins(int delta)
    {
        if (currencyData == null || delta == 0)
        {
            return;
        }

        currencyData.AddGoldCoins(delta, false);
        currencyData.Broadcast();
    }
}
