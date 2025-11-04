using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板：包含装备栏、背包、仓库、战利品等功能
/// </summary>
public class InventoryPanel : UIPanelBase
{
    // [Header("容器UI节点")]
    // public Transform equipmentGridRoot;     // 装备栏2x5格
    // public Transform backpackContent;       // 背包 ScrollView 内容节点
    // public Transform storageContent;        // 仓库 ScrollView 内容节点
    // public Transform lootGridRoot;          // 战利品

    [Header("Panel节点")]
    public Transform LootGridText;
    public Transform LootGrid;
    public Transform InfoNodeRoot;
    public Transform RightPanel;

    // [Header("物品槽预制体")]
    // public GameObject itemSlotPrefab;

    // private List<ItemSlot> equipmentSlots = new List<ItemSlot>();
    // private List<ItemSlot> backpackSlots = new List<ItemSlot>();
    // private List<ItemSlot> storageSlots = new List<ItemSlot>();
    // private List<ItemSlot> lootSlots = new List<ItemSlot>();


    public override void OnInit()
    {
        base.OnInit();
        //InitSlots();

        //注册事件监听，当库存数据变化时自动刷新
        // InventoryManager.Instance.OnInventoryChangedEvent += OnInventoryUpdated;
    }
    public override void OnClose()
    {
        base.OnClose();
        // if (InventoryManager.Instance != null)
        //     InventoryManager.Instance.OnInventoryChangedEvent -= OnInventoryUpdated;
    }
    /// <summary>
    /// 初始化面板
    /// </summary>
    // private void InitSlots()
    // {
    //     var inv = InventoryManager.Instance;

    //     // 确保SO与UI格子数量一致
    //     inv.EquipmentData.EnsureSlotCount(10);
    //     // inv.BackpackData.EnsureSlotCount(inv.BackpackData.maxCount);
    //     // inv.StorageData.EnsureSlotCount(inv.StorageData.maxCount);

    //     // 初始化装备栏固定 2x5 = 10格
    //     CreateEmptySlots(equipmentSlots, equipmentGridRoot, 10);

    //     // 初始化背包默认格子
    //     //CreateEmptySlots(backpackSlots, backpackContent, inv.BackpackData.maxCount);

    //     // 初始化仓库默认格子
    //     //CreateEmptySlots(storageSlots, storageContent, inv.StorageData.maxCount);
    // }

    public override void OnShow()
    {
        base.OnShow();
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

    // /// <summary>
    // /// 刷新所有UI格子
    // /// 每次由 InventoryManager 的 SO 数据更新时调用
    // /// </summary>
    // public void RefreshAll()
    // {
    //     // 确保数据实时性
    //     var inv = InventoryManager.Instance;
    //     if (inv == null)
    //     {
    //         Debug.LogError("[InventoryPanel] InventoryManager.Instance 为 null");
    //         return;
    //     }

    //     RefreshEquipment(inv.EquipmentData);
    //     RefreshBackpack(inv.BackpackData);
    //     RefreshStorage(inv.StorageData);
    //     RefreshLoot(inv.LootData);
    // }

    // /// <summary>
    // /// 刷新装备栏
    // /// </summary>
    // private void RefreshEquipment(InventoryDataSO equipmentData)
    // {
    //     var data = equipmentData != null ? equipmentData.items : new List<ItemStack>();

    //     for (int i = 0; i < equipmentSlots.Count; i++)
    //     {
    //         if (i < data.Count)
    //             equipmentSlots[i].SetItem(data[i], InventoryType.Equipment, i);
    //         else
    //             equipmentSlots[i].ClearSlot();
    //     }
    // }

    // /// <summary>
    // /// 刷新背包动态格子
    // /// </summary>
    // private void RefreshBackpack(InventoryDataSO backpackData)
    // {
    //     var data = backpackData != null ? backpackData.items : new List<ItemStack>();

    //     EnsureSlotCount(backpackSlots, backpackContent, data.Count);

    //     for (int i = 0; i < backpackSlots.Count; i++)
    //     {
    //         if (i < data.Count)
    //             backpackSlots[i].SetItem(data[i], InventoryType.Backpack, i);
    //         else
    //             backpackSlots[i].ClearSlot();
    //     }
    // }

    // // <summary>
    // // 刷新仓库动态格子
    // // </summary>
    // private void RefreshStorage(InventoryDataSO storageData)
    // {
    //    var data = storageData != null ? storageData.items : new List<ItemStack>();

    //    EnsureSlotCount(storageSlots, storageContent, data.Count);

    //    for (int i = 0; i < storageSlots.Count; i++)
    //    {
    //        if (i < data.Count)
    //            storageSlots[i].SetItem(data[i], InventoryType.Storage, i);
    //        else
    //            storageSlots[i].ClearSlot();
    //    }
    // }
    // /// <summary>
    // /// 刷新战利品
    // /// </summary>
    // private void RefreshLoot(InventoryDataSO data)
    // {
    //     EnsureSlotCount(lootSlots, lootGridRoot, data.items.Count);
    //     for (int i = 0; i < lootSlots.Count; i++)
    //     {
    //         if (i < data.items.Count)
    //             lootSlots[i].SetItem(data.items[i], InventoryType.Loot, i);
    //         else
    //             lootSlots[i].ClearSlot();
    //     }
    // }
    // /// <summary>
    // /// 初始化时创建空格子
    // /// </summary>
    // private void CreateEmptySlots(List<ItemSlot> list, Transform parent, int count)
    // {
    //     for (int i = 0; i < count; i++)
    //     {
    //         var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlot>();
    //         slot.ClearSlot(); // 默认显示空格子
    //         list.Add(slot);
    //     }
    // }

    // /// <summary>
    // /// 确保Slot数量足够，用于动态格子
    // /// </summary>
    // private void EnsureSlotCount(List<ItemSlot> list, Transform parent, int targetCount)
    // {
    //     while (list.Count < targetCount)
    //     {
    //         var slot = Instantiate(itemSlotPrefab, parent).GetComponent<ItemSlot>();
    //         list.Add(slot);
    //         slot.ClearSlot(); // 默认显示空格子
    //     }
    // }
    // /// <summary>
    // /// 监听库存变化事件
    // /// </summary>
    // /// <param name="type"></param>
    // private void OnInventoryUpdated(InventoryType type)
    // {
    //     var inv = InventoryManager.Instance;
    //     switch (type)
    //     {
    //         case InventoryType.Equipment:
    //             RefreshEquipment(inv.EquipmentData); break;
    //         case InventoryType.Backpack:
    //             RefreshBackpack(inv.BackpackData); break;
    //         case InventoryType.Storage:
    //             RefreshStorage(inv.StorageData);
    //             break;
    //         case InventoryType.Loot:
    //             RefreshLoot(inv.LootData); break;
    //     }
    // }


    // public void OnSortBackpackButtonClicked()
    // {
    //     InventoryManager.Instance.BackpackData.SortItems();
    //     InventoryManager.Instance.OnInventoryChanged(InventoryType.Backpack);
    // }
    // public void OnSortStorageButtonClicked()
    // {
    //     InventoryManager.Instance.StorageData.SortItems();
    //     InventoryManager.Instance.OnInventoryChanged(InventoryType.Storage);
    // }

}