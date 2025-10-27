using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoSingleton<InventoryManager>
{
    [Header("背包数据")]
    public InventoryDataSO BackpackData;
    [Header("装备数据")]
    public InventoryDataSO EquipmentData;
    [Header("仓库数据")]
    public InventoryDataSO StorageData;

    public event Action<InventoryType> OnInventoryChangedEvent;

    [ContextMenu("测试扩展背包容量 +5")]
    private void TestExpand()
    {
        BackpackData.maxCount += 5;
        Debug.Log($"[InventoryDataSO] 已扩展容量: {BackpackData.maxCount}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="count"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool AddItem(ItemDataSO item, int count, InventoryType type)
    {
        var inv = GetInventory(type);
        return inv.AddItem(item, count, type);
    }
    // 根据类型获得库数据
    public InventoryDataSO GetInventory(InventoryType type)
    {
        return type switch
        {
            InventoryType.Backpack => BackpackData,
            InventoryType.Equipment => EquipmentData,
            InventoryType.Storage => StorageData,
            _ => null
        };
    }
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
