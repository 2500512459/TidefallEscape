using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// 整理功能
    /// </summary>
    /// <param name="type"></param>
    public void SortInventory(InventoryType type)
    {
        var data = GetInventory(type);
        if (data == null) return;

        // 去除空格子前先复制一份
        var items = new List<ItemStack>(data.items);

        // 排序
        items.Sort(new ItemComparer());

        // 重新赋值
        data.items.Clear();
        data.items.AddRange(items);

        // 通知更新
        OnInventoryChanged(type);
    }
}
