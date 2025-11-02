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
    public Image selectIcon;    // 选中图标
    public TextMeshProUGUI selectItemName;  // 选中物品名称
    public TextMeshProUGUI selectNum;   // 选中物品数量
    public TextMeshProUGUI selectItemDescription; // 选中物品描述

    [Header("购买/出售按钮")]
    public Button BuyOrSellBtn;
    public TextMeshProUGUI BuyOrSellText;
    // 库存数据
    private InventoryDataSO BackpackData;   // 背包数据
    private InventoryDataSO StorageData;    // 仓库数据
    private InventoryDataSO ShopData;       // 商店数据

    private InventoryDataSO currentData;    // 当前展示的数据源

    private void Awake()
    {
        var inv = InventoryManager.Instance;
        BackpackData = inv.GetInventory(InventoryType.Backpack);
        StorageData = inv.GetInventory(InventoryType.Storage);
        ShopData = ShopManager.Instance.GetInventory(shopType);

        BackpackData.EnsureSlotCount(BackpackData.maxCount);    // 确保数据数量足够
        StorageData.EnsureSlotCount(StorageData.maxCount);
        ShopData.EnsureSlotCount(ShopData.maxCount);
    }
    protected override void Start()
    {
        base.Start();

        // 默认显示商店全部物品
        showType = InventoryType.Shop;
        currentData = ShopData;

        InitScrollView(currentData);
        UpdateBuyOrSellText();
    }
    
    /// <summary>
    /// 初始化或刷新滚动视图
    /// </summary>
    private void InitScrollView(InventoryDataSO data)
    {
        if (data == null) return;

        loopScrollView.InitXScrollView(data.maxCount);
        loopScrollView.AddUpdateCellAction(OnUpdateScrollItemAction);
        loopScrollView.AddCellClickAction(OnClickScrollItemAction);
    }
    /// <summary>
    /// 更新item信息
    /// </summary>
    protected override void OnUpdateScrollItemAction(ItemSlot item, int index)
    {
        base.OnUpdateScrollItemAction(item, index);
        if (currentData == null) return;

        item.SetItem(currentData.items[index], showType, index);

        var bagScrollViewItem = item as ItemSlot;
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
        ItemStack stack = currentData.items[curSelectIndex];
        if (stack == null || stack.item == null)
        {
            InfoNode.SetActive(false);
            return;
        }
        InfoNode.SetActive(true);
        selectIcon.sprite = stack.item.icon;
        selectItemName.text = stack.item.itemName;
        selectNum.text = stack.count.ToString();
        selectItemDescription.text = stack.item.description;
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

        curSelectIndex = -1;
        InfoNode.SetActive(false);

        UpdateBuyOrSellText();

        loopScrollView.ClearAllCells();
        InitScrollView(currentData);
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
}
