using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InventoryScrollViewPanel : ScrollViewPanel
{
    public InventoryType type;      // 当前显示的库存类型
    private InventoryDataSO InventoryData;      // 当前库存数据
    [Header("当前选中的节点信息数据")]
    public int curSelectIndex = -1;   // 当前选中的索引
    public GameObject InfoNode;  // 选中物品面板
    public Image selectIcon;    // 选中图标
    public TextMeshProUGUI selectItemName;  // 选中物品名称
    public TextMeshProUGUI selectNum;   // 选中物品数量
    public TextMeshProUGUI selectItemDescription; // 选中物品描述

    void Awake()
    {
        InventoryData = InventoryManager.Instance.GetInventory(type);
        InventoryData.EnsureSlotCount(InventoryData.maxCount);
    }

    protected override void Start()
    {
        base.Start();
        InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryChanged;
        SetupScrollView();
    }
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryChanged;
    }
    /// <summary>
    /// 更新选中信息
    /// </summary>
    private void UpdateSelectItemInfo()
    {
        if (curSelectIndex < 0 || curSelectIndex >= InventoryData.items.Count)
        {
            InfoNode?.SetActive(false);
            return;
        }
        var data = InventoryData.items[curSelectIndex];
        if (data == null || data.item == null)
        {
            InfoNode?.SetActive(false);
            return;
        }
        if (selectNum != null) selectNum.text = data.count.ToString();
        if (selectItemName != null) selectItemName.text = data.item.itemName;
        if (selectIcon != null) selectIcon.sprite = data.item.icon;
        if (selectItemDescription != null) selectItemDescription.text = data.item.description;
    }

    /// <summary>
    /// 更新item信息
    /// </summary>
    protected override void OnUpdateScrollItemAction(StorageItem item, int index)
    {
        base.OnUpdateScrollItemAction(item, index);
        item.SetItem(InventoryData.items[index], type, index);

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

        var data = InventoryData.items[index];
        // 点击空格子（没有物品）
        if (data == null || data.item == null)
        {
            curSelectIndex = -1;
            InfoNode.SetActive(false);
            loopScrollView.UpdateScrollView(true);
            return;
        }

        curSelectIndex = index;
        InfoNode.SetActive(true);
        UpdateSelectItemInfo();
        loopScrollView.UpdateScrollView(true);

    }

    /// <summary>
    /// 整理物品后刷新显示，重置选中状态并强制更新滚动视图
    /// </summary>
    public void RefreshAfterSort()
    {
        curSelectIndex = -1;
        if (InfoNode != null)
            InfoNode.SetActive(false);

        if (loopScrollView != null)
        {
            loopScrollView.RefreshData(InventoryData.items.Count);
        }
    }

    void SetupScrollView()
    {
        if (loopScrollView == null || InventoryData == null) return;

        loopScrollView.InitXScrollView(InventoryData.maxCount);
        loopScrollView.AddUpdateCellAction(OnUpdateScrollItemAction);
        loopScrollView.AddCellClickAction(OnClickScrollItemAction);
        loopScrollView.UpdateScrollView(true);
    }
    private void OnInventoryChanged(InventoryType type)
    {
        if (type != this.type) return; // 只刷新自己对应的类型
        RefreshAfterSort();
    }
}
