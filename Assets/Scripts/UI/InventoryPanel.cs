using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板：包含装备栏、背包、仓库三个区域
/// </summary>
public class InventoryPanel : UIPanelBase
{
    [Header("引用UI节点")]
    public Transform equipmentGridRoot;     // 装备栏（2x5）
    public Transform backpackContent;       // 背包 ScrollView 内容节点
    public Transform storageContent;        // 仓库 ScrollView 内容节点

    [Header("物品槽预制体")]
    public GameObject itemSlotPrefab;

    private List<ItemSlotUI> equipmentSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> backpackSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> storageSlots = new List<ItemSlotUI>();

    public override void OnInit()
    {
        base.OnInit();
        StartCoroutine(InitSlots());

        //订阅事件：当背包数据变化时自动刷新
        InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryUpdated;
    }
    public override void OnClose()
    {
        base.OnClose();
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryUpdated;
    }
    /// <summary>
    /// 初始化格子
    /// </summary>
    IEnumerator InitSlots()
    {
        yield return null;
        var inv = InventoryManager.Instance;

        // 确保SO与UI格子数量一致
        inv.EquipmentData.EnsureSlotCount(10);
        inv.BackpackData.EnsureSlotCount(inv.BackpackData.maxCount);
        inv.StorageData.EnsureSlotCount(inv.StorageData.maxCount);

        // 初始化装备栏（固定 2x5 = 10格）
        CreateEmptySlots(equipmentSlots, equipmentGridRoot, 10);

        // 初始化背包默认格子
        CreateEmptySlots(backpackSlots, backpackContent, inv.BackpackData.maxCount);

        // 初始化仓库默认格子
        CreateEmptySlots(storageSlots, storageContent, inv.StorageData.maxCount);
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshAll();
    }

    /// <summary>
    /// 刷新所有UI区域
    /// 每次都从 InventoryManager 的 SO 中重新加载最新数据
    /// </summary>
    public void RefreshAll()
    {
        // 确保数据实时更新
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogError("[InventoryPanel] InventoryManager.Instance 为 null");
            return;
        }

        RefreshEquipment(inv.EquipmentData);
        RefreshBackpack(inv.BackpackData);
        RefreshStorage(inv.StorageData);
    }

    /// <summary>
    /// 刷新装备栏
    /// </summary>
    private void RefreshEquipment(InventoryDataSO equipmentData)
    {
        var data = equipmentData != null ? equipmentData.items : new List<ItemStack>();

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            if (i < data.Count)
                equipmentSlots[i].SetItem(data[i], InventoryType.Equipment, i);
            else
                equipmentSlots[i].ClearSlot();
        }
    }

    /// <summary>
    /// 刷新背包栏（可动态增长）
    /// </summary>
    private void RefreshBackpack(InventoryDataSO backpackData)
    {
        var data = backpackData != null ? backpackData.items : new List<ItemStack>();

        EnsureSlotCount(backpackSlots, backpackContent, data.Count);

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            if (i < data.Count)
                backpackSlots[i].SetItem(data[i], InventoryType.Backpack, i);
            else
                backpackSlots[i].ClearSlot();
        }
    }

    /// <summary>
    /// 刷新仓库栏（可动态增长）
    /// </summary>
    private void RefreshStorage(InventoryDataSO storageData)
    {
        var data = storageData != null ? storageData.items : new List<ItemStack>();

        EnsureSlotCount(storageSlots, storageContent, data.Count);

        for (int i = 0; i < storageSlots.Count; i++)
        {
            if (i < data.Count)
                storageSlots[i].SetItem(data[i], InventoryType.Storage, i);
            else
                storageSlots[i].ClearSlot();
        }
    }

    /// <summary>
    /// 初始化时创建空格子
    /// </summary>
    private void CreateEmptySlots(List<ItemSlotUI> list, Transform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlotUI>();
            slot.ClearSlot(); // 默认显示背景（白边）
            list.Add(slot);
        }
    }

    /// <summary>
    /// 确保Slot数量够用，不够则动态生成
    /// </summary>
    private void EnsureSlotCount(List<ItemSlotUI> list, Transform parent, int targetCount)
    {
        while (list.Count < targetCount)
        {
            var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlotUI>();
            list.Add(slot);
            slot.ClearSlot(); // 默认显示背景
        }
    }
    /// <summary>
    /// 处理界面更新事件
    /// </summary>
    /// <param name="type"></param>
    private void OnInventoryUpdated(InventoryType type)
    {
        if (type == InventoryType.Backpack)
            RefreshBackpack(InventoryManager.Instance.BackpackData);
        else if (type == InventoryType.Equipment)
            RefreshEquipment(InventoryManager.Instance.EquipmentData);
        else if (type == InventoryType.Storage)
            RefreshStorage(InventoryManager.Instance.StorageData);
    }

}
