using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoSingleton<ShopManager>
{
    [Header("武器店数据")]
    public InventoryDataSO WeaponShopData;
    [Header("弹药店数据")]
    public InventoryDataSO AmmoShopData;
    [Header("消耗品店数据")]
    public InventoryDataSO ConsumableShopData;
    [Header("材料店数据")]
    public InventoryDataSO MaterialShopData;
    [Header("食物店数据")]
    public InventoryDataSO FoodShopData;
    public event Action<ShopType> OnShopChangedEvent;   // 商店数据更新事件

    // 根据类型获得库数据
    public InventoryDataSO GetInventory(ShopType type)
    {
        return type switch
        {
            ShopType.WeaponShop => WeaponShopData,
            ShopType.AmmoShop => AmmoShopData,
            ShopType.ConsumableShop => ConsumableShopData,
            ShopType.MaterialShop => MaterialShopData,
            ShopType.FoodShop => FoodShopData,
            _ => null
        };
    }

    // 商店数据更新事件广播
    public void OnInventoryChanged(ShopType type)
    {
        OnShopChangedEvent?.Invoke(type);
        // 可选：触发 UI 刷新或保存事件
        Debug.Log($"{type} 数据已更新");
    }
}
