using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoSingleton<InventoryManager>
{
    [Header("背包数据")]
    public InventoryDataSO BackpackData;
    [Header("装备数据")]
    public InventoryDataSO EquipmentData;
    [Header("仓库数据")]
    public InventoryDataSO StorageData;
    [Header("当前掉落栏临时数据")]
    public InventoryDataSO LootData;                // 临时运行时容器

    [Header("当前场景")]
    public InventoryContext currenContext = InventoryContext.Home;

    public event Action<InventoryType> OnInventoryChangedEvent;

    [ContextMenu("测试扩展背包容量 +5")]
    private void TestExpand()
    {
        BackpackData.maxCount += 5;
        Debug.Log($"[InventoryDataSO] 已扩展容量: {BackpackData.maxCount}");
    }

    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool AddItem(ItemDataSO item, int count, InventoryType type)
    {
        var inv = GetInventory(type);
        if (inv == null)
        {
            Debug.LogWarning($"[InventoryManager] AddItem 失败：{type} 无效。");
            return false;
        }

        bool result = inv.AddItem(item, count, type);
        QuestManager.Instance.UpdateQuestProgress(item.itemName, count);
        OnInventoryChanged(type);
        return result;
    }
    // 根据类型获得库数据
    public InventoryDataSO GetInventory(InventoryType type)
    {
        return type switch
        {
            InventoryType.Backpack => BackpackData,
            InventoryType.Equipment => EquipmentData,
            InventoryType.Storage => StorageData,
            InventoryType.Loot => LootData,
            _ => null
        };
    }
    // 仓库数据更新事件广播
    public void OnInventoryChanged(InventoryType type)
    {
        OnInventoryChangedEvent?.Invoke(type);
        // 可选：触发 UI 刷新或保存事件
        Debug.Log($"{type} 数据已更新");
    }

    #region 检测任务物品
    // 任务物品检测
    public void CheckQuestItem(string questItemName)
    {
        // 检测背包
        CheckInventoryForQuestItem(BackpackData, questItemName);
        // 检测仓库
        CheckInventoryForQuestItem(StorageData, questItemName);
        // 检测装备栏
        CheckInventoryForQuestItem(EquipmentData, questItemName);
    }

    private void CheckInventoryForQuestItem(InventoryDataSO backpackData, string questItemName)
    {
        if (backpackData == null || backpackData.items == null)
            return;

        foreach (var item in backpackData.items)
        {
            if (item == null || item.item == null)
                continue;

            if (item.item.itemName == questItemName)
            {
                QuestManager.Instance.UpdateQuestProgress(questItemName, item.count);
            }
        }
    }

    #endregion

    #region 消耗任务物品
    public bool ConsumeQuestItems(string questItemName, int amount)
    {
        if (string.IsNullOrEmpty(questItemName) || amount <= 0)
            return true;

        int remaining = amount;

        ConsumeQuestItemsFromInventory(BackpackData, questItemName, ref remaining);
        ConsumeQuestItemsFromInventory(StorageData, questItemName, ref remaining);
        ConsumeQuestItemsFromInventory(EquipmentData, questItemName, ref remaining);
        ConsumeQuestItemsFromInventory(LootData, questItemName, ref remaining);

        return remaining <= 0;
    }

    private void ConsumeQuestItemsFromInventory(InventoryDataSO inventory, string questItemName, ref int remaining)
    {
        if (inventory == null || remaining <= 0)
            return;

        bool changed = false;

        for (int i = inventory.items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var stack = inventory.items[i];
            if (stack == null || stack.item == null)
                continue;

            if (stack.item.itemName != questItemName)
                continue;

            int removeCount = Mathf.Min(remaining, stack.count);
            stack.count -= removeCount;
            remaining -= removeCount;
            changed = true;

            if (stack.count <= 0)
            {
                inventory.items[i] = null;
            }
        }

        if (changed)
        {
            OnInventoryChanged(inventory.type);
        }
    }

    public int GetQuestItemCount(string questItemName)
    {
        if (string.IsNullOrEmpty(questItemName))
            return 0;

        int total = 0;
        total += GetQuestItemCountFromInventory(BackpackData, questItemName);
        total += GetQuestItemCountFromInventory(StorageData, questItemName);
        total += GetQuestItemCountFromInventory(EquipmentData, questItemName);
        total += GetQuestItemCountFromInventory(LootData, questItemName);
        return total;
    }

    private int GetQuestItemCountFromInventory(InventoryDataSO inventory, string questItemName)
    {
        if (inventory == null || inventory.items == null)
            return 0;

        int total = 0;
        foreach (var stack in inventory.items)
        {
            if (stack == null || stack.item == null)
                continue;

            if (stack.item.itemName == questItemName)
            {
                total += stack.count;
            }
        }
        return total;
    }
    #endregion
}
